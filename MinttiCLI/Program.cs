using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using mintti_sdk.ble.bean;
using mintti_sdk.ble.constants;
using mintti_sdk.ble.manager;
using Newtonsoft.Json;

namespace MinttiCLI
{
    class Program
    {
        private static int _exitCode;

        [STAThread]
        static int Main(string[] args)
        {
            if (args.Length == 0 || args.Contains("-help") || args.Contains("--help"))
            {
                PrintHelp();
                return 0;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // The Mintti BLE SDK marshals its WinRT callbacks onto the WinForms
            // SynchronizationContext captured at init time. We must therefore install
            // that context on THIS STA thread and run all SDK work while a message
            // pump is active on the same thread (mirroring how the GUI demo works).
            var syncContext = new WindowsFormsSynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(syncContext);

            syncContext.Post(async _ =>
            {
                try
                {
                    _exitCode = await RunAsync(args);
                }
                catch (Exception ex)
                {
                    PrintError("INTERNAL_ERROR", ex.Message);
                    Console.Error.WriteLine("=== FULL EXCEPTION (debug) ===");
                    Console.Error.WriteLine(ex.ToString());
                    var inner = ex.InnerException;
                    while (inner != null)
                    {
                        Console.Error.WriteLine("--- Inner Exception ---");
                        Console.Error.WriteLine(inner.ToString());
                        inner = inner.InnerException;
                    }
                    _exitCode = 1;
                }
                finally
                {
                    Application.ExitThread();
                }
            }, null);

            Application.Run();
            return _exitCode;
        }

        static async Task<int> RunAsync(string[] args)
        {
            var ble = MinttiBle.GetInstance;

            if (args.Contains("-list"))
            {
                return await ListDevices(ble);
            }

            if (args.Contains("-connect"))
            {
                string mac = GetArgValue(args, "-mac");
                if (string.IsNullOrEmpty(mac))
                {
                    PrintError("MISSING_MAC", "Please provide a MAC address using -mac");
                    return 1;
                }
                return await ConnectAndStream(ble, mac);
            }

            PrintError("UNKNOWN_COMMAND", "Unknown command or missing arguments");
            PrintHelp();
            return 1;
        }

        static void PrintHelp()
        {
            Console.WriteLine("Mintti Stethoscope CLI Tool");
            Console.WriteLine("Usage:");
            Console.WriteLine("  mintti_cli.exe [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  -list              Scan for available stethoscope devices (5 seconds).");
            Console.WriteLine("  -connect -mac MAC  Connect to a device and stream data to stdout.");
            Console.WriteLine("  -help              Show this help message.");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  mintti_cli.exe -list");
            Console.WriteLine("  mintti_cli.exe -connect -mac AA:BB:CC:DD:EE:FF");
            Console.WriteLine();
            Console.WriteLine("Data Format (Stdout):");
            Console.WriteLine("  List:        DATA:ITEM index={n} name=\"{name}\" mac=\"{mac}\"");
            Console.WriteLine("  Stream:      DATA:STREAM type=audio data=[...]");
            Console.WriteLine("  Heart Rate:  DATA:STREAM type=heartrate value={int}");
            Console.WriteLine("  Status:      DATA:STATUS message=\"...\"");
            Console.WriteLine("  Error:       DATA:ERROR code={code} message=\"...\"");
        }

        static void PrintError(string code, string message)
        {
            Console.WriteLine($"DATA:ERROR code={code} message=\"{message}\"");
        }

        static void PrintOk(string command, string extra = "")
        {
            string output = $"DATA:OK command={command}";
            if (!string.IsNullOrEmpty(extra)) output += " " + extra;
            Console.WriteLine(output);
        }

        static string GetArgValue(string[] args, string flag)
        {
            int index = Array.IndexOf(args, flag);
            if (index >= 0 && index < args.Length - 1)
            {
                return args[index + 1];
            }
            return null;
        }

        static async Task<bool> CheckBleAsync(MinttiBle ble)
        {
            bool isEnable = await ble.GetBleEnableAsync();
            if (!isEnable)
            {
                PrintError("BLE_UNAVAILABLE", "Bluetooth is not available on this system");
                return false;
            }

            bool isOpen = await ble.IsOpenBleAsync();
            if (!isOpen)
            {
                PrintError("BLE_DISABLED", "Bluetooth is turned off. Please enable Bluetooth first.");
                return false;
            }

            return true;
        }

        static async Task<int> ListDevices(MinttiBle ble)
        {
            if (!await CheckBleAsync(ble))
            {
                return 1;
            }

            List<DeviceInfo> devices = new List<DeviceInfo>();

            ble.DeviceWatcherChanged += (device) =>
            {
                if (device == null || string.IsNullOrEmpty(device.Mac))
                {
                    return;
                }

                if (!devices.Any(d => d.Mac == device.Mac))
                {
                    devices.Add(device);
                }
            };

            ble.StartBleDeviceWatcher();
            Console.WriteLine("DATA:STATUS message=\"Scanning for 5 seconds...\"");
            await Task.Delay(5000);
            ble.StopBleDeviceWatcher();

            Console.WriteLine($"DATA:LIST count={devices.Count}");
            for (int i = 0; i < devices.Count; i++)
            {
                Console.WriteLine($"DATA:ITEM index={i} name=\"{devices[i].Name}\" mac=\"{devices[i].Mac}\"");
            }
            PrintOk("list");
            return 0;
        }

        static async Task<int> ConnectAndStream(MinttiBle ble, string mac)
        {
            if (!await CheckBleAsync(ble))
            {
                return 1;
            }

            var connectResult = new TaskCompletionSource<bool>();

            ble.MessageChanged += (type, message, data) =>
            {
                if (type == MsgType.ConnectStatus)
                {
                    if (message == MinttiConstants.CONNECTED)
                    {
                        connectResult.TrySetResult(true);
                    }
                    else if (message == MinttiConstants.CONNECTION_FAILED || message == MinttiConstants.DISCONNECT)
                    {
                        connectResult.TrySetResult(false);
                    }
                    Console.WriteLine($"DATA:STATUS type=connection message=\"{message}\"");
                }
            };

            ble.DataCallback += (data) =>
            {
                string jsonData = JsonConvert.SerializeObject(data);
                Console.WriteLine($"DATA:STREAM type=audio data={jsonData}");
            };

            ble.HeartRateCallback += (hr) =>
            {
                Console.WriteLine($"DATA:STREAM type=heartrate value={hr}");
            };

            ble.ConnectByMac(mac);

            // Non-blocking wait so the STA message pump keeps delivering SDK callbacks.
            var timeout = Task.Delay(10000);
            var finished = await Task.WhenAny(connectResult.Task, timeout);
            bool connected = finished == connectResult.Task && connectResult.Task.Result;

            if (!connected)
            {
                PrintError("CONNECT_FAILED", "Failed to connect to device " + mac);
                return 1;
            }

            PrintOk("connect", $"mac={mac}");

            ble.StartMeasure();
            Console.WriteLine("DATA:STATUS message=\"Streaming started. Press Ctrl+C to stop.\"");

            var tcs = new TaskCompletionSource<bool>();
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                tcs.SetResult(true);
            };

            await tcs.Task;

            ble.StopMeasure();
            ble.Dispose();
            PrintOk("stop");
            return 0;
        }
    }
}

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
        private static string[] _args;

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

            // The Mintti BLE SDK was written to run inside a WinForms Form: it relies on
            // the WinForms message loop / SynchronizationContext and (in StartBleDeviceWatcher)
            // on there being a real Form present. A console host lacks this and the SDK
            // throws a NullReferenceException. To match the working GUI demo exactly, we
            // host a real Form and run all SDK work from its Load event -- but the form is
            // kept completely invisible so this stays a console tool.
            _args = args;
            Application.Run(new HiddenForm());
            return _exitCode;
        }

        // A real WinForms Form that is never visible. Creating it gives the SDK the exact
        // runtime environment it expects (message pump, sync context, an active Form).
        private sealed class HiddenForm : Form
        {
            public HiddenForm()
            {
                Opacity = 0;
                ShowInTaskbar = false;
                WindowState = FormWindowState.Minimized;
                FormBorderStyle = FormBorderStyle.None;
                Load += HiddenForm_Load;
            }

            private async void HiddenForm_Load(object sender, EventArgs e)
            {
                // Keep it hidden even though Load requires the form to be "shown".
                Hide();

                try
                {
                    _exitCode = await RunAsync(_args);
                }
                catch (Exception ex)
                {
                    DumpException(ex);
                    _exitCode = 1;
                }
                finally
                {
                    // Always release the BLE adapter/device so we never leave the radio
                    // held by this process (a lingering handle stops the device from
                    // advertising, which makes later scans find nothing).
                    try
                    {
                        MinttiBle.GetInstance.StopBleDeviceWatcher();
                    }
                    catch { /* ignore */ }
                    try
                    {
                        MinttiBle.GetInstance.Dispose();
                    }
                    catch { /* ignore */ }

                    Close();

                    // Force the process to terminate immediately so no background SDK
                    // thread keeps the Bluetooth adapter busy after we're done.
                    Environment.Exit(_exitCode);
                }
            }
        }

        static void DumpException(Exception ex)
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
        }

        // Shared state touched by the SDK event handlers.
        private static readonly List<DeviceInfo> _devices = new List<DeviceInfo>();
        private static readonly TaskCompletionSource<bool> _connectResult = new TaskCompletionSource<bool>();
        private static bool _streaming;

        static async Task<int> RunAsync(string[] args)
        {
            var ble = MinttiBle.GetInstance;

            // IMPORTANT: The Mintti SDK raises several of these events internally (some
            // without null-checks). The GUI demo subscribes to ALL of them in its
            // constructor before ever scanning/connecting. If any are left unsubscribed,
            // the SDK throws a NullReferenceException (e.g. inside StartBleDeviceWatcher).
            // So we wire up every event up front, exactly like the GUI does.
            WireAllEvents(ble);

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

        static void WireAllEvents(MinttiBle ble)
        {
            // Device discovered during scanning.
            ble.DeviceWatcherChanged += (device) =>
            {
                if (device == null || string.IsNullOrEmpty(device.Mac))
                {
                    return;
                }
                lock (_devices)
                {
                    if (!_devices.Any(d => d.Mac == device.Mac))
                    {
                        _devices.Add(device);
                    }
                }
            };

            // Connection status + general messages.
            ble.MessageChanged += (type, message, data) =>
            {
                if (type == MsgType.ConnectStatus)
                {
                    if (message == MinttiConstants.CONNECTED)
                    {
                        _connectResult.TrySetResult(true);
                    }
                    else if (message == MinttiConstants.CONNECTION_FAILED || message == MinttiConstants.DISCONNECT)
                    {
                        _connectResult.TrySetResult(false);
                    }
                    Console.WriteLine($"DATA:STATUS type=connection message=\"{message}\"");
                }
            };

            // Auscultation audio data.
            ble.DataCallback += (data) =>
            {
                if (!_streaming) return;
                string jsonData = JsonConvert.SerializeObject(data);
                Console.WriteLine($"DATA:STREAM type=audio data={jsonData}");
            };

            // Heart rate.
            ble.HeartRateCallback += (hr) =>
            {
                if (!_streaming) return;
                Console.WriteLine($"DATA:STREAM type=heartrate value={hr}");
            };

            // The remaining events are not used by the CLI, but MUST be subscribed so the
            // SDK never invokes a null delegate. No-op handlers are enough.
            ble.PowerCallback += (power) => { };
            ble.ModelSwitchback += (mode) => { };
            ble.DataLoseCallback += () => { };
            ble.PressTooBigCallback += () => { };
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

            ble.StartBleDeviceWatcher();
            Console.WriteLine("DATA:STATUS message=\"Scanning for 5 seconds...\"");
            await Task.Delay(5000);
            ble.StopBleDeviceWatcher();

            DeviceInfo[] found;
            lock (_devices)
            {
                found = _devices.ToArray();
            }

            Console.WriteLine($"DATA:LIST count={found.Length}");
            for (int i = 0; i < found.Length; i++)
            {
                Console.WriteLine($"DATA:ITEM index={i} name=\"{found[i].Name}\" mac=\"{found[i].Mac}\"");
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

            // The SDK can only connect to a device it has already discovered during a scan
            // in THIS session (ConnectByMac looks the MAC up in a cache the watcher fills).
            // Since -connect is a fresh process, we must scan first -- exactly like the GUI
            // (scan -> device appears -> select -> connect).
            Console.WriteLine("DATA:STATUS message=\"Scanning for target device...\"");
            ble.StartBleDeviceWatcher();

            bool found = false;
            var scanDeadline = DateTime.UtcNow.AddSeconds(15);
            while (DateTime.UtcNow < scanDeadline)
            {
                lock (_devices)
                {
                    found = _devices.Any(d => string.Equals(d.Mac, mac, StringComparison.OrdinalIgnoreCase));
                }
                if (found)
                {
                    break;
                }
                await Task.Delay(300);
            }

            ble.StopBleDeviceWatcher();

            if (!found)
            {
                PrintError("DEVICE_NOT_FOUND", "Device " + mac + " was not found during scan. Make sure it is powered on and in range.");
                return 1;
            }

            Console.WriteLine("DATA:STATUS message=\"Device found, connecting...\"");
            ble.ConnectByMac(mac);

            // Non-blocking wait so the STA message pump keeps delivering SDK callbacks.
            var timeout = Task.Delay(15000);
            var finished = await Task.WhenAny(_connectResult.Task, timeout);
            bool connected = finished == _connectResult.Task && _connectResult.Task.Result;

            if (!connected)
            {
                PrintError("CONNECT_FAILED", "Failed to connect to device " + mac);
                return 1;
            }

            PrintOk("connect", $"mac={mac}");

            _streaming = true;
            ble.StartMeasure();
            Console.WriteLine("DATA:STATUS message=\"Streaming started. Press Ctrl+C to stop.\"");

            var tcs = new TaskCompletionSource<bool>();
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                tcs.SetResult(true);
            };

            await tcs.Task;

            _streaming = false;
            ble.StopMeasure();
            ble.Dispose();
            PrintOk("stop");
            return 0;
        }
    }
}

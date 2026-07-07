using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using mintti_sdk.ble.bean;
using mintti_sdk.ble.constants;
using mintti_sdk.ble.manager;
using Newtonsoft.Json;

namespace MinttiCLI
{
    class Program
    {
        private static bool _isScanning = false;
        private static bool _isConnected = false;
        private static string _targetMac = null;
        private static AutoResetEvent _waitHandle = new AutoResetEvent(false);

        static async Task Main(string[] args)
        {
            if (args.Length == 0 || args.Contains("-help") || args.Contains("--help"))
            {
                PrintHelp();
                return;
            }

            try
            {
                // Initialize SDK
                var ble = MinttiBle.GetInstance;
                
                if (args.Contains("-list"))
                {
                    await ListDevices(ble);
                }
                else if (args.Contains("-connect"))
                {
                    string mac = GetArgValue(args, "-mac");
                    if (string.IsNullOrEmpty(mac))
                    {
                        PrintError("MISSING_MAC", "Please provide a MAC address using -mac");
                        Environment.Exit(1);
                    }
                    await ConnectAndStream(ble, mac);
                }
                else
                {
                    PrintError("UNKNOWN_COMMAND", "Unknown command or missing arguments");
                    PrintHelp();
                    Environment.Exit(1);
                }
            }
            catch (Exception ex)
            {
                PrintError("INTERNAL_ERROR", ex.Message);
                Environment.Exit(1);
            }
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

        static async Task ListDevices(MinttiBle ble)
        {
            List<DeviceInfo> devices = new List<DeviceInfo>();
            
            ble.DeviceWatcherChanged += (device) => {
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
        }

        static async Task ConnectAndStream(MinttiBle ble, string mac)
        {
            bool connected = false;
            
            ble.MessageChanged += (type, message, data) => {
                if (type == MsgType.ConnectStatus)
                {
                    if (message == MinttiConstants.CONNECTED)
                    {
                        connected = true;
                        _waitHandle.Set();
                    }
                    else if (message == MinttiConstants.CONNECTION_FAILED || message == MinttiConstants.DISCONNECT)
                    {
                        connected = false;
                        _waitHandle.Set();
                    }
                    Console.WriteLine($"DATA:STATUS type=connection message=\"{message}\"");
                }
            };

            ble.DataCallback += (data) => {
                // Stream data as JSON array of shorts
                string jsonData = JsonConvert.SerializeObject(data);
                Console.WriteLine($"DATA:STREAM type=audio data={jsonData}");
            };

            ble.HeartRateCallback += (hr) => {
                Console.WriteLine($"DATA:STREAM type=heartrate value={hr}");
            };

            ble.ConnectByMac(mac);
            
            // Wait for connection result
            _waitHandle.WaitOne(10000);

            if (!connected)
            {
                PrintError("CONNECT_FAILED", "Failed to connect to device " + mac);
                return;
            }

            PrintOk("connect", $"mac={mac}");
            
            // Start measuring
            ble.StartMeasure();
            Console.WriteLine("DATA:STATUS message=\"Streaming started. Press Ctrl+C to stop.\"");

            // Keep alive until interrupted
            var tcs = new TaskCompletionSource<bool>();
            Console.CancelKeyPress += (s, e) => {
                e.Cancel = true;
                tcs.SetResult(true);
            };

            await tcs.Task;

            ble.StopMeasure();
            ble.Dispose();
            PrintOk("stop");
        }
    }
}

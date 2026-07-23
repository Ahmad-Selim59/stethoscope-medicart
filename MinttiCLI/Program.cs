using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

        // When true, verbose [SDK] diagnostics are emitted (to stderr). OFF by default:
        // the SDK callbacks fire on the BLE worker thread, and doing synchronous console
        // writes there stalls that thread. If the stall exceeds the BLE supervision
        // timeout the device itself drops the link (symptom: one audio packet, then
        // "Device unavailable"). Keeping diagnostics off (or async, see below) keeps the
        // BLE thread responsive so streaming stays alive.
        private static bool _verbose;

        // A queued output line and which stream it belongs to.
        private struct OutLine
        {
            public bool IsError;
            public string Text;
        }

        // Single background writer for BOTH stdout (DATA) and stderr (diagnostics). SDK
        // callbacks fire on the BLE worker thread; writing to a (slow) console there blocks
        // that thread and stalls/drops the BLE stream. We instead enqueue every line and let
        // this dedicated thread drain it, keeping all callbacks instant and non-blocking.
        private static readonly BlockingCollection<OutLine> _outputQueue =
            new BlockingCollection<OutLine>(new ConcurrentQueue<OutLine>());
        private static Thread _outputThread;

        static void StartOutputWriter()
        {
            var stdout = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false), 1 << 16)
            {
                AutoFlush = false
            };
            var stderr = new StreamWriter(Console.OpenStandardError(), new UTF8Encoding(false), 1 << 16)
            {
                AutoFlush = false
            };

            _outputThread = new Thread(() =>
            {
                try
                {
                    foreach (var item in _outputQueue.GetConsumingEnumerable())
                    {
                        var target = item.IsError ? stderr : stdout;
                        target.WriteLine(item.Text);
                        // Flush when the queue drains so consumers get timely data without
                        // paying a syscall per line during bursts.
                        if (_outputQueue.Count == 0)
                        {
                            stdout.Flush();
                            stderr.Flush();
                        }
                    }
                }
                catch { /* shutting down */ }
                finally
                {
                    try { stdout.Flush(); } catch { }
                    try { stderr.Flush(); } catch { }
                }
            })
            {
                IsBackground = true,
                Name = "output-writer"
            };
            _outputThread.Start();
        }

        // Enqueue a DATA line for stdout (non-blocking for SDK callbacks).
        static void Emit(string line)
        {
            if (!_outputQueue.IsAddingCompleted)
            {
                _outputQueue.Add(new OutLine { IsError = false, Text = line });
            }
        }

        // Enqueue a diagnostic line for stderr, only when -verbose is set. Also non-blocking
        // so it can never stall the BLE worker thread.
        static void Log(string line)
        {
            if (_verbose && !_outputQueue.IsAddingCompleted)
            {
                _outputQueue.Add(new OutLine { IsError = true, Text = line });
            }
        }

        // Always-on diagnostic to stderr (errors/exception dumps), routed through the same
        // writer so it never blocks and never interleaves with the buffered streams.
        static void LogErr(string line)
        {
            if (!_outputQueue.IsAddingCompleted)
            {
                _outputQueue.Add(new OutLine { IsError = true, Text = line });
            }
        }

        static void FlushOutput()
        {
            try
            {
                _outputQueue.CompleteAdding();
                _outputThread?.Join(TimeSpan.FromSeconds(2));
            }
            catch { /* ignore */ }
        }

        [STAThread]
        static int Main(string[] args)
        {
            StartOutputWriter();

            if (args.Length == 0 || args.Contains("-help") || args.Contains("--help"))
            {
                PrintHelp();
                FlushOutput();
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

                    // Give the BLE stack time to actually send the disconnect PDU to the
                    // device before we kill the process. Without this, Environment.Exit tears
                    // the process down mid-disconnect, leaving the stethoscope half-connected
                    // on its side. The next run then gets a stale, cached GATT handle from
                    // Windows (symptom: "GattServices size=0" / immediate "Disconnect"),
                    // which only a power-cycle clears. This settle delay is what lets repeated
                    // connect/disconnect cycles work without power-cycling the device.
                    Thread.Sleep(2000);

                    Close();

                    // Drain any queued stdout before we terminate.
                    FlushOutput();

                    // Force the process to terminate so no background SDK thread keeps the
                    // Bluetooth adapter busy after we're done.
                    Environment.Exit(_exitCode);
                }
            }
        }

        static void DumpException(Exception ex)
        {
            PrintError("INTERNAL_ERROR", ex.Message);
            LogErr("=== FULL EXCEPTION (debug) ===");
            LogErr(ex.ToString());
            var inner = ex.InnerException;
            while (inner != null)
            {
                LogErr("--- Inner Exception ---");
                LogErr(inner.ToString());
                inner = inner.InnerException;
            }
        }

        // Shared state touched by the SDK event handlers.
        private static readonly List<DeviceInfo> _devices = new List<DeviceInfo>();
        private static TaskCompletionSource<bool> _connectResult = new TaskCompletionSource<bool>();
        private static bool _streaming;

        static async Task<int> RunAsync(string[] args)
        {
            // Verbose SDK diagnostics are OFF by default: synchronous logging on the SDK's
            // BLE callback thread can stall it and make the device drop the link mid-stream.
            _verbose = args.Contains("-verbose") || args.Contains("-v");

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
                // Log EVERY message the SDK emits so we can see exactly what happens
                // during a connect attempt (written to stderr so it doesn't pollute stdout).
                Log($"[SDK] MessageChanged type={type} message=\"{message}\"");

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
                    Emit($"DATA:STATUS type=connection message=\"{message}\"");
                }
            };

            // Auscultation audio data. Enqueue to the background writer so this callback
            // returns immediately and never blocks the message pump / BLE pipeline.
            ble.DataCallback += (data) =>
            {
                if (!_streaming) return;
                string jsonData = JsonConvert.SerializeObject(data);
                Emit($"DATA:STREAM type=audio data={jsonData}");
            };

            // Heart rate.
            ble.HeartRateCallback += (hr) =>
            {
                if (!_streaming) return;
                Emit($"DATA:STREAM type=heartrate value={hr}");
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
            Emit("Mintti Stethoscope CLI Tool");
            Emit("Usage:");
            Emit("  MinttiCLI.exe [options]");
            Emit("");
            Emit("Options:");
            Emit("  -list              Scan for available stethoscope devices (5 seconds).");
            Emit("  -connect -mac MAC  Connect to a device and stream data to stdout.");
            Emit("  -verbose, -v       Print SDK diagnostics to stderr (for debugging).");
            Emit("  -help              Show this help message.");
            Emit("");
            Emit("Examples:");
            Emit("  MinttiCLI.exe -list");
            Emit("  MinttiCLI.exe -connect -mac AA:BB:CC:DD:EE:FF");
            Emit("");
            Emit("Data Format (Stdout):");
            Emit("  List:        DATA:ITEM index={n} name=\"{name}\" mac=\"{mac}\"");
            Emit("  Stream:      DATA:STREAM type=audio data=[...]");
            Emit("  Heart Rate:  DATA:STREAM type=heartrate value={int}");
            Emit("  Status:      DATA:STATUS message=\"...\"");
            Emit("  Error:       DATA:ERROR code={code} message=\"...\"");
        }

        static void PrintError(string code, string message)
        {
            Emit($"DATA:ERROR code={code} message=\"{message}\"");
        }

        static void PrintOk(string command, string extra = "")
        {
            string output = $"DATA:OK command={command}";
            if (!string.IsNullOrEmpty(extra)) output += " " + extra;
            Emit(output);
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
            Emit("DATA:STATUS message=\"Scanning for 5 seconds...\"");
            await Task.Delay(5000);
            ble.StopBleDeviceWatcher();

            DeviceInfo[] found;
            lock (_devices)
            {
                found = _devices.ToArray();
            }

            Emit($"DATA:LIST count={found.Length}");
            for (int i = 0; i < found.Length; i++)
            {
                Emit($"DATA:ITEM index={i} name=\"{found[i].Name}\" mac=\"{found[i].Mac}\"");
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
            Emit("DATA:STATUS message=\"Scanning for target device...\"");

            // Create a FRESH result BEFORE scanning so a real CONNECTED can resolve it, but
            // reset again right before connecting so transient scan-phase messages can't
            // pre-latch the outcome.
            _connectResult = new TaskCompletionSource<bool>();
            ble.StartBleDeviceWatcher();

            DeviceInfo target = null;
            var scanDeadline = DateTime.UtcNow.AddSeconds(15);
            while (DateTime.UtcNow < scanDeadline)
            {
                lock (_devices)
                {
                    target = _devices.FirstOrDefault(d => string.Equals(d.Mac, mac, StringComparison.OrdinalIgnoreCase));
                }
                if (target != null)
                {
                    break;
                }
                await Task.Delay(300);
            }

            if (target == null)
            {
                ble.StopBleDeviceWatcher();
                PrintError("DEVICE_NOT_FOUND", "Device " + mac + " was not found during scan. Make sure it is powered on and in range.");
                return 1;
            }

            // Use the EXACT MAC string the SDK reported during discovery (its internal
            // cache is keyed on this), not the raw command-line arg. The GUI does the same.
            string targetMac = target.Mac;
            Log($"[SDK] Connecting to discovered device name=\"{target.Name}\" mac=\"{targetMac}\"");
            Emit("DATA:STATUS message=\"Device found, connecting...\"");

            // IMPORTANT: Connect while the watcher is STILL RUNNING. Stopping the watcher
            // evicts the SDK's freshly-discovered device object, after which ConnectByMac
            // has nothing to connect to and silently reports no status. So we connect first
            // and only stop the watcher once the connection is established.
            //
            // Retry a few times WITHOUT disposing (disposing + re-wiring would double-
            // subscribe the event handlers). A stale/half-open connection left by a previous
            // run often fails on the first attempt but succeeds on a retry once Windows
            // drops the cached connection.
            bool connected = false;
            const int maxAttempts = 3;
            for (int attempt = 1; attempt <= maxAttempts && !connected; attempt++)
            {
                Log($"[SDK] connect attempt {attempt}/{maxAttempts}");
                _connectResult = new TaskCompletionSource<bool>();
                ble.ConnectByMac(targetMac);

                // Non-blocking wait so the STA message pump keeps delivering SDK callbacks.
                var finished = await Task.WhenAny(_connectResult.Task, Task.Delay(15000));
                connected = finished == _connectResult.Task && _connectResult.Task.Result;

                if (!connected && attempt < maxAttempts)
                {
                    Log("[SDK] attempt failed, waiting before retry...");
                    await Task.Delay(2500); // let the BLE stack drop the stale connection
                }
            }

            // Now that we're connected (or gave up), stop scanning.
            ble.StopBleDeviceWatcher();

            if (!connected)
            {
                PrintError("CONNECT_FAILED",
                    "Failed to connect to device " + targetMac +
                    " after " + maxAttempts + " attempts. If this persists, power-cycle the stethoscope.");
                return 1;
            }

            PrintOk("connect", $"mac={targetMac}");

            _streaming = true;
            ble.StartMeasure();
            Emit("DATA:STATUS message=\"Streaming started. Press Ctrl+C to stop.\"");

            var tcs = new TaskCompletionSource<bool>();
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                // TrySetResult: a second Ctrl+C (or a race) must not throw
                // "attempt to transition a task to a final state when it had already completed".
                tcs.TrySetResult(true);
            };

            await tcs.Task;

            _streaming = false;
            Emit("DATA:STATUS message=\"Stopping and disconnecting...\"");
            ble.StopMeasure();
            ble.Dispose();
            PrintOk("stop");
            return 0;
        }
    }
}


using mintti_sdk.ble.bean;
using mintti_sdk.ble.constants;
using mintti_sdk.ble.manager;
using MinttiSDK.Utils;
using NAudio.Wave;
using Sunny.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MinttiSDK
{
    public partial class Form1 : UIForm
    {
        WaveOut waveOut;            //播放器
        BufferedWaveProvider bufferedWaveProvider;       //5s缓存区
        private Queue<double> dataQueue = new Queue<double>(500);
        private int num = 5;//每次删除增加几个点
        SynchronizationContext _syncContext = null;
        EchoMode _mode = EchoMode.MODE_BELL_ECHO;
        public Form1()
        {
            InitializeComponent();
            MinttiBle.GetInstance.DeviceWatcherChanged += _ble_DeviceWatcherChanged;
            MinttiBle.GetInstance.MessageChanged += _ble_MessageChanged;
            MinttiBle.GetInstance.DataCallback += Form1_DataCallback;
            MinttiBle.GetInstance.ModelSwitchback += Form1_ModelSwitchback;
            MinttiBle.GetInstance.PowerCallback += GetInstance_PowerCallback;
            MinttiBle.GetInstance.HeartRateCallback += GetInstance_HeartRateCallback;
            MinttiBle.GetInstance.DataLoseCallback += GetInstance_DataLoseCallback;
            MinttiBle.GetInstance.PressTooBigCallback += GetInstance_PressTooBigCallback;
            _syncContext = SynchronizationContext.Current;
            InitChart();
            NaudioInit();
            switchBtnStatus(false);
        }
        private void switchBtnStatus(bool status)
        {
            btMeasure.Enabled = status;
            btModelSwitch.Enabled = status;
            btReadPower.Enabled = status;
        }
        private long _time = 0;
        private int count = 0;
        private void GetInstance_PressTooBigCallback()
        {
            var curTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            count++;
            //Console.WriteLine("按压力度过大 :{0}",curTime);
            Action a = () =>
            {

                if (count % 8 == 0)
                {
                    ShowWarningTip("Excessive pressure");
                }

            };
            Invoke(a);
            _time = curTime;

        }

        //数据丢失回调
        private void GetInstance_DataLoseCallback()
        {
            //Console.WriteLine("data lost");
            Action a = () =>
            {
                ShowWarningNotifier("loss of data");
            };
            Invoke(a);
        }

        public void NaudioInit()
        {
            waveOut = new WaveOut();
            WaveFormat wf = new WaveFormat(8000, 1);
            bufferedWaveProvider = new BufferedWaveProvider(wf);
            waveOut.Init(bufferedWaveProvider);
            waveOut.Play();
        }
        /// </summary>
        /// <param name="data">要填入的数据</param>
        /// <param name="position">数据的起始位置</param>
        /// <param name="len">数据的长度</param>
        public void addDataToBufferedWaveProvider(short[] data, int position, int len)
        {
            //Console.WriteLine("数据={0}",data[0]);
            byte[] byteArray = ShortArray2ByteArray(data);
            bufferedWaveProvider.AddSamples(byteArray, position, byteArray.Length);
            _syncContext.Post(DealData, data);
            FileUtil.WritePcmData(data);
        }
        private void DealData(Object data)
        {
            short[] datas = (short[])data;
            for (int i = 0; i < datas.Length; i += 40)
            {
                UpdateQueueValue(datas[i]);
            }
            RefleData();
        }

        //更新队列中的值
        private void UpdateQueueValue(short data)
        {
            if (dataQueue.Count > 500)
            {
                //先出列
                for (int i = 0; i < num; i++)
                {
                    dataQueue.Dequeue();
                }
            }
            dataQueue.Enqueue(data);
        }
        private void RefleData()
        {
            this.chart2.Series[0].Points.Clear();
            for (int i = 0; i < dataQueue.Count; i++)
            {
                this.chart2.Series[0].Points.AddXY((i + 1), dataQueue.ElementAt(i));
            }
        }
        //电量改变回调
        private void GetInstance_PowerCallback(int power)
        {
            Action<int> a = (p) => { lbPower.Text = $"The current power is:{p}"; };
            Invoke(a, power);
        }
        //模式改变回调
        private void Form1_ModelSwitchback(EchoMode mode)
        {
            Action<EchoMode> a = (m) => {

                lbModel.Text = m == EchoMode.MODE_BELL_ECHO ? "Heart sound pattern" : "Lung sound pattern";
            };
            Invoke(a, mode);
        }

        //听诊数据回调
        private void Form1_DataCallback(short[] data)
        {
            //Console.WriteLine("数据0={0}", data[0]);
            addDataToBufferedWaveProvider(data, 0, data.Length);
        }

        private void GetInstance_HeartRateCallback(int hr)
        {
            Action<int> a = h => { lbBpm.Text = "" + hr; };
            BeginInvoke(a, hr);

        }

        private void _ble_MessageChanged(MsgType type, string message, byte[] data = null)
        {
            if (type == MsgType.ConnectStatus)
            {
                Action<string> status = HandleConnectMessage;
                Invoke(status, message);
            }
            else
            {
                Action<string> a = HandleMessage;
                Invoke(a, message);
            }

        }

        //扫描到的设备信息回调
        private void _ble_DeviceWatcherChanged(DeviceInfo deviceInfo)
        {
            Action<string> a = HandleMessage;
            Invoke(a, deviceInfo.Name + "_" + deviceInfo.Mac);
        }

        private void HandleMessage(string str)
        {
            lbList.Items.Add(str);
        }
        private void HandleConnectMessage(string str)
        {
            lbList.Items.Add(str);
            if (str.Equals(MinttiConstants.CONNECTION_FAILED))
            {
                MessageBox.Show("Connection failed!");
                switchBtnStatus(false);
                btConnect.Text = "Connect the device";
            }
            else if (str.Equals(MinttiConstants.CONNECTED))
            {
                switchBtnStatus(true);
            }
            else if (str == MinttiConstants.DISCONNECT)
            {
                MessageBox.Show("Disconnect!");
                switchBtnStatus(false);
                btConnect.Text = "Connect the device";
            }
        }
        private async Task<bool> CheckBle()
        {
            bool isEnable = await MinttiBle.GetInstance.GetBleEnableAsync();
            bool isOpenBle = await MinttiBle.GetInstance.IsOpenBleAsync();

            if (!isEnable)
            {
                MessageBox.Show("Bluetooth is not available！");
                return false;
            }
            if (!isOpenBle)
            {
                MessageBox.Show("Please turn on Bluetooth first!");
                return false;
            }
            return true;
        }

        //开始和停止扫描
        private async void btScan_Click(object sender, EventArgs e)
        {
            bool isOpen = await CheckBle();
            if (!isOpen)
            {
                return;
            }

            if (btScan.Text.Equals("Start scanning"))
            {
                btScan.Text = "Stop scanning";
                MinttiBle.GetInstance.StartBleDeviceWatcher();
            }
            else
            {
                btScan.Text = "Start scanning";
                MinttiBle.GetInstance.StopBleDeviceWatcher();
            }
        }
        //连接和断开连接
        private async void btConnect_Click(object sender, EventArgs e)
        {
            bool isOpen = await CheckBle();
            if (!isOpen)
            {
                return;
            }
            if (btConnect.Text.Equals("Connect the device"))
            {
                if (btScan.Text.Equals("Stop scanning"))
                {
                    btScan.Text = "Start scanning";
                    MinttiBle.GetInstance.StopBleDeviceWatcher();
                }
                if (lbList.SelectedItem == null)
                {
                    MessageBox.Show("Please select a device to connect to");
                    return;
                }
                btConnect.Text = "Disconnect";
                string str = lbList.SelectedItem.ToString();
                if (str == null)
                {
                    MessageBox.Show("Please select the device to be connected");
                    return;
                }
                switchBtnStatus(false);
                string[] info = str.Split('_');
                MinttiBle.GetInstance.ConnectByMac(info[1]);
            }
            else
            {
                btConnect.Text = "Connect the device";
                await disConnect();

            }
        }
        private async Task disConnect()
        {
            await Task.Factory.StartNew(() =>
            {
                MinttiBle.GetInstance.Dispose();

            });
        }
        //开始和停止测量
        private async void btMeasure_Click(object sender, EventArgs e)
        {
            bool isOpen = await CheckBle();
            if (!isOpen)
            {
                return;
            }
            if (btMeasure.Text.Equals("Start measuring"))
            {
                btMeasure.Text = "End measurement";
                FileUtil.CreateRecordFile();
                MinttiBle.GetInstance.StartMeasure();
                //// 创建一个新的任务，指定要执行的方法
                //Task task = Task.Factory.StartNew(() =>
                //{

                //});
            }
            else
            {
                btMeasure.Text = "Start measuring";
                MinttiBle.GetInstance.StopMeasure();
                FileUtil.PcmToWav();
            }
        }
        //读取电量
        private async void btReadPower_ClickAsync(object sender, EventArgs e)
        {
            bool isOpen = await CheckBle();
            if (!isOpen)
            {
                return;
            }
            string power = await MinttiBle.GetInstance.GetPowerAsync();
            lbPower.Text = "The current power is:" + power;
        }
        //切换模式
        private async void btModelSwitch_ClickAsync(object sender, EventArgs e)
        {
            bool isOpen = await CheckBle();
            if (!isOpen)
            {
                return;
            }
            if (_mode == EchoMode.MODE_BELL_ECHO)
            {
                await MinttiBle.GetInstance.SwitchModelAsync(EchoMode.MODE_DIAPHRAGM_ECHO);
                _mode = EchoMode.MODE_DIAPHRAGM_ECHO;
                lbModel.Text = "Lung sound pattern";
            }
            else
            {
                await MinttiBle.GetInstance.SwitchModelAsync(EchoMode.MODE_BELL_ECHO);
                _mode = EchoMode.MODE_BELL_ECHO;
                lbModel.Text = "Heart sound pattern";
            }

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            lbModel.Text = _mode == EchoMode.MODE_BELL_ECHO ? "Heart sound pattern" : "Lung sound pattern";
            // lbModel.Text = MinttiBle.Sum(2, 4).ToString();
            //lbModel.Text = System.AppDomain.CurrentDomain.BaseDirectory;
        }

        public static byte[] ShortArray2ByteArray(short[] paramShort)
        {
            byte[] arrayOfByte = new byte[paramShort.Length * 2];
            for (int i = 0; i < paramShort.Length; i++)
            {
                arrayOfByte[2 * i] = BitConverter.GetBytes(paramShort[i])[0];
                arrayOfByte[1 + 2 * i] = BitConverter.GetBytes(paramShort[i])[1];

            }
            return arrayOfByte;
        }

        #region 内存回收
        [DllImport("kernel32.dll", EntryPoint = "SetProcessWorkingSetSize")]
        public static extern int SetProcessWorkingSetSize(IntPtr process, int minSize, int maxSize);
        /// <summary>
        /// 释放内存
        /// </summary>
        public static void ClearMemory()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                SetProcessWorkingSetSize(System.Diagnostics.Process.GetCurrentProcess().Handle, -1, -1);
            }
        }

        #endregion

        private void timer1_Tick(object sender, EventArgs e)
        {
            ClearMemory();
        }
    }
}

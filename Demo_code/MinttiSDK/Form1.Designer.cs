
using System.Drawing;
using System.Windows.Forms.DataVisualization.Charting;

namespace MinttiSDK
{
    partial class Form1
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend1 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.Series series1 = new System.Windows.Forms.DataVisualization.Charting.Series();
            this.btScan = new System.Windows.Forms.Button();
            this.btConnect = new System.Windows.Forms.Button();
            this.btMeasure = new System.Windows.Forms.Button();
            this.btReadPower = new System.Windows.Forms.Button();
            this.btModelSwitch = new System.Windows.Forms.Button();
            this.lbList = new System.Windows.Forms.ListBox();
            this.lbPower = new System.Windows.Forms.Label();
            this.lbModel = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.lbBpm = new System.Windows.Forms.Label();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.gridControl1 = new MinttiSDK.GridControl();
            this.chart2 = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.gridControl1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.chart2)).BeginInit();
            this.SuspendLayout();
            // 
            // btScan
            // 
            this.btScan.Location = new System.Drawing.Point(58, 52);
            this.btScan.Name = "btScan";
            this.btScan.Size = new System.Drawing.Size(177, 44);
            this.btScan.TabIndex = 0;
            this.btScan.Text = "Start scanning";
            this.btScan.UseVisualStyleBackColor = true;
            this.btScan.Click += new System.EventHandler(this.btScan_Click);
            // 
            // btConnect
            // 
            this.btConnect.Location = new System.Drawing.Point(253, 52);
            this.btConnect.Name = "btConnect";
            this.btConnect.Size = new System.Drawing.Size(175, 44);
            this.btConnect.TabIndex = 1;
            this.btConnect.Text = "Connect the device";
            this.btConnect.UseVisualStyleBackColor = true;
            this.btConnect.Click += new System.EventHandler(this.btConnect_Click);
            // 
            // btMeasure
            // 
            this.btMeasure.Location = new System.Drawing.Point(58, 125);
            this.btMeasure.Name = "btMeasure";
            this.btMeasure.Size = new System.Drawing.Size(177, 44);
            this.btMeasure.TabIndex = 2;
            this.btMeasure.Text = "Start measuring";
            this.btMeasure.UseVisualStyleBackColor = true;
            this.btMeasure.Click += new System.EventHandler(this.btMeasure_Click);
            // 
            // btReadPower
            // 
            this.btReadPower.Location = new System.Drawing.Point(253, 125);
            this.btReadPower.Name = "btReadPower";
            this.btReadPower.Size = new System.Drawing.Size(175, 44);
            this.btReadPower.TabIndex = 3;
            this.btReadPower.Text = "Read battery";
            this.btReadPower.UseVisualStyleBackColor = true;
            this.btReadPower.Click += new System.EventHandler(this.btReadPower_ClickAsync);
            // 
            // btModelSwitch
            // 
            this.btModelSwitch.Location = new System.Drawing.Point(58, 195);
            this.btModelSwitch.Name = "btModelSwitch";
            this.btModelSwitch.Size = new System.Drawing.Size(177, 44);
            this.btModelSwitch.TabIndex = 4;
            this.btModelSwitch.Text = "Mode switch";
            this.btModelSwitch.UseVisualStyleBackColor = true;
            this.btModelSwitch.Click += new System.EventHandler(this.btModelSwitch_ClickAsync);
            // 
            // lbList
            // 
            this.lbList.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lbList.FormattingEnabled = true;
            this.lbList.ItemHeight = 22;
            this.lbList.Location = new System.Drawing.Point(554, 41);
            this.lbList.Name = "lbList";
            this.lbList.Size = new System.Drawing.Size(461, 202);
            this.lbList.TabIndex = 6;
            // 
            // lbPower
            // 
            this.lbPower.AutoSize = true;
            this.lbPower.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lbPower.Location = new System.Drawing.Point(246, 190);
            this.lbPower.Name = "lbPower";
            this.lbPower.Size = new System.Drawing.Size(182, 22);
            this.lbPower.TabIndex = 8;
            this.lbPower.Text = "The current power is:";
            // 
            // lbModel
            // 
            this.lbModel.AutoSize = true;
            this.lbModel.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lbModel.Location = new System.Drawing.Point(246, 220);
            this.lbModel.Name = "lbModel";
            this.lbModel.Size = new System.Drawing.Size(175, 22);
            this.lbModel.TabIndex = 9;
            this.lbModel.Text = "Heart sound pattern";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label1.Location = new System.Drawing.Point(246, 247);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(160, 22);
            this.label1.TabIndex = 10;
            this.label1.Text = "Current heart rate:";
            // 
            // lbBpm
            // 
            this.lbBpm.AutoSize = true;
            this.lbBpm.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lbBpm.Location = new System.Drawing.Point(415, 247);
            this.lbBpm.Name = "lbBpm";
            this.lbBpm.Size = new System.Drawing.Size(30, 22);
            this.lbBpm.TabIndex = 11;
            this.lbBpm.Text = "00";
            // 
            // timer1
            // 
            this.timer1.Enabled = true;
            this.timer1.Interval = 2000;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // gridControl1
            // 
            this.gridControl1.BackColor = System.Drawing.Color.White;
            this.gridControl1.Controls.Add(this.chart2);
            this.gridControl1.Location = new System.Drawing.Point(74, 341);
            this.gridControl1.Name = "gridControl1";
            this.gridControl1.Size = new System.Drawing.Size(941, 250);
            this.gridControl1.TabIndex = 12;
            // 
            // chart2
            // 
            this.chart2.BackColor = System.Drawing.Color.Transparent;
            chartArea1.Name = "ChartArea1";
            this.chart2.ChartAreas.Add(chartArea1);
            legend1.Name = "Legend1";
            this.chart2.Legends.Add(legend1);
            this.chart2.Location = new System.Drawing.Point(-28, 0);
            this.chart2.Name = "chart2";
            series1.ChartArea = "ChartArea1";
            series1.Legend = "Legend1";
            series1.Name = "Series1";
            this.chart2.Series.Add(series1);
            this.chart2.Size = new System.Drawing.Size(996, 250);
            this.chart2.TabIndex = 0;
            this.chart2.Text = "chart2";
            // 
            // Form1
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(1084, 640);
            this.Controls.Add(this.gridControl1);
            this.Controls.Add(this.lbBpm);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.lbModel);
            this.Controls.Add(this.lbPower);
            this.Controls.Add(this.lbList);
            this.Controls.Add(this.btModelSwitch);
            this.Controls.Add(this.btReadPower);
            this.Controls.Add(this.btMeasure);
            this.Controls.Add(this.btConnect);
            this.Controls.Add(this.btScan);
            this.Name = "Form1";
            this.Text = "MinttiDemo";
            this.ZoomScaleRect = new System.Drawing.Rectangle(15, 15, 1084, 640);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.gridControl1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.chart2)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        /// <summary>
        /// 初始化图表
        /// </summary>
        private void InitChart()
        {
            //定义图表区域
            this.chart2.ChartAreas.Clear();
            ChartArea chartArea1 = new ChartArea("C1");
            this.chart2.ChartAreas.Add(chartArea1);
            //定义存储和显示点的容器
            this.chart2.Series.Clear();
            Series series1 = new Series("S1");
            series1.ChartArea = "C1";
            this.chart2.Series.Add(series1);
            this.chart2.BackColor = Color.Transparent;
            //设置图表显示样式
            this.chart2.ChartAreas[0].AxisY.Minimum = -32768;
            this.chart2.ChartAreas[0].AxisY.Maximum = 32767;
            this.chart2.ChartAreas[0].AxisX.Interval = 1;
            this.chart2.ChartAreas[0].AxisY.Interval = 1;
            this.chart2.ChartAreas[0].AxisX.Enabled = AxisEnabled.False;
            this.chart2.ChartAreas[0].AxisY.Enabled = AxisEnabled.False;
            this.chart2.ChartAreas[0].BackColor = Color.Transparent;
            //设置图表显示样式
            this.chart2.Series[0].Color = Color.Green;
            this.chart2.Series[0].ChartType = SeriesChartType.Line;
            this.chart2.Series[0].Points.Clear();
            this.chart2.Legends.Clear();//去除右边的Series

        }

        #endregion

        private System.Windows.Forms.Button btScan;
        private System.Windows.Forms.Button btConnect;
        private System.Windows.Forms.Button btMeasure;
        private System.Windows.Forms.Button btReadPower;
        private System.Windows.Forms.Button btModelSwitch;
        private System.Windows.Forms.ListBox lbList;
        private System.Windows.Forms.Label lbPower;
        private System.Windows.Forms.Label lbModel;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label lbBpm;
        private GridControl gridControl1;
        private Chart chart2;
        private System.Windows.Forms.Timer timer1;
    }
}


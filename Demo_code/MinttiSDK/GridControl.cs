using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MinttiSDK
{
    public partial class GridControl : Panel
    {
        float intervalX;
        float intervalY;
        public GridControl()
        {
            InitializeComponent();
            intervalX = 10;
            intervalY = 10;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            //Console.WriteLine("绘制");
            using (Graphics g = e.Graphics)
            {
                //设置抗锯齿
                g.SmoothingMode = SmoothingMode.AntiAlias;
                Pen p = new Pen(Color.FromArgb(255, 241, 241, 241));

                //画竖向网格线
                for (int i = 0; i < Width; i++)
                {
                    g.DrawLine(p, intervalX * i, 0, intervalX * i, Height);
                }
                //画横向网格线
                for (int i = 0; i < Height; i++)
                {
                    g.DrawLine(p, 0, intervalY * i, Width, intervalY * i);
                }
            }
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {

        }
    }
}

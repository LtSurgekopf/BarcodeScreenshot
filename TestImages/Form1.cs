using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TestImages
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

            var breakCoordinateLabel = new Func<int, string>(c =>
            {
                StringBuilder sb = new StringBuilder();
                foreach (var ch in c.ToString())
                {
                    sb.AppendLine(ch.ToString());
                }

                return sb.ToString();
            });

            var pxWidth = 1920;
            var pxHeight = 1080;
        
            var step = 20;

            var BrushList = new[] { Brushes.Black, Brushes.Red, Brushes.Blue, Brushes.DarkOliveGreen, Brushes.Violet, Brushes.Orange, Brushes.Aqua, Brushes.GreenYellow, Brushes.Tomato, Brushes.PeachPuff};
            var PenList = new[] { Pens.Black, Pens.Red, Pens.Blue, Pens.DarkOliveGreen, Pens.Violet, Pens.Orange, Pens.Aqua, Pens.GreenYellow, Pens.Tomato, Pens.PeachPuff };

            var brushIndex = 0;

            var fontSize = 7;

            Bitmap bitmap = new Bitmap(pxWidth, pxHeight);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.White);
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                

                for (var x = step; x <= pxWidth; x += step)
                {
                    var penLine = PenList[brushIndex];
                    var brushText = BrushList[brushIndex];

                    var pointStart = new Point(x, 0);
                    var pointEnd = new Point(x, pxHeight);
                    g.DrawLine(penLine, pointStart, pointEnd);
                    g.DrawString(breakCoordinateLabel(x), new Font("Courier", fontSize), brushText, new PointF(x+1, 1));
                    g.DrawString(breakCoordinateLabel(x), new Font("Courier", fontSize), brushText, new PointF(x+1, pxHeight / 2f));

                    brushIndex = (brushIndex + 1) % BrushList.Length;
                }

                brushIndex = 0;
                for (var y = step; y <= pxHeight; y += step)
                {
                    var penLine = PenList[brushIndex];
                    var brushText = BrushList[brushIndex];

                    var pointStart = new Point(0, y);
                    var pointEnd = new Point(pxWidth, y);
                    g.DrawLine(penLine, pointStart, pointEnd);
                    g.DrawString(y.ToString(), new Font("Courier", fontSize), brushText, new PointF(1, y+1));
                    g.DrawString(y.ToString(), new Font("Courier", fontSize), brushText, new PointF(pxWidth / 2f, y+1));

                    brushIndex = (brushIndex + 1) % BrushList.Length;
                }
            }
            bitmap.Save($"testImage-{pxWidth}x{pxHeight}.png", ImageFormat.Png);
            pictureBox1.Image = bitmap;

            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;
        }
    }
}

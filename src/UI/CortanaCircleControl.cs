using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace BatteryAlert.UI
{
    public class CortanaCircleControl : Control
    {
        private int _percent = 95;
        private bool _isPluggedIn = true;
        private bool _isCharging = true;
        private Color _accentColor = Color.FromArgb(0, 216, 246);
        private Color _trackColor = Color.FromArgb(32, 34, 42);

        public int Percent
        {
            get { return _percent; }
            set
            {
                int val = Math.Max(0, Math.Min(100, value));
                if (_percent != val)
                {
                    _percent = val;
                    Invalidate();
                }
            }
        }

        public bool IsPluggedIn
        {
            get { return _isPluggedIn; }
            set
            {
                if (_isPluggedIn != value)
                {
                    _isPluggedIn = value;
                    Invalidate();
                }
            }
        }

        public bool IsCharging
        {
            get { return _isCharging; }
            set
            {
                if (_isCharging != value)
                {
                    _isCharging = value;
                    Invalidate();
                }
            }
        }

        public CortanaCircleControl()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Size = new Size(170, 170);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            float size = Math.Min(Width, Height);
            float padding = 18f;
            float diameter = size - (padding * 2f);
            float x = (Width - diameter) / 2f;
            float y = (Height - diameter) / 2f;
            RectangleF rect = new RectangleF(x, y, diameter, diameter);

            // Layered subtle ambient glow when plugged in
            if (_isPluggedIn)
            {
                using (Pen outerGlow = new Pen(Color.FromArgb(16, 0, 216, 246), 16f))
                {
                    g.DrawEllipse(outerGlow, rect);
                }
                using (Pen innerGlow = new Pen(Color.FromArgb(32, 0, 216, 246), 10f))
                {
                    g.DrawEllipse(innerGlow, rect);
                }
            }

            // Background circle track
            using (Pen trackPen = new Pen(_trackColor, 6f))
            {
                g.DrawEllipse(trackPen, rect);
            }

            // Active Cortana arc
            float sweepAngle = (_percent / 100f) * 360f;
            if (sweepAngle > 0.5f)
            {
                using (Pen arcPen = new Pen(_accentColor, 6f))
                {
                    arcPen.StartCap = LineCap.Round;
                    arcPen.EndCap = LineCap.Round;
                    g.DrawArc(arcPen, rect, -90f, sweepAngle);
                }
            }

            // Percentage Text
            string percentText = string.Format("{0}%", _percent);
            using (Font numFont = new Font("Segoe UI", 28f, FontStyle.Bold, GraphicsUnit.Point))
            {
                SizeF numSize = g.MeasureString(percentText, numFont);
                float numX = (Width - numSize.Width) / 2f;
                float numY = (Height - numSize.Height) / 2f - 9f;

                using (SolidBrush numBrush = new SolidBrush(Color.FromArgb(248, 248, 252)))
                {
                    g.DrawString(percentText, numFont, numBrush, numX, numY);
                }
            }

            // Status Subtext & Bolt Icon
            string statusText = _isPluggedIn ? (_isCharging ? "CHARGING" : "PLUGGED IN") : "ON BATTERY";
            using (Font statusFont = new Font("Segoe UI Semibold", 8.2f, FontStyle.Regular, GraphicsUnit.Point))
            {
                SizeF statusSize = g.MeasureString(statusText, statusFont);
                float boltWidth = _isPluggedIn ? 10f : 0f;
                float totalWidth = statusSize.Width + boltWidth;
                float startX = (Width - totalWidth) / 2f;
                float statusY = (Height / 2f) + 24f;

                if (_isPluggedIn)
                {
                    // Draw crisp vector lightning bolt
                    DrawBolt(g, startX, statusY + 1.5f, 7.5f, 10.5f, _accentColor);
                    startX += 11f;
                }

                Color statusColor = _isPluggedIn ? _accentColor : Color.FromArgb(145, 145, 155);
                using (SolidBrush statusBrush = new SolidBrush(statusColor))
                {
                    g.DrawString(statusText, statusFont, statusBrush, startX, statusY);
                }
            }
        }

        private static void DrawBolt(Graphics g, float x, float y, float w, float h, Color color)
        {
            PointF[] bolt = new PointF[]
            {
                new PointF(x + w * 0.58f, y),
                new PointF(x, y + h * 0.54f),
                new PointF(x + w * 0.46f, y + h * 0.54f),
                new PointF(x + w * 0.32f, y + h),
                new PointF(x + w, y + h * 0.42f),
                new PointF(x + w * 0.56f, y + h * 0.42f)
            };

            using (SolidBrush brush = new SolidBrush(color))
            {
                g.FillPolygon(brush, bolt);
            }
        }
    }
}

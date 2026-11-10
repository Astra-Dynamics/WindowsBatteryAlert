using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace BatteryAlert.UI
{
    public class CortanaCircleControl : Control
    {
        private int _percent = 95;
        private bool _isPluggedIn = true;
        private bool _isCharging = true;
        private Color _accentColor = Color.FromArgb(0, 216, 246);
        private Color _trackColor = Color.FromArgb(32, 32, 36);

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
            Size = new Size(160, 160);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            int size = Math.Min(Width, Height);
            int padding = 16;
            int diameter = size - (padding * 2);
            int x = (Width - diameter) / 2;
            int y = (Height - diameter) / 2;
            Rectangle rect = new Rectangle(x, y, diameter, diameter);

            // Subtle glow when plugged in
            if (_isPluggedIn)
            {
                using (Pen glowPen = new Pen(Color.FromArgb(22, 0, 216, 246), 14f))
                {
                    g.DrawEllipse(glowPen, rect);
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
            using (Font numFont = new Font("Segoe UI", 26f, FontStyle.Bold))
            {
                SizeF numSize = g.MeasureString(percentText, numFont);
                float numX = (Width - numSize.Width) / 2f;
                float numY = (Height - numSize.Height) / 2f - 8f;

                using (SolidBrush numBrush = new SolidBrush(Color.FromArgb(245, 245, 245)))
                {
                    g.DrawString(percentText, numFont, numBrush, numX, numY);
                }
            }

            // Status Subtext & Bolt Icon
            string statusText = _isPluggedIn ? (_isCharging ? "CHARGING" : "PLUGGED IN") : "ON BATTERY";
            using (Font statusFont = new Font("Segoe UI Semibold", 8f))
            {
                SizeF statusSize = g.MeasureString(statusText, statusFont);
                float totalWidth = statusSize.Width;
                float boltWidth = _isPluggedIn ? 10f : 0f;
                float startX = (Width - (totalWidth + boltWidth)) / 2f;
                float statusY = (Height / 2f) + 20f;

                if (_isPluggedIn)
                {
                    // Draw small lightning bolt
                    DrawBolt(g, startX, statusY + 1f, 8f, 11f, _accentColor);
                    startX += 11f;
                }

                Color statusColor = _isPluggedIn ? _accentColor : Color.FromArgb(140, 140, 145);
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
                new PointF(x + w * 0.55f, y),
                new PointF(x, y + h * 0.55f),
                new PointF(x + w * 0.45f, y + h * 0.55f),
                new PointF(x + w * 0.35f, y + h),
                new PointF(x + w, y + h * 0.42f),
                new PointF(x + w * 0.55f, y + h * 0.42f)
            };

            using (SolidBrush brush = new SolidBrush(color))
            {
                g.FillPolygon(brush, bolt);
            }
        }
    }
}

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace BatteryAlert.UI
{
    public class ThresholdChipControl : Control
    {
        private readonly int _threshold;
        private readonly bool _isLocked;
        private bool _isDeleteHovered;
        private RectangleF _deleteRect;

        public event Action<int> DeleteRequested;

        public int Threshold { get { return _threshold; } }
        public bool IsLocked { get { return _isLocked; } }

        public ThresholdChipControl(int threshold, bool isLocked)
        {
            _threshold = threshold;
            _isLocked = isLocked;

            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.SupportsTransparentBackColor, true);

            BackColor = Color.Transparent;
            Font = new Font("Segoe UI", 9f, FontStyle.Regular, GraphicsUnit.Point);
            Size = new Size(68, 30);
            Cursor = Cursors.Default;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!_isLocked)
            {
                bool wasHovered = _isDeleteHovered;
                _isDeleteHovered = _deleteRect.Contains(e.Location);
                if (wasHovered != _isDeleteHovered)
                {
                    Cursor = _isDeleteHovered ? Cursors.Hand : Cursors.Default;
                    Invalidate();
                }
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_isDeleteHovered)
            {
                _isDeleteHovered = false;
                Cursor = Cursors.Default;
                Invalidate();
            }
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (!_isLocked && e.Button == MouseButtons.Left && _deleteRect.Contains(e.Location))
            {
                Action<int> handler = DeleteRequested;
                if (handler != null)
                {
                    handler(_threshold);
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = GetRoundedRectangle(rect, 8))
            {
                // Background
                using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(24, 25, 30)))
                {
                    g.FillPath(bgBrush, path);
                }

                // Border
                Color borderColor = _isLocked ? Color.FromArgb(45, 46, 54) : Color.FromArgb(60, 62, 74);
                using (Pen borderPen = new Pen(borderColor, 1.1f))
                {
                    g.DrawPath(borderPen, path);
                }
            }

            // Percentage Text
            string text = string.Format("{0}%", _threshold);
            RectangleF textRect = new RectangleF(6f, 0f, Width - 26f, Height);
            using (StringFormat sf = new StringFormat())
            {
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;
                using (SolidBrush textBrush = new SolidBrush(Color.FromArgb(235, 238, 245)))
                {
                    g.DrawString(text, Font, textBrush, textRect, sf);
                }
            }

            // Icon area on the right
            _deleteRect = new RectangleF(Width - 22f, (Height - 16f) / 2f, 16f, 16f);

            if (_isLocked)
            {
                // Crisp Vector Padlock
                DrawVectorLock(g, _deleteRect.X + 3f, _deleteRect.Y + 2f, 10f, 11f, Color.FromArgb(130, 135, 145));
            }
            else
            {
                // Crisp Close "×"
                Color crossColor = _isDeleteHovered ? Color.FromArgb(255, 90, 90) : Color.FromArgb(145, 150, 165);
                using (Font crossFont = new Font("Segoe UI", 9f, FontStyle.Bold))
                using (StringFormat sf = new StringFormat())
                {
                    sf.Alignment = StringAlignment.Center;
                    sf.LineAlignment = StringAlignment.Center;
                    using (SolidBrush brush = new SolidBrush(crossColor))
                    {
                        g.DrawString("×", crossFont, brush, _deleteRect, sf);
                    }
                }
            }
        }

        private static void DrawVectorLock(Graphics g, float x, float y, float w, float h, Color color)
        {
            // Padlock arch shackle
            using (Pen pen = new Pen(color, 1.4f))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                g.DrawArc(pen, x + 1.5f, y, w - 3f, 6.5f, 180f, 180f);
            }

            // Padlock body
            RectangleF bodyRect = new RectangleF(x, y + 4.5f, w, h - 4.5f);
            using (GraphicsPath bodyPath = GetRoundedRectangleF(bodyRect, 2f))
            {
                using (SolidBrush brush = new SolidBrush(color))
                {
                    g.FillPath(brush, bodyPath);
                }
            }

            // Small keyhole dot
            using (SolidBrush keyBrush = new SolidBrush(Color.FromArgb(24, 25, 30)))
            {
                g.FillEllipse(keyBrush, x + (w / 2f) - 1f, y + 6.5f, 2f, 2f);
            }
        }

        private static GraphicsPath GetRoundedRectangle(Rectangle bounds, int radius)
        {
            return GetRoundedRectangleF(new RectangleF(bounds.X, bounds.Y, bounds.Width, bounds.Height), radius);
        }

        private static GraphicsPath GetRoundedRectangleF(RectangleF bounds, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float diameter = radius * 2f;
            RectangleF arc = new RectangleF(bounds.Location, new SizeF(diameter, diameter));

            path.AddArc(arc, 180f, 90f);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270f, 90f);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0f, 90f);
            arc.X = bounds.Left;
            path.AddArc(arc, 90f, 90f);
            path.CloseFigure();
            return path;
        }
    }
}

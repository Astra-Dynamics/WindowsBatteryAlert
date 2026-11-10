using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace BatteryAlert.UI
{
    public class ThresholdChipControl : Control
    {
        private readonly int _threshold;
        private readonly bool _isLocked;
        private bool _isDeleteHovered;
        private Rectangle _deleteRect;

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
            Font = new Font("Segoe UI", 8.5f, FontStyle.Regular);
            Size = new Size(62, 28);
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
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = GetRoundedRectangle(rect, 6))
            {
                // Background
                using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(24, 24, 28)))
                {
                    g.FillPath(bgBrush, path);
                }

                // Border
                Color borderColor = _isLocked ? Color.FromArgb(42, 42, 48) : Color.FromArgb(55, 55, 65);
                using (Pen borderPen = new Pen(borderColor, 1f))
                {
                    g.DrawPath(borderPen, path);
                }
            }

            // Text: "95%"
            string text = string.Format("{0}%", _threshold);
            Rectangle textRect = new Rectangle(6, 0, Width - 24, Height);
            TextRenderer.DrawText(g, text, Font, textRect, Color.FromArgb(235, 235, 235),
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter);

            // Icon area on the right
            _deleteRect = new Rectangle(Width - 20, (Height - 16) / 2, 16, 16);

            if (_isLocked)
            {
                // Draw small lock icon
                DrawLock(g, _deleteRect.X + 2, _deleteRect.Y + 2, 10, 11, Color.FromArgb(120, 120, 130));
            }
            else
            {
                // Draw small "✕" remove icon
                Color crossColor = _isDeleteHovered ? Color.FromArgb(255, 85, 85) : Color.FromArgb(140, 140, 150);
                using (Font crossFont = new Font("Segoe UI", 8f, FontStyle.Bold))
                {
                    TextRenderer.DrawText(g, "×", crossFont, _deleteRect, crossColor,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
            }
        }

        private static void DrawLock(Graphics g, int x, int y, int w, int h, Color color)
        {
            // Padlock shackle
            using (Pen pen = new Pen(color, 1.2f))
            {
                g.DrawArc(pen, x + 2, y, w - 4, 6, 180, 180);
            }
            // Padlock body
            using (SolidBrush brush = new SolidBrush(color))
            {
                g.FillRectangle(brush, x + 1, y + 4, w - 2, h - 4);
            }
        }

        private static GraphicsPath GetRoundedRectangle(Rectangle bounds, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = radius * 2;
            Rectangle arc = new Rectangle(bounds.Location, new Size(diameter, diameter));

            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace BatteryAlert.UI
{
    public class PillButton : Control
    {
        private bool _isHovered;
        private bool _isPressed;
        private bool _isActive;
        private Color _accentColor = Color.FromArgb(0, 216, 246);
        private Color _idleBackColor = Color.FromArgb(28, 28, 30);
        private Color _idleBorderColor = Color.FromArgb(50, 50, 54);
        private Color _activeTextColor = Color.FromArgb(16, 16, 18);
        private Color _idleTextColor = Color.FromArgb(220, 220, 220);
        private int _borderRadius = 14;

        public bool IsActive
        {
            get { return _isActive; }
            set { _isActive = value; Invalidate(); }
        }

        public Color AccentColor
        {
            get { return _accentColor; }
            set { _accentColor = value; Invalidate(); }
        }

        public Color IdleBackColor
        {
            get { return _idleBackColor; }
            set { _idleBackColor = value; Invalidate(); }
        }

        public Color IdleBorderColor
        {
            get { return _idleBorderColor; }
            set { _idleBorderColor = value; Invalidate(); }
        }

        public PillButton()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            Cursor = Cursors.Hand;
            Size = new Size(80, 28);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _isHovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _isHovered = false;
            _isPressed = false;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                _isPressed = true;
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _isPressed = false;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
            int radius = Math.Min(_borderRadius, rect.Height / 2);

            using (GraphicsPath path = GetRoundedRectangle(rect, radius))
            {
                Color bg;
                Color border;
                Color text;

                if (_isActive)
                {
                    bg = _isPressed ? Color.FromArgb(0, 180, 205) : (_isHovered ? Color.FromArgb(40, 230, 255) : _accentColor);
                    border = bg;
                    text = _activeTextColor;
                }
                else
                {
                    bg = _isPressed ? Color.FromArgb(42, 42, 46) : (_isHovered ? Color.FromArgb(36, 36, 40) : _idleBackColor);
                    border = _isHovered ? _accentColor : _idleBorderColor;
                    text = _isHovered ? Color.White : _idleTextColor;
                }

                using (SolidBrush brush = new SolidBrush(bg))
                {
                    g.FillPath(brush, path);
                }

                using (Pen pen = new Pen(border, 1.2f))
                {
                    g.DrawPath(pen, path);
                }

                TextRenderer.DrawText(g, Text, Font, rect, text,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
            }
        }

        private static GraphicsPath GetRoundedRectangle(Rectangle bounds, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = radius * 2;
            Rectangle arc = new Rectangle(bounds.Location, new Size(diameter, diameter));

            // Top left
            path.AddArc(arc, 180, 90);

            // Top right
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);

            // Bottom right
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);

            // Bottom left
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);

            path.CloseFigure();
            return path;
        }
    }
}

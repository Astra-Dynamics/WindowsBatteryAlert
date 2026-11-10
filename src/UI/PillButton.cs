using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace BatteryAlert.UI
{
    public class PillButton : Control
    {
        private bool _isHovered;
        private bool _isPressed;
        private bool _isActive;
        private bool _isOutlineStyle = false;
        private Color _accentColor = Color.FromArgb(0, 216, 246);
        private Color _idleBackColor = Color.FromArgb(25, 25, 30);
        private Color _idleBorderColor = Color.FromArgb(50, 50, 58);
        private Color _activeTextColor = Color.FromArgb(0, 225, 255);
        private Color _idleTextColor = Color.FromArgb(210, 210, 220);
        private int _borderRadius = 14;

        public bool IsActive
        {
            get { return _isActive; }
            set { _isActive = value; Invalidate(); }
        }

        public bool IsOutlineStyle
        {
            get { return _isOutlineStyle; }
            set { _isOutlineStyle = value; Invalidate(); }
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
            Font = new Font("Segoe UI", 8.5f, FontStyle.Regular);
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
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
            int radius = Math.Min(_borderRadius, rect.Height / 2);

            using (GraphicsPath path = GetRoundedRectangle(rect, radius))
            {
                Color bg;
                Color border;
                Color text;
                float borderWidth = 1.2f;

                if (_isOutlineStyle)
                {
                    // Modern Cortana / Fluent toggle style
                    if (_isActive)
                    {
                        bg = _isPressed ? Color.FromArgb(28, 42, 50) : (_isHovered ? Color.FromArgb(20, 36, 44) : Color.FromArgb(16, 26, 32));
                        border = _isHovered ? Color.FromArgb(40, 235, 255) : _accentColor;
                        borderWidth = 1.5f;
                        text = Color.FromArgb(0, 225, 255);
                    }
                    else
                    {
                        bg = _isPressed ? Color.FromArgb(32, 32, 36) : (_isHovered ? Color.FromArgb(26, 26, 30) : _idleBackColor);
                        border = _isHovered ? Color.FromArgb(80, 80, 92) : _idleBorderColor;
                        text = Color.FromArgb(150, 150, 160);
                    }
                }
                else
                {
                    // Standard action pill button
                    if (_isActive)
                    {
                        bg = _isPressed ? Color.FromArgb(0, 175, 200) : (_isHovered ? Color.FromArgb(30, 225, 255) : _accentColor);
                        border = bg;
                        text = Color.FromArgb(14, 14, 16);
                    }
                    else
                    {
                        bg = _isPressed ? Color.FromArgb(38, 38, 44) : (_isHovered ? Color.FromArgb(32, 32, 38) : _idleBackColor);
                        border = _isHovered ? Color.FromArgb(0, 216, 246) : _idleBorderColor;
                        text = _isHovered ? Color.White : _idleTextColor;
                    }
                }

                using (SolidBrush brush = new SolidBrush(bg))
                {
                    g.FillPath(brush, path);
                }

                using (Pen pen = new Pen(border, borderWidth))
                {
                    g.DrawPath(pen, path);
                }

                // High-precision subpixel text alignment
                using (StringFormat sf = new StringFormat())
                {
                    sf.Alignment = StringAlignment.Center;
                    sf.LineAlignment = StringAlignment.Center;
                    using (SolidBrush textBrush = new SolidBrush(text))
                    {
                        g.DrawString(Text, Font, textBrush, new RectangleF(0, 0, Width, Height), sf);
                    }
                }
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

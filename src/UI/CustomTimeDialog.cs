using System;
using System.Drawing;
using System.Windows.Forms;
using BatteryAlert.Native;

namespace BatteryAlert.UI
{
    public class CustomTimeDialog : Form
    {
        private readonly DateTimePicker _picker;
        private readonly PillButton _btnConfirm;
        private readonly PillButton _btnCancel;
        private readonly Label _lblPrompt;
        private readonly Label _lblError;

        public DateTime SelectedTime { get; private set; }

        public CustomTimeDialog()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(290, 180);
            BackColor = Color.FromArgb(20, 20, 24);
            ShowInTaskbar = false;
            TopMost = true;

            // Apply dark mode border
            int darkMode = 1;
            NativeMethods.DwmSetWindowAttribute(Handle, NativeMethods.DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));
            int roundCorner = NativeMethods.DWMWCP_ROUND;
            NativeMethods.DwmSetWindowAttribute(Handle, NativeMethods.DWMWA_WINDOW_CORNER_PREFERENCE, ref roundCorner, sizeof(int));

            _lblPrompt = new Label
            {
                Text = "PAUSE NOTIFICATIONS UNTIL",
                Font = new Font("Segoe UI Semibold", 8.5f),
                ForeColor = Color.FromArgb(0, 216, 246),
                Location = new Point(20, 18),
                AutoSize = true
            };

            _picker = new DateTimePicker
            {
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "ddd, MMM d  •  h:mm tt",
                Font = new Font("Segoe UI", 10.5f),
                Location = new Point(20, 52),
                Width = 250,
                Value = DateTime.Now.AddHours(3)
            };

            _lblError = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 8f),
                ForeColor = Color.FromArgb(255, 120, 120),
                Location = new Point(20, 90),
                AutoSize = true
            };

            _btnConfirm = new PillButton
            {
                Text = "Set Pause",
                IsActive = true,
                Location = new Point(160, 126),
                Size = new Size(110, 32)
            };
            _btnConfirm.Click += OnConfirmClicked;

            _btnCancel = new PillButton
            {
                Text = "Cancel",
                IsActive = false,
                Location = new Point(70, 126),
                Size = new Size(80, 32)
            };
            _btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            Controls.Add(_lblPrompt);
            Controls.Add(_picker);
            Controls.Add(_lblError);
            Controls.Add(_btnCancel);
            Controls.Add(_btnConfirm);
        }

        private void OnConfirmClicked(object sender, EventArgs e)
        {
            if (_picker.Value <= DateTime.Now.AddMinutes(1))
            {
                _lblError.Text = "Please select a time in the future.";
                return;
            }

            SelectedTime = _picker.Value;
            DialogResult = DialogResult.OK;
            Close();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            // 1px subtle border
            using (Pen pen = new Pen(Color.FromArgb(45, 45, 55), 1f))
            {
                e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
            }
        }
    }
}

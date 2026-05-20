using System;
using System.Collections.Generic;
using System.Text;

namespace WF_Client
{
    public class FocusModeService
    {
        private Form _focusOverlay;
        private bool _isActive = false;
        public event Action OnActivated;
        public event Action OnDeactivated;

        public bool IsActive => _isActive;

        public void Activate()
        {
            if (_isActive) return;
            _isActive = true;

            var form = Application.OpenForms[0];
            form.Invoke(() =>
            {
                _focusOverlay = new Form
                {
                    FormBorderStyle = FormBorderStyle.None,
                    WindowState = FormWindowState.Maximized,
                    TopMost = true,
                    BackColor = System.Drawing.Color.Black,
                    Opacity = 0.85,
                    Text = "집중 모드"
                };

                var label = new Label
                {
                    Text = "집중 모드 활성화\n선생님이 집중 모드를 설정했습니다.",
                    ForeColor = System.Drawing.Color.White,
                    Font = new System.Drawing.Font("Segoe UI", 20, System.Drawing.FontStyle.Bold),
                    AutoSize = true,
                    TextAlign = System.Drawing.ContentAlignment.MiddleCenter
                };

                _focusOverlay.Controls.Add(label);
                _focusOverlay.Load += (s, e) =>
                {
                    label.Location = new System.Drawing.Point(
                        (_focusOverlay.Width - label.Width) / 2,
                        (_focusOverlay.Height - label.Height) / 2
                    );
                };

                _focusOverlay.Show();
                OnActivated?.Invoke();

                // 3초 후 창만 닫기 (로직은 유지)
                var timer = new System.Windows.Forms.Timer { Interval = 3000 };
                timer.Tick += (s, e) =>
                {
                    timer.Stop();
                    _focusOverlay?.Close();
                    _focusOverlay = null;
                };
                timer.Start();
            });
        }

        public void Deactivate()
        {
            if (!_isActive) return;
            _isActive = false;
            _focusOverlay?.Close();
            _focusOverlay = null;

            OnDeactivated?.Invoke();
        }
    }
}

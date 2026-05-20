using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace WF_Server
{
    public partial class ClientControlForm : Form
    {
        private ClientSession _session;
        private ServerController _controller;
        private string _currentProcessName;
        private Action<string, string, string, string> _appendLog; // 로그 콜백

        public ClientControlForm(ClientSession session, ServerController controller, string szCurrentProcessName, Action<string, string, string, string> appendLog)
        {
            InitializeComponent();
            _session = session;
            _controller = controller;
            _currentProcessName = szCurrentProcessName;
            _appendLog = appendLog;

            InitUI();
        }

        private void InitUI()
        {
            this.Text = $"클라이언트 제어 - {_session.PcName}";
            this.Size = new Size(320, 460);  // ← 높이 늘림
            this.BackColor = Color.FromArgb(13, 13, 13);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;

            // 현재 프로세스 표시
            var lblCurrent = new Label
            {
                Text = "현재 프로세스 :",
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 9),
                AutoSize = true,
                Location = new Point(20, 20)
            };

            var lblProcessName = new Label
            {
                Name = "lblProcessName",
                Text = _session.CurrentProcess ?? "...",
                ForeColor = Color.FromArgb(184, 150, 12),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(130, 20)
            };

            // 허용 프로세스 입력
            var lblAllow = new Label
            {
                Text = "차단 프로세스 추가 :",
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 9),
                AutoSize = true,
                Location = new Point(20, 60)
            };

            var txtProcess = new TextBox
            {
                Location = new Point(20, 82),
                Size = new Size(180, 24),
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9),
                PlaceholderText = "예: notepad"
            };

            var btnAdd = new Button
            {
                Text = "추가",
                Location = new Point(210, 80),
                Size = new Size(70, 26),
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };

            // 리스트박스
            var lstBlocked = new ListBox
            {
                Location = new Point(20, 120),
                Size = new Size(260, 130),
                BackColor = Color.FromArgb(26, 26, 26),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9)
            };

            var btnRemove = new Button
            {
                Text = "선택 삭제",
                Location = new Point(20, 260),
                Size = new Size(80, 26),
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };

            // 이벤트
            btnAdd.Click += (s, e) =>
            {
                string process = txtProcess.Text.Trim();
                if (string.IsNullOrWhiteSpace(process)) return;
                if (!lstBlocked.Items.Contains(process))
                    lstBlocked.Items.Add(process);
                txtProcess.Clear();
            };

            btnRemove.Click += (s, e) =>
            {
                if (lstBlocked.SelectedItem != null)
                    lstBlocked.Items.Remove(lstBlocked.SelectedItem);
            };

            // 집중 모드 ON
            var btnFocusOn = new Button
            {
                Text = "집중 모드 ON",
                Location = new Point(20, 305),
                Size = new Size(120, 34),
                BackColor = Color.FromArgb(184, 150, 12),
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };

            btnFocusOn.Click += async (s, e) =>
            {
                var blockedList = lstBlocked.Items.Cast<string>().ToList();
                if (blockedList.Count == 0)
                {
                    MessageBox.Show("차단할 프로세스를 최소 1개 추가하세요.", "알림");
                    return;
                }
                await _controller.SendFocusModeOff(_session);
                await Task.Delay(200);
                await _controller.SendFocusMode(_session, blockedList);
                string blockedStr = string.Join(", ", blockedList);
                _appendLog?.Invoke(_session.PcName, "집중 모드 ON", "성공", $"'{_session.PcName}'에 집중 모드 적용 | 차단: {blockedStr}");
            };

            // 집중 모드 OFF
            var btnFocusOff = new Button
            {
                Text = "집중 모드 OFF",
                Location = new Point(160, 305),
                Size = new Size(120, 34),
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };

            btnFocusOff.Click += async (s, e) =>
            {
                await _controller.SendFocusModeOff(_session);
            };


            //메시지 보내는 구간
            var btnSendMsg = new Button
            {
                Text = "메시지 보내기",
                Location = new Point(20, 355),
                Size = new Size(260, 34),
                BackColor = Color.FromArgb(50, 80, 120),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };

            btnSendMsg.Click += async (s, e) =>
            {
                var inputForm = new Form
                {
                    Text = $"메시지 보내기 → {_session.PcName}",
                    Size = new Size(360, 160),
                    BackColor = Color.FromArgb(13, 13, 13),
                    FormBorderStyle = FormBorderStyle.FixedSingle,
                    MaximizeBox = false,
                    StartPosition = FormStartPosition.CenterParent
                };

                var txtMsg = new TextBox
                {
                    Location = new Point(20, 20),
                    Size = new Size(300, 24),
                    BackColor = Color.FromArgb(40, 40, 40),
                    ForeColor = Color.White,
                    BorderStyle = BorderStyle.FixedSingle,
                    Font = new Font("Segoe UI", 9),
                    PlaceholderText = "메시지를 입력하세요"
                };

                var btnSend = new Button
                {
                    Text = "전송",
                    Location = new Point(200, 60),
                    Size = new Size(120, 34),
                    BackColor = Color.FromArgb(184, 150, 12),
                    ForeColor = Color.Black,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };

                btnSend.Click += async (ss, ee) =>
                {
                    string msg = txtMsg.Text.Trim();
                    if (string.IsNullOrWhiteSpace(msg)) return;
                    await _controller.SendMessage(_session, msg);
                    _appendLog?.Invoke(_session.PcName, "메시지 전송", "성공", msg);
                    inputForm.Close();
                };

                inputForm.Controls.AddRange(new Control[] { txtMsg, btnSend });
                inputForm.ShowDialog();
            };

            // ← 모든 컨트롤 추가
            this.Controls.AddRange(new Control[]
            {
                lblCurrent, lblProcessName,
                lblAllow, txtProcess, btnAdd,
                lstBlocked, btnRemove,
                btnFocusOn, btnFocusOff,
                btnSendMsg
            });

            // 현재 프로세스 실시간 갱신
            var timer = new System.Windows.Forms.Timer { Interval = 500 };
            timer.Tick += (s, e) => lblProcessName.Text = _session.CurrentProcess ?? "...";
            timer.Start();
            this.FormClosed += (s, e) => timer.Stop();
        }
    }
}

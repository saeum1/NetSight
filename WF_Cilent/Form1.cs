using WF_Client;

namespace WF_Cilent
{
    public partial class Form1 : Form
    {
        private TcpClientService _tcpService;
        private FocusModeService _focusModeService;
        private ClientCommandHandler _commandHandler;

        // UI 컨트롤
        private TextBox txtPcName;
        private Label lblConnectStatus;
        private Label lblMonitorStatus;
        private Label lblFocusStatus;
        private Button btnConnect;
        private Button btnDisconnect;

        public Form1()
        {
            InitializeComponent();
            InitUI();
            InitServices();
        }

        private void InitUI()
        {
            this.Text = "LocalDesk Client";
            this.Size = new Size(320, 320);
            this.BackColor = Color.FromArgb(13, 13, 13);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            int labelX = 20, valueX = 130, rowH = 36, startY = 20;

            // 학생 PC 이름
            var lblPcName = MakeLabel("학생 PC 이름", new Point(labelX, startY));
            txtPcName = new TextBox
            {
                Text = "1번 PC",
                Location = new Point(valueX, startY - 2),
                Size = new Size(140, 22),
                BackColor = Color.FromArgb(26, 26, 26),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9)
            };

            // 연결 상태
            var lblConnLabel = MakeLabel("연결 상태", new Point(labelX, startY + rowH));
            lblConnectStatus = MakeValueLabel("연결 없음", Color.Gray, new Point(valueX, startY + rowH));

            // 모니터링 상태
            var lblMonLabel = MakeLabel("모니터링 상태", new Point(labelX, startY + rowH * 2));
            lblMonitorStatus = MakeValueLabel("중지됨", Color.Gray, new Point(valueX, startY + rowH * 2));

            // 집중 모드
            var lblFocusLabel = MakeLabel("집중 모드", new Point(labelX, startY + rowH * 3));
            lblFocusStatus = MakeValueLabel("해제됨", Color.Gray, new Point(valueX, startY + rowH * 3));

            // 버튼
            btnConnect = new Button
            {
                Text = "서버 연결",
                Location = new Point(20, startY + rowH * 4 + 10),
                Size = new Size(110, 34),
                BackColor = Color.FromArgb(184, 150, 12),
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnConnect.Click += BtnConnect_Click;

            btnDisconnect = new Button
            {
                Text = "연결 종료",
                Location = new Point(150, startY + rowH * 4 + 10),
                Size = new Size(110, 34),
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            btnDisconnect.Click += BtnDisconnect_Click;


            //메시지 구간
            var btnSendMsg = new Button
            {
                Text = "교사에게 메시지",
                Location = new Point(20, startY + rowH * 5 + 10),
                Size = new Size(240, 34),
                BackColor = Color.FromArgb(50, 80, 120),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = false,
                Name = "btnSendMsg"
            };

            btnSendMsg.Click += async (s, e) =>
            {
                var inputForm = new Form
                {
                    Text = "교사에게 메시지 보내기",
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
                    Location = new Point(200, 60),
                    Text = "전송",
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
                    await _tcpService.SendMessageToServer(msg);
                    inputForm.Close();
                };

                inputForm.Controls.AddRange(new Control[] { txtMsg, btnSend });
                inputForm.ShowDialog();
            };

            this.Controls.AddRange(new Control[]
            {
            lblPcName, txtPcName,
            lblConnLabel, lblConnectStatus,
            lblMonLabel, lblMonitorStatus,
            lblFocusLabel, lblFocusStatus,
            btnConnect, btnDisconnect,
            btnSendMsg
            });
        }

        private void InitServices()
        {
            _focusModeService = new FocusModeService();
            _tcpService = new TcpClientService();
            _commandHandler = new ClientCommandHandler(_tcpService, _focusModeService);
            _tcpService.SetCommandHandler(_commandHandler);

            _tcpService.OnConnected += () => this.Invoke(() =>
            {
                lblConnectStatus.Text = "연결 중";
                lblConnectStatus.ForeColor = Color.FromArgb(184, 150, 12);
                lblMonitorStatus.Text = "모니터링 중";
                lblMonitorStatus.ForeColor = Color.FromArgb(184, 150, 12);
                btnConnect.Enabled = false;
                btnDisconnect.Enabled = true;
                this.Controls.Find("btnSendMsg", true).FirstOrDefault().Enabled = true;
                _tcpService.StartMonitoring();
            });

            _tcpService.OnDisconnected += () => this.Invoke(() =>
            {
                lblConnectStatus.Text = "연결 없음";
                lblConnectStatus.ForeColor = Color.Gray;
                lblMonitorStatus.Text = "중지됨";
                lblMonitorStatus.ForeColor = Color.Gray;
                lblFocusStatus.Text = "해제됨";
                lblFocusStatus.ForeColor = Color.Gray;
                btnConnect.Enabled = true;
                btnDisconnect.Enabled = false;
                this.Controls.Find("btnSendMsg", true).FirstOrDefault().Enabled = false;
            });

            _focusModeService.OnActivated += () => this.Invoke(() =>
            {
                lblFocusStatus.Text = "실행 중";
                lblFocusStatus.ForeColor = Color.OrangeRed;
            });

            _focusModeService.OnDeactivated += () => this.Invoke(() =>
            {
                lblFocusStatus.Text = "해제됨";
                lblFocusStatus.ForeColor = Color.Gray;
            });
        }

        private async void BtnConnect_Click(object sender, EventArgs e)
        {
            await _tcpService.ConnectAsync(txtPcName.Text);
        }

        private void BtnDisconnect_Click(object sender, EventArgs e)
        {
            _tcpService.Disconnect();
        }

        // 헬퍼
        private Label MakeLabel(string text, Point loc) => new Label
        {
            Text = text,
            ForeColor = Color.Gray,
            Font = new Font("Segoe UI", 9),
            AutoSize = true,
            Location = loc
        };

        private Label MakeValueLabel(string text, Color color, Point loc) => new Label
        {
            Text = text,
            ForeColor = color,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            AutoSize = true,
            Location = loc
        };
    }
}

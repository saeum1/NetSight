using WF_Server.DBManager;

namespace WF_Server
{
    public partial class Form1 : Form
    {
        private TcpServer _tcpServer;
        private ClientSessionManager _sessionManager;
        private ServerPacketRouter _router;
        private ServerController _controller;
        private FlowLayoutPanel _gridPanel;
        private ListView _logListView;          // 추가
        private string _processName;

        //메시지함
        private Label _lblMessageCount;
        private List<(string PcName, string Message, DateTime Time)> _receivedMessages = new();
        private int _unreadCount = 0;

        private List<string> _globalBlockedList = new List<string>();
        private readonly object _imageLock = new object();

        public Form1()
        {
            InitializeComponent();
            InitUI();
            InitServer();
            DB.Initialize();
        }


        //UI부분
        private void InitUI()
        {
            this.Text = "LocalDesk Server";
            this.Size = new Size(1000, 900);
            this.BackColor = Color.FromArgb(13, 13, 13);

            // 툴바
            var toolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Color.FromArgb(26, 26, 26),
                Padding = new Padding(8, 6, 8, 6)
            };

            var lblToolbar = new Label
            {
                Text = "전체 제어",
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 9),
                AutoSize = true,
                Location = new Point(12, 13)
            };

            var btnGlobalFocusSetting = new Button
            {
                Text = "집중 모드 설정",
                Location = new Point(80, 7),
                Size = new Size(110, 30),
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };

            var btnGlobalFocusOn = new Button
            {
                Text = "전체 집중 ON",
                Location = new Point(202, 7),
                Size = new Size(110, 30),
                BackColor = Color.FromArgb(184, 150, 12),
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };

            var btnGlobalFocusOff = new Button
            {
                Text = "전체 집중 OFF",
                Location = new Point(322, 7),
                Size = new Size(110, 30),
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };

            var btnMessageBox = new Button
            {
                Text = "메시지 : 0개",
                Location = new Point(442, 7),
                Size = new Size(120, 30),
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Name = "btnMessageBox"
            };

            btnMessageBox.Click += (s, e) => OpenMessageBoxForm();

            btnGlobalFocusSetting.Click += (s, e) =>
            {
                var settingForm = new Form
                {
                    Text = "전체 집중 모드 설정",
                    Size = new Size(320, 380),
                    BackColor = Color.FromArgb(13, 13, 13),
                    FormBorderStyle = FormBorderStyle.FixedSingle,
                    MaximizeBox = false,
                    StartPosition = FormStartPosition.CenterParent
                };

                var lblAllow = new Label
                {
                    Text = "차단 프로세스 목록",
                    ForeColor = Color.Gray,
                    Font = new Font("Segoe UI", 9),
                    AutoSize = true,
                    Location = new Point(20, 20)
                };

                var txtProcess = new TextBox
                {
                    Location = new Point(20, 45),
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
                    Location = new Point(210, 43),
                    Size = new Size(70, 26),
                    BackColor = Color.FromArgb(50, 50, 50),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand
                };

                var lstBlocked = new ListBox
                {
                    Location = new Point(20, 82),
                    Size = new Size(260, 180),
                    BackColor = Color.FromArgb(26, 26, 26),
                    ForeColor = Color.White,
                    BorderStyle = BorderStyle.FixedSingle,
                    Font = new Font("Segoe UI", 9)
                };

                var btnRemove = new Button
                {
                    Text = "선택 삭제",
                    Location = new Point(20, 272),
                    Size = new Size(80, 26),
                    BackColor = Color.FromArgb(50, 50, 50),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand
                };

                var btnConfirm = new Button
                {
                    Text = "확인",
                    Location = new Point(200, 272),
                    Size = new Size(80, 26),
                    BackColor = Color.FromArgb(184, 150, 12),
                    ForeColor = Color.Black,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };

                //집중모드에 추가 할 프로세스 추가 버튼 이벤트
                btnAdd.Click += (s, e) =>
                {
                    string process = txtProcess.Text.Trim();
                    if (string.IsNullOrWhiteSpace(process)) return;
                    if (!lstBlocked.Items.Contains(process))
                        lstBlocked.Items.Add(process);
                    txtProcess.Clear();
                };

                //집중모드에서 제거 할 프로세스 버튼 이벤트 (리스트 형식이라 누르면 선택됨)
                btnRemove.Click += (s, e) =>
                {
                    if (lstBlocked.SelectedItem != null)
                        lstBlocked.Items.Remove(lstBlocked.SelectedItem);
                };

                //집중 모드 설정 완료 버튼 클릭 이벤트
                btnConfirm.Click += (s, e) =>
                {
                    _globalBlockedList = lstBlocked.Items.Cast<string>().ToList();
                    settingForm.Close();
                };

                settingForm.Controls.AddRange(new Control[]
                {
                    lblAllow, txtProcess, btnAdd,
                    lstBlocked, btnRemove, btnConfirm
                });

                settingForm.ShowDialog();
            };

            //전체 집중ON 버튼 클릭 이벤트
            btnGlobalFocusOn.Click += async (s, e) =>
            {
                var allUserBlockProcessList = _globalBlockedList;
                if (allUserBlockProcessList.Count == 0)
                {
                    MessageBox.Show("차단할 프로세스를 최소 1개 추가하세요.", "알림");
                    return;
                }
                var sessions = _sessionManager.GetAll().ToList(); // ← 이거 추가
                foreach (var session in sessions)
                {
                    await _controller.SendFocusMode(session, allUserBlockProcessList);
                }
                string blockedStr = string.Join(", ", allUserBlockProcessList);
                AppendLog("전체", "집중 모드 ON", "성공", $"전체 {sessions.Count}대에 집중 모드 적용 | 차단: {blockedStr}");
            };

            //전체 집중OFF 버튼 클릭 이벤트
            btnGlobalFocusOff.Click += async (s, e) =>
            {
                foreach (var session in _sessionManager.GetAll())
                {
                    await _controller.SendFocusModeOff(session); //프로세스 포커스 취소
                }
            };

            toolbar.Controls.AddRange(new Control[]
            {
                lblToolbar, btnGlobalFocusSetting,
                btnGlobalFocusOn, btnGlobalFocusOff, btnMessageBox
            });

            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                BackColor = Color.FromArgb(13, 13, 13),
                SplitterWidth = 4,
                Panel1MinSize = 200,
                Panel2MinSize = 120
            };

            this.Load += (s, e) =>
            {
                split.SplitterDistance = (int)(split.Height * 0.65);
                LoadLogsFromDB();
            };

            _gridPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(13, 13, 13),
                AutoScroll = true,
                Padding = new Padding(6)
            };
            split.Panel1.Controls.Add(_gridPanel);

            var logContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(17, 17, 17)
            };

            var logHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 28,
                BackColor = Color.FromArgb(26, 26, 26)
            };

            var lblLog = new Label
            {
                Text = "DB LOG",
                ForeColor = Color.FromArgb(184, 150, 12),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(10, 6)
            };

            var btnClear = new Button
            {
                Text = "Clear",
                ForeColor = Color.Gray,
                BackColor = Color.FromArgb(40, 40, 40),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8),
                Size = new Size(52, 20),
                Location = new Point(920, 4)
            };

            btnClear.FlatAppearance.BorderColor = Color.FromArgb(60, 60, 60);
            btnClear.Click += (s, e) => _logListView.Items.Clear();
            logHeader.Controls.AddRange(new Control[] { lblLog, btnClear });

            _logListView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = false,
                BackColor = Color.FromArgb(17, 17, 17),
                ForeColor = Color.FromArgb(210, 210, 210),
                BorderStyle = BorderStyle.None,
                Font = new Font("Consolas", 9),
                HeaderStyle = ColumnHeaderStyle.Nonclickable
            };

            _logListView.Columns.Add("시간", 150);
            _logListView.Columns.Add("학생 PC", 130);
            _logListView.Columns.Add("이벤트 종류", 180);
            _logListView.Columns.Add("결과", 100);
            _logListView.Columns.Add("상세 로그 메시지", 340);

            logContainer.Controls.Add(_logListView);
            logContainer.Controls.Add(logHeader);
            split.Panel2.Controls.Add(logContainer);

            this.Controls.Add(toolbar);
            this.Controls.Add(split);
        }


        // ── 로그 추가 헬퍼 ───────────────────────────────────────────────────
        public void AppendLog(string pcName, string eventType, string result, string detail)
        {
            var safePcName = pcName ?? "Unknown";  // null 방어

            var item = new ListViewItem(DateTime.Now.ToString("HH:mm:ss"));
            item.SubItems.Add(pcName);
            item.SubItems.Add(eventType);
            item.SubItems.Add(result);
            item.SubItems.Add(detail);
            _logListView.Items.Insert(0, item);   // 최신 로그가 맨 위

            if (_logListView.Items.Count > 500)   // 500줄 초과 시 오래된 것 제거
                _logListView.Items.RemoveAt(_logListView.Items.Count - 1);

            DB.SaveLog(pcName, pcName, eventType, result, detail); //여기서 DB에 저장하는 함수 호출
        }

        public void InitServer()
        {
            _sessionManager = new ClientSessionManager(); //연결된 학생들 목록 관리 객체
            _router = new ServerPacketRouter(); //클라이언트가 보냇 패킷 처리 객체
            _controller = new ServerController(); //서버->클라 명령 처리 객체
            _tcpServer = new TcpServer(); //TCP 서버 객체

            _tcpServer.OnClientAccepted += (session) => //이벤트 연결1
            {
                session.OnPacketReceived += (s, data) => _router.Route(s, data); //클라에서 패킷 보내면 router에게 전송

                session.OnHandshakeCompleted += (s) =>
                {
                    if (_sessionManager.IsPcNameDuplicate(s.PcName))
                    {
                        // 중복 이름 → 거부 패킷 전송 후 연결 해제
                        s.SendAsync(new byte[] { PacketType.DuplicateName }).Wait();
                        this.Invoke(() =>
                        {
                            AppendLog(s.PcName, "접속 거부", "실패", $"중복된 PC 이름: '{s.PcName}'");
                        });
                        s.Disconnect();
                        return;
                    }
                    _sessionManager.Add(s); //접속 학생 목록 추가
                };
            };

            _sessionManager.OnSessionAdded += (session) => //이벤트 연결2
            {
                this.Invoke(() => //학생 목록에 새 학생이 추가되면 실행
                {
                    AddClientCard(session); //학생 카드 UI 생성
                    AppendLog(session.PcName, "접속", "성공", $"세션 ID: {session.SessionId}"); //DB 로그 추가
                });
            };

            _sessionManager.OnSessionRemoved += (session) => //이벤트 연결3
            {
                this.Invoke(() => //학생 연결 끊기면 실행
                {
                    RemoveClientCard(session); //학생 카드 UI 제거
                    AppendLog(session.PcName, "연결 해제", "-", $"세션 ID: {session.SessionId}"); //DB 로그 추가
                });
            };

            _router.OnMonitorFrame += (session, data) => //이벤트 연결4
            {
                try  //학생 화면 이미 도착 시 실행
                {
                    byte[] imageData = new byte[data.Length - 8]; //이미지 데아터 크기 만큼 배열 할당
                    Buffer.BlockCopy(data, 8, imageData, 0, imageData.Length); //패킷 헤더 제외해서 이미지만 추출
                     
                    using var ms = new MemoryStream(imageData); // imageData 바이트 배열을 메모리 스트림으로 변환
                    var bmp = new Bitmap(ms); //비트맵 생성
                    lock (_imageLock) // 다른 스레드가 CurrentImage 접근 못하게 잠금
                    {
                        session.CurrentImage = bmp; // 세션에 최신 이미지 저장
                    }

                    this.Invoke(() =>
                    {
                        var card = _gridPanel.Controls[session.SessionId] as Panel; // 그리드에서 해당 학생의 카드 Panel 찾기
                        if (card == null) return;

                        var pb = card.Controls[session.SessionId] as PictureBox; // 카드 안에서 PictureBox 찾기
                        if (pb == null) return;

                        pb.Image?.Dispose(); // 기존 이미지 메모리 해제(안하면 메모리 누수)
                        pb.Image = bmp; // 새 이미지로 교체해서 화면에 표시 (안하면 멈추더라)
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[이미지 에러] {ex.Message}");
                }
            };

            _router.OnFocusProcessReport += (session, processName) =>
            {// 클라가 현재 포커스된 프로세스 이름을 주기적으로 서버에 보고할 때 발생하는 이벤트
                this.Invoke(() =>
                {
                    _processName = processName;
                    //DB.SaveLog(session.SessionId, session.PcName, "포커스 변경", "감지", $"프로세스: {processName}");
                    //AppendLog(session.PcName, "포커스 변경", "감지", $"프로세스: {processName}");
                });
            };

            _router.OnProcessKillAlert += (session, processName) =>
            {// 집중 모드 중 차단 프로세스가 실행됐다가 Kill된 것을 클라가 서버에 알릴 때 발생
                this.Invoke(() =>
                {
                    AppendLog(session.PcName, "집중 모드", "경고", $"'{processName}' 종료 감지");
                });
            };

            _router.OnIsNotExistProcess += (session, processName) =>
            {// 집중 모드 ON 시 서버가 지정한 프로세스가 클라에 아예 없을 때 발생
                this.Invoke(() =>
                {
                    AppendLog(session.PcName, "집중 모드", "경고", $"'{processName}' 프로세스가 존재하지 않음");
                });
            };

            _router.OnFocusGuardResult += (session, processName, isAllowed) =>
            {// FocusGuardLoop가 주기적으로 포커스 상태를 감시하고 결과를 서버에 보고할 때 발생
                this.Invoke(() =>
                {
                    AppendLog(session.PcName, "포커스 감시", isAllowed ? "정상" : "이탈", $"프로세스: {processName}");
                });
            };

            _router.OnMessageReceived += (session, message) =>
            {
                this.Invoke(() =>
                {
                    _receivedMessages.Add((session.PcName, message, DateTime.Now));
                    _unreadCount++;

                    // 툴바의 메시지 버튼 텍스트 갱신
                    var btn = this.Controls.Find("btnMessageBox", true).FirstOrDefault() as Button;
                    if (btn != null) btn.Text = $"메시지 : {_unreadCount}개";

                    AppendLog(session.PcName, "메시지 수신", "확인", message);
                });
            };

            _tcpServer.Start(9221);
        }

        private void AddClientCard(ClientSession session)
        {
            var card = new Panel
            {
                Name = session.SessionId,
                Size = new Size(460, 300),
                BackColor = Color.FromArgb(17, 17, 17),
                Margin = new Padding(6)
            };
            card.DoubleClick += (s, e) => OpenClientControlForm(session); // 카드 패널 더블클릭 시 해당 학생 제어창 열기

            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 30,
                BackColor = Color.FromArgb(26, 26, 26)
            };
            header.DoubleClick += (s, e) => OpenClientControlForm(session); // 헤더 부분 더블클릭 시도 동일하게 제어창 열기

            var lblId = new Label
            {
                Text = session.SessionId,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(10, 7)
            };
            var lblLive = new Label
            {
                Text = "LIVE",
                ForeColor = Color.Black,
                BackColor = Color.FromArgb(184, 150, 12),
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(380, 7),
                Padding = new Padding(4, 2, 4, 2)
            };
            header.Controls.AddRange(new Control[] { lblId, lblLive });

            var pb = new PictureBox
            {
                Name = session.SessionId,
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(10, 10, 10),
                SizeMode = PictureBoxSizeMode.Zoom
            };
            pb.DoubleClick += (s, e) => OpenClientControlForm(session); // PictureBox 더블클릭도 동일

            // 우클릭 컨텍스트 메뉴
            var contextMenu = new ContextMenuStrip();
            var menuControl = new ToolStripMenuItem("제어 창 열기");
            var menuFullScreen = new ToolStripMenuItem("전체화면으로 보기");

            menuControl.Click += (s, e) => OpenClientControlForm(session);
            menuFullScreen.Click += (s, e) => OpenFullScreenViewer(session);
            //각 메뉴 클릭 이벤트 등록

            contextMenu.Items.AddRange(new ToolStripItem[] { menuControl, menuFullScreen });

            card.ContextMenuStrip = contextMenu;
            header.ContextMenuStrip = contextMenu;
            pb.ContextMenuStrip = contextMenu;

            card.Controls.Add(pb);
            card.Controls.Add(header);
            _gridPanel.Controls.Add(card);
        }

        private void OpenClientControlForm(ClientSession session)
        {
            var form = new ClientControlForm(session, _controller, _processName, AppendLog);
            form.Show();
        }

        private void OpenMessageBoxForm()
        {
            var msgForm = new Form
            {
                Text = "받은 메시지함",
                Size = new Size(520, 420),
                BackColor = Color.FromArgb(13, 13, 13),
                FormBorderStyle = FormBorderStyle.FixedSingle,
                MaximizeBox = false,
                StartPosition = FormStartPosition.CenterParent
            };

            var listView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = false,
                BackColor = Color.FromArgb(26, 26, 26),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None,
                Font = new Font("Consolas", 9),
                HeaderStyle = ColumnHeaderStyle.Nonclickable
            };

            listView.Columns.Add("시간", 90);
            listView.Columns.Add("보낸이 (PC)", 120);
            listView.Columns.Add("메시지 내용", 270);

            foreach (var m in _receivedMessages.AsEnumerable().Reverse())
            {
                var item = new ListViewItem(m.Time.ToString("HH:mm:ss"));
                item.SubItems.Add(m.PcName);
                item.SubItems.Add(m.Message);
                listView.Items.Add(item);
            }

            // 메시지 클릭 시 상세 팝업
            listView.DoubleClick += (s, e) =>
            {
                if (listView.SelectedItems.Count == 0) return;
                var selected = listView.SelectedItems[0];
                MessageBox.Show(
                    $"보낸이 : {selected.SubItems[1].Text}\n\n{selected.SubItems[2].Text}",
                    $"메시지 - {selected.SubItems[0].Text}",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            };

            // 메시지함 열면 읽음 처리
            _unreadCount = 0;
            var btn = this.Controls.Find("btnMessageBox", true).FirstOrDefault() as Button;
            if (btn != null) btn.Text = "메시지 : 0개";

            msgForm.Controls.Add(listView);
            msgForm.ShowDialog();
        }


        private DateTime _lastMouseSend = DateTime.MinValue;
        private void OpenFullScreenViewer(ClientSession session)
        {
            var viewer = new Form
            {
                Text = $"전체화면 - {session.PcName}",
                WindowState = FormWindowState.Maximized,
                BackColor = Color.Black,
                FormBorderStyle = FormBorderStyle.Sizable
            };

            var pb = new PictureBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black,
                SizeMode = PictureBoxSizeMode.Zoom,
                Image = session.CurrentImage  // 현재 이미지로 초기화
            };

            // 타이머를 왜 쓰냐 -> OnMonitorFrame 이벤트는 네트워크 수신 스레드에서 발생하는데, UI 갱신은 메인스레드라 이새끼가 처리해줌
            var timer = new System.Windows.Forms.Timer { Interval = 50 }; // 50ms(초당 20프레임)마다 실행되는 타이머 생성
            timer.Tick += (s, e) =>
            {
                Bitmap img;
                lock (_imageLock) // 다른 스레드가 이미지 교체 중일 수 있으니 lock 걸고
                {
                    img = session.CurrentImage;
                }
                if (img == null) return;
                try { pb.Image = img; } catch { }
            };
            timer.Start();

            // 마우스 이벤트
            pb.MouseMove += async (s, e) =>
            {
                if ((DateTime.Now - _lastMouseSend).TotalMilliseconds < 30) return; // 마지막 전송으로부터 30ms 안 지났으면 스킵 (쓰로틀링 - 패킷 폭주 방지)
                _lastMouseSend = DateTime.Now;

                int x, y;
                lock (_imageLock)
                {
                    var img = session.CurrentImage;
                    if (img == null) return;
                    x = (int)(e.X * (img.Width / (double)pb.Width));
                    y = (int)(e.Y * (img.Height / (double)pb.Height));
                    // PictureBox 위의 마우스 좌표를 실제 클라 화면 해상도 좌표로 변환
                    // 예: PictureBox가 800px인데 실제 화면이 1920px면 좌표를 2.4배 키움
                }
                await _controller.SendMouseEvent(session, x, y, (byte)PacketType.MouseAction.Move); // 변환된 좌표로 마우스 이동 패킷 전송
            };

            pb.MouseDown += async (s, e) =>
            {
                int x, y;
                lock (_imageLock)
                {
                    var img = session.CurrentImage;
                    if (img == null) return;
                    x = (int)(e.X * (img.Width / (double)pb.Width));
                    y = (int)(e.Y * (img.Height / (double)pb.Height));
                }

                byte action = e.Button switch
                {
                    MouseButtons.Left => (byte)PacketType.MouseAction.LeftDown,
                    MouseButtons.Right => (byte)PacketType.MouseAction.RightDown,
                    _ => (byte)PacketType.MouseAction.Move
                };
                await _controller.SendMouseEvent(session, x, y, action);
            };

            pb.MouseUp += async (s, e) =>
            {
                int x, y;
                lock (_imageLock)
                {
                    var img = session.CurrentImage;
                    if (img == null) return;
                    x = (int)(e.X * (img.Width / (double)pb.Width));
                    y = (int)(e.Y * (img.Height / (double)pb.Height));
                }

                byte action = e.Button switch
                {
                    MouseButtons.Left => (byte)PacketType.MouseAction.LeftUp,
                    MouseButtons.Right => (byte)PacketType.MouseAction.RightUp,
                    _ => (byte)PacketType.MouseAction.Move
                };
                await _controller.SendMouseEvent(session, x, y, action);
            };

            pb.MouseWheel += async (s, e) =>
            {
                int x, y;
                lock (_imageLock)
                {
                    var img = session.CurrentImage;
                    if (img == null) return;
                    x = (int)(e.X * (img.Width / (double)pb.Width));
                    y = (int)(e.Y * (img.Height / (double)pb.Height));
                }
                await _controller.SendMouseEvent(session, x, y, (byte)PacketType.MouseAction.Wheel, e.Delta);
            };

            // 키보드 이벤트
            viewer.KeyPreview = true;
            // Form이 키 이벤트를 컨트롤보다 먼저 받게 설정
            // 이게 없으면 PictureBox가 포커스 갖고 있을 때 KeyDown 안 잡힘

            viewer.KeyDown += async (s, e) =>
            {
                if (e.KeyCode == Keys.Escape) { viewer.Close(); return; }
                await _controller.SendKeyEvent(session, (int)e.KeyCode, 1);
            };
            viewer.KeyUp += async (s, e) =>
            {
                await _controller.SendKeyEvent(session, (int)e.KeyCode, 0);
            };
            viewer.FormClosed += (s, e) => timer.Stop(); // 뷰어 닫힐 때 타이머 정지 (안하면 닫힌 후에도 타이머가 계속 돌면서 메모리 낭비)
            viewer.KeyPreview = true;
            viewer.Controls.Add(pb);
            viewer.Show();
        }

        private void RemoveClientCard(ClientSession session)
        {
            var card = _gridPanel.Controls[session.SessionId];
            if (card != null)
            {
                _gridPanel.Controls.Remove(card);
                card.Dispose();
            }
        }

        private void LoadLogsFromDB() //기존 로그를 DB에서 가져옴
        {
            var logs = DB.GetRecentLogs(200);   // 최근 200개
            foreach (var log in logs)           // 이미 DESC 정렬이라 그대로 Insert
            {
                var item = new ListViewItem(log.CreatedAt);
                item.SubItems.Add(log.StudentName);
                item.SubItems.Add(log.EventType);
                item.SubItems.Add(log.Result);
                item.SubItems.Add(log.Message);
                item.ForeColor = Color.FromArgb(130, 130, 130); // 기존 로그는 회색으로 구분
                _logListView.Items.Add(item);
            }
        }
    }
}
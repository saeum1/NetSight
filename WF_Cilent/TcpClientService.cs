using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace WF_Client
{
    public class TcpClientService
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private MonitorFrameService _frameService;
        private ClientCommandHandler _commandHandler;
        private bool _isMonitoring = false;

        public event Action OnConnected;
        public event Action OnDisconnected;

        public TcpClientService()
        {
            _frameService = new MonitorFrameService();
        }

        public void SetCommandHandler(ClientCommandHandler handler)
        {
            _commandHandler = handler;
        }

        public async Task ConnectAsync(string pcName)
        {
            try
            {
                // 1. 연결 먼저
                _client = new TcpClient();
                await _client.ConnectAsync(ClientConfig.ServerIp, ClientConfig.ServerPort);
                _stream = _client.GetStream();

                // 2. 연결 후 PC 이름 전송
                byte[] nameBytes = Encoding.UTF8.GetBytes(pcName);
                byte[] nameLen = BitConverter.GetBytes(nameBytes.Length);
                await _stream.WriteAsync(nameLen, 0, 4);
                await _stream.WriteAsync(nameBytes, 0, nameBytes.Length);

                Console.WriteLine("[연결] 서버 접속 완료");

                // 연결 성공 후 상태 초기화 추가
                _isMonitoring = false;
                _blockedProcesses.Clear();
                OnConnected?.Invoke();

                Task.Run(() => ReceiveLoop());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[연결 실패] {ex.Message}");
            }
        }

        // 집중 모드 관련 필드
        private List<string> _blockedProcesses = new();
        private bool _lastFocusWasAllowed = false;
        private bool _processKillAlertSent = false;

        public void StartMonitoring()
        {
            if (_isMonitoring) return;
            _isMonitoring = true;
            _blockedProcesses.Clear();
            Task.Run(() => MonitorLoop());
        }

        public void StartMonitoring(List<string> blockedProcesses)
        {
            if (_isMonitoring) return;

            _isMonitoring = true;

            _blockedProcesses = blockedProcesses;

            _processKillAlertSent = false;

            Task.Run(() => MonitorLoop());
        }

        public void StopMonitoring()
        {
            _isMonitoring = false;

            _blockedProcesses.Clear();
        }

        public void Disconnect()
        {
            _isMonitoring = false;
            _stream?.Close();
            _client?.Close();
            OnDisconnected?.Invoke();
        }

        //private async Task MonitorLoop()
        //{
        //    while (_isMonitoring)
        //    {
        //        try
        //        {
        //            byte[] packet = _frameService.CaptureFrame();
        //            byte[] header = BitConverter.GetBytes(packet.Length);

        //            await _stream.WriteAsync(header, 0, 4);
        //            await _stream.WriteAsync(packet, 0, packet.Length);

        //            await Task.Delay(ClientConfig.FrameIntervalMs);
        //        }
        //        catch
        //        {
        //            Console.WriteLine("[모니터링] 전송 실패");
        //            break;
        //        }
        //    }
        //}

        // 모니터링 루프에 포커스 프로세스 전송 추가
        private async Task MonitorLoop()
        {
            while (_isMonitoring)
            {
                try
                {
                    // 화면 캡처 전송
                    byte[] packet = _frameService.CaptureFrame();
                    byte[] header = BitConverter.GetBytes(packet.Length);
                    await _stream.WriteAsync(header, 0, 4);
                    await _stream.WriteAsync(packet, 0, packet.Length);

                    // 현재 포커스 프로세스 이름 전송
                    string processName = GetFocusedProcessName();

                    Console.WriteLine("현재 프로세스 : " + processName);

                    byte[] nameBytes = Encoding.UTF8.GetBytes(processName);
                    byte[] processPacket = new byte[1 + nameBytes.Length];
                    processPacket[0] = 0x20;
                    Buffer.BlockCopy(nameBytes, 0, processPacket, 1, nameBytes.Length);

                    byte[] processHeader = BitConverter.GetBytes(processPacket.Length);
                    await _stream.WriteAsync(processHeader, 0, 4);
                    await _stream.WriteAsync(processPacket, 0, processPacket.Length);

                    await Task.Delay(ClientConfig.FrameIntervalMs);
                }
                catch
                {
                    Console.WriteLine("송신 실패");
                    break;
                }
            }
        }

        // 현재 포커스된 프로세스 이름 가져오기
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        public static string GetFocusedProcessName()
        {
            try
            {
                IntPtr hwnd = GetForegroundWindow();
                GetWindowThreadProcessId(hwnd, out uint pid);

                if (pid == 0) return "idle";

                var process = Process.GetProcessById((int)pid);
                return process.ProcessName;
            }
            catch
            {
                return "unknown";
            }
        }

        // 집중 모드 시작
        public void StartFocusMode(List<string> blockedProcesses)
        {
            _blockedProcesses = blockedProcesses;
            Task.Run(() => FocusGuardLoop());
        }

        // 집중 모드 해제
        public void StopFocusMode()
        {
            _blockedProcesses.Clear();
        }

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private async Task FocusGuardLoop()
        {
            while (_blockedProcesses.Count > 0)
            {
                try
                {
                    foreach (string blocked in _blockedProcesses)
                    {
                        var targets = Process.GetProcesses()
                            .Where(p => p.ProcessName.Equals(blocked, StringComparison.OrdinalIgnoreCase))
                            .ToArray();

                        foreach (var proc in targets)
                        {
                            try
                            {
                                proc.Kill();

                                await SendFocusGuardResult(proc.ProcessName, false);
                            }
                            catch { }
                        }
                    }
                }
                catch { }

                await Task.Delay(500);
            }
        }

        private async Task SendFocusGuardResult(string processName, bool isAllowed)
        {
            try
            {
                string msg = $"{processName}|{(isAllowed ? "정상" : "이탈")}";
                byte[] msgBytes = Encoding.UTF8.GetBytes(msg);
                byte[] packet = new byte[1 + msgBytes.Length];
                packet[0] = 0x25;
                Buffer.BlockCopy(msgBytes, 0, packet, 1, msgBytes.Length);

                byte[] header = BitConverter.GetBytes(packet.Length);
                await _stream.WriteAsync(header, 0, 4);
                await _stream.WriteAsync(packet, 0, packet.Length);
            }
            catch { }
        }

        public async Task SendMessageToServer(string message)
        {
            try
            {
                byte[] msgBytes = Encoding.UTF8.GetBytes(message);
                byte[] packet = new byte[1 + msgBytes.Length];
                packet[0] = PacketType.CTPK_SEND_MESSAGE;
                Buffer.BlockCopy(msgBytes, 0, packet, 1, msgBytes.Length); 

                byte[] header = BitConverter.GetBytes(packet.Length);
                await _stream.WriteAsync(header, 0, 4);
                await _stream.WriteAsync(packet, 0, packet.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[메시지 전송 실패] {ex.Message}");
            }
        }

        private async Task ReceiveLoop()
        {
            byte[] headerBuf = new byte[4];

            try
            {
                while (_client.Connected)
                {
                    await ReadExactAsync(headerBuf, 4);
                    int size = BitConverter.ToInt32(headerBuf, 0);

                    byte[] data = new byte[size];
                    await ReadExactAsync(data, size);

                    _commandHandler?.Handle(data);
                }
            }
            catch
            {
                Console.WriteLine("[수신] 연결 끊김");
                Disconnect();
            }
        }

        private async Task ReadExactAsync(byte[] buf, int size)
        {
            int received = 0;
            while (received < size)
            {
                int read = await _stream.ReadAsync(buf, received, size - received);
                if (read == 0) throw new Exception("연결 종료");
                received += read;
            }
        }
    }
}

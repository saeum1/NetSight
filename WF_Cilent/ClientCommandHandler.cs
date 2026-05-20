using System;
using System.Collections.Generic;
using System.Text;

namespace WF_Client
{
    public class ClientCommandHandler
    {
        private FocusModeService _focusModeService;
        private TcpClientService _tcpService;
        private InputSimulator _inputSimulator;
        public ClientCommandHandler(TcpClientService tcpService, FocusModeService focusModeService)
        {
            _tcpService = tcpService;
            _focusModeService = focusModeService;
            _inputSimulator = new InputSimulator();
        }

        public void Handle(byte[] data)
        {
            if (data.Length == 0) return;

            byte commandType = data[0];
            switch (commandType)
            {
                case 0x05: // 연결 종료
                    _tcpService.Disconnect();
                    Console.WriteLine("[명령] 연결 종료");
                    break;

                case PacketType.DuplicateName: // 중복 이름으로 인한 접속 거부
                    _tcpService.Disconnect();
                    Application.OpenForms[0]?.Invoke(() =>
                        MessageBox.Show(
                            "이미 같은 이름으로 접속된 PC가 있습니다.\nPC 이름을 변경 후 다시 연결해 주세요.",
                            "연결 거부 - 중복된 이름",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning));
                    Console.WriteLine("[명령] 중복 이름으로 연결 거부됨");
                    break;

                case 0x21: // 집중 모드 시작
                    string joined = Encoding.UTF8.GetString(data, 1, data.Length - 1);
                    List<string> blockedProcesses = joined.Split('|', StringSplitOptions.RemoveEmptyEntries)
                                                          .Select(x => x.Trim()).ToList();

                    //_tcpService.StartMonitoring(blockedProcesses);
                    _tcpService.StartFocusMode(blockedProcesses);  // ← 이거 추가 (키보드 훅, 작업표시줄)
                    _focusModeService.Activate();
                    break;

                case 0x22: // 집중 모드 해제
                    //_tcpService.StopMonitoring();   // ← StopMonitoring도 같이 호출
                    _tcpService.StopFocusMode();
                    _focusModeService.Deactivate();
                    break;

                case 0x30:
                    int x = BitConverter.ToInt32(data, 1);
                    int y = BitConverter.ToInt32(data, 5);
                    byte action = data[9];
                    int delta = BitConverter.ToInt32(data, 10);
                    _inputSimulator.SimulateMouse(x, y, action, delta);
                    break;

                case 0x31: // 키보드 이벤트
                    int vk = BitConverter.ToInt32(data, 1);
                    byte isDown = data[5];
                    _inputSimulator.SimulateKeyboard(vk, isDown);
                    break;

                case PacketType.STPK_SEND_MESSAGE: // 서버→클라 메시지 수신
                    string serverMsg = Encoding.UTF8.GetString(data, 1, data.Length - 1);
                    Application.OpenForms[0]?.Invoke(() =>
                        MessageBox.Show(
                            serverMsg,
                            "교사 메시지",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information));
                    Console.WriteLine($"[메시지 수신] {serverMsg}");
                    break;
            }
        }
    }
}

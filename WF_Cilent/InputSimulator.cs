using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace WF_Client
{
    public class InputSimulator
    {
        // --- Win32 API 선언 ---

        // [추가] 마우스 커서 위치를 OS 절대 좌표로 이동시키는 Win32 API
        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out Win32Point lpPoint);

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

        // Win32 전용 가벼운 구조체 정의 (WPF Point 구조체 대신 사용)
        [StructLayout(LayoutKind.Sequential)]
        private struct Win32Point
        {
            public int X;
            public int Y;
        }

        // mouse_event 플래그 정의
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        private const uint MOUSEEVENTF_WHEEL = 0x0800;

        // keybd_event 플래그 정의
        private const uint KEYEVENTF_KEYUP = 0x0002;

        // 주도권 관리를 위한 필드
        private DateTime _lastLocalInputTime = DateTime.MinValue;
        private Win32Point _lastMousePos;

        public InputSimulator()
        {
            GetCursorPos(out _lastMousePos);
        }

        /// <summary>
        /// SENDER 호스트(로컬)의 마우스 움직임을 감지하여 마지막 입력 시간을 갱신
        /// </summary>
        public void UpdateLocalInputStatus()
        {
            if (GetCursorPos(out Win32Point currentPos))
            {
                if (currentPos.X != _lastMousePos.X || currentPos.Y != _lastMousePos.Y)
                {
                    _lastLocalInputTime = DateTime.Now;
                    _lastMousePos = currentPos;
                }
            }
        }

        /// <summary>
        /// RECEIVER가 현재 원격 제어를 할 수 있는 상태인지 검사 (3초 규칙)
        /// </summary>
        public bool CanRemoteControl()
        {
            return (DateTime.Now - _lastLocalInputTime).TotalSeconds >= 3;
        }

        /// <summary>
        /// 수신된 패킷을 기반으로 마우스 동작 시뮬레이션 (WPF 버전)
        /// </summary>
        public void SimulateMouse(int x, int y, byte action, int delta = 0)
        {
            if (!CanRemoteControl()) return;

            // [수정] Win32 API를 사용하여 화면 전역 절대 좌표로 커서 이동
            SetCursorPos(x, y);

            // 이동 후 좌표를 로컬 마지막 좌표에 동기화해서 내 움직임으로 오인하지 않게 차단
            _lastMousePos = new Win32Point { X = x, Y = y };

            // 클릭 액션 수행
            PacketType.MouseAction mouseAction = (PacketType.MouseAction)action;
            switch (mouseAction)
            {
                case PacketType.MouseAction.LeftDown:
                    mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                    break;
                case PacketType.MouseAction.LeftUp:
                    mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                    break;
                case PacketType.MouseAction.RightDown:
                    mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0);
                    break;
                case PacketType.MouseAction.RightUp:
                    mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
                    break;
                case PacketType.MouseAction.Wheel:
                    mouse_event(MOUSEEVENTF_WHEEL, 0, 0, (uint)delta, 0);
                    break;
            }
        }

        /// <summary>
        /// 수신된 패킷을 기반으로 키보드 동작 시뮬레이션
        /// </summary>
        public void SimulateKeyboard(int virtualKey, byte isDown)
        {
            if (!CanRemoteControl()) return;

            byte vk = (byte)virtualKey;
            uint flags = (isDown == 0) ? KEYEVENTF_KEYUP : 0;

            keybd_event(vk, 0, flags, 0);
        }
    }
}

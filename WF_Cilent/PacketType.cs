using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace WF_Client
{
    public class PacketType
    {
        // 클라 → 서버
        public const byte MonitorFrame = 0x02;
        public const byte FocusProcess = 0x20;
        public const byte ProcessKillAlert = 0x23;
        public const byte IsNotExistProcess = 0x24;
        public const byte CTPK_SEND_MESSAGE = 0x41; // 클라→서버 메시지

        // 서버 → 클라
        public const byte FocusModeOn = 0x21;
        public const byte FocusModeOff = 0x22;
        public const byte FocusGuardResult = 0x25;
        public const byte DuplicateName = 0x06; // 중복 이름 거부
        public const byte STPK_SEND_MESSAGE = 0x40; // 서버→클라 메시지

        // 서버 → 클라 (원격 제어)
        public const byte MouseEvent = 0x30;
        public const byte KeyEvent = 0x31;

        // 마우스 액션 종류
        public enum MouseAction : byte
        {
            Move = 0,
            LeftDown = 1,
            LeftUp = 2,
            RightDown = 3,
            RightUp = 4,
            Wheel = 5,
            DoubleLeftClick = 6
        }

        // 마우스 패킷 구조체 (9바이트)
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct MouseEventPacket
        {
            public int X;
            public int Y;
            public byte Action;
        }

        // 키보드 패킷 구조체 (5바이트)
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct KeyEventPacket
        {
            public int VirtualKey;
            public byte IsDown;
        }
    }
}

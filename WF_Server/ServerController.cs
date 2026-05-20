using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace WF_Server
{
    public class ServerController
    {
        // 집중 모드 시작 - 허용 프로세스 지정
        public async Task SendFocusMode(ClientSession session, List<string> blockedProcesses)
        {
            if (session.IsFocusMode) return; // 이미 집중 모드면 스킵

            string joined = string.Join("|", blockedProcesses);
            byte[] dataBytes = Encoding.UTF8.GetBytes(joined);
            byte[] packet = new byte[1 + dataBytes.Length]; // 패킷 타입 1바이트 + 데이터 바이트 크기로 배열 할당
            packet[0] = PacketType.FocusModeOn; // 첫 번째 바이트에 패킷 타입(0x21) 세팅

            Buffer.BlockCopy(dataBytes, 0, packet, 1, dataBytes.Length); // dataBytes를 packet의 1번째 바이트부터 복사
            //이게 뭔 개소리냐면 잘 보셈 -> 기본 dataBytes에는 프로세스 리스트만 담겨잇음
            //근데 packet배열에 패킷 크기 + 리스트 크기의 배열을 할당했잖
            //그 다음 [0]에 패킷id 넣어주고, 나머지 공간에 dattaBytes에 넣어둔 프로세스 리스트 넣는거임

            await session.SendAsync(packet);
            session.IsFocusMode = true;
        }

        // 집중 모드 해제
        public async Task SendFocusModeOff(ClientSession session)
        {
            if (!session.IsFocusMode) return; // 이미 해제 상태면 스킵

            await session.SendAsync(new byte[] { PacketType.FocusModeOff }); // 집중모드 껐다는 패킷 보냄

            session.IsFocusMode = false;
        }

        //연결 종료
        public async Task SendDisconnect(ClientSession session)
        {
            await session.SendAsync(new byte[] { 0x05 }); //연결 종료라는 패킷 보냄
            session.Disconnect();
        }

        public async Task SendMouseEvent(ClientSession session, int x, int y, byte action, int delta = 0)
        {
            var packet = new PacketType.MouseEventPacket { X = x, Y = y, Action = action, Delta = delta };
            byte[] data = StructureToBytes(packet);
            byte[] sendBuffer = new byte[1 + data.Length];
            sendBuffer[0] = PacketType.MouseEvent;
            Buffer.BlockCopy(data, 0, sendBuffer, 1, data.Length);
            await session.SendAsync(sendBuffer);
        }

        public async Task SendKeyEvent(ClientSession session, int virtualKey, byte isDown)
        {
            var packet = new PacketType.KeyEventPacket { VirtualKey = virtualKey, IsDown = isDown };
            byte[] data = StructureToBytes(packet);
            byte[] sendBuffer = new byte[1 + data.Length];
            sendBuffer[0] = PacketType.KeyEvent;
            Buffer.BlockCopy(data, 0, sendBuffer, 1, data.Length);
            await session.SendAsync(sendBuffer);
        }

        public async Task SendMessage(ClientSession session, string message)
        {
            byte[] msgBytes = Encoding.UTF8.GetBytes(message);
            byte[] packet = new byte[1 + msgBytes.Length];
            packet[0] = PacketType.STPK_SEND_MESSAGE;
            Buffer.BlockCopy(msgBytes, 0, packet, 1, msgBytes.Length);
            await session.SendAsync(packet);
        }

        //아마 니가 처음보는 문법일텐데 마샬링이라고 함
        //메모리 관리랑 비관리 메모리 사이를 변환하는 작업임
        private byte[] StructureToBytes<T>(T str) where T : struct
        {
            int size = Marshal.SizeOf(str);
            byte[] arr = new byte[size];
            IntPtr ptr = Marshal.AllocHGlobal(size); //니가 환장하는 c++나왔음 void*임 저게 ㅇㅎ
            //alloc이라는 거 CT 제작할 때 본듯? 최선우랑 어울리니까 CT부터 생각나는게 인생 망했노 ㄴ 그게 무슨 말임 지금
            try
            { //결국 요약하면 GC가 자꾸 객체 이동시키니까 못하게 고정 주소로 바꾸는거임
                //치엔에서 초록색 정적 주소랑 같은거임? ㅇㅇ

                Marshal.StructureToPtr(str, ptr, true);
                Marshal.Copy(ptr, arr, 0, size);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
            return arr;
        }
    }
}

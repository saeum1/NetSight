using System;
using System.Collections.Generic;
using System.Text;

namespace WF_Server
{
    public class ServerPacketRouter
    {
        public event Action<ClientSession> OnConnect;
        public event Action<ClientSession> OnDisconnect;
        public event Action<ClientSession, byte[]> OnMonitorFrame;
        public event Action<ClientSession, string> OnError;
        public event Action<ClientSession, string> OnFocusProcessReport; // 클라 포커스 프로세스 수신
        public event Action<ClientSession, string> OnProcessKillAlert;
        public event Action<ClientSession, string> OnIsNotExistProcess;
        public event Action<ClientSession, string, bool> OnFocusGuardResult;
        public event Action<ClientSession, string> OnMessageReceived; // 클라→서버 메시지

        //이건 설명할 거 없음.
        public void Route(ClientSession session, byte[] data)
        {
            if (data.Length == 0) return;

            byte packetType = data[0];

            switch (packetType)
            {
                case 0x01: //사실 상 안씀. 차피 세션에서 관리 전부 해줌.
                    OnConnect?.Invoke(session);
                    break;

                case PacketType.MonitorFrame:
                    OnMonitorFrame?.Invoke(session, data[1..]);
                    break;

                case 0x04:
                    string msg = System.Text.Encoding.UTF8.GetString(data, 1, data.Length - 1);
                    OnError?.Invoke(session, msg);
                    break;

                case 0x05:  //이새끼도 0x01과 동일함.
                    OnDisconnect?.Invoke(session);
                    session.Disconnect();
                    break;

                case PacketType.FocusProcess:
                    string processName = Encoding.UTF8.GetString(data, 1, data.Length - 1);
                    OnFocusProcessReport?.Invoke(session, processName);
                    break;

                case PacketType.ProcessKillAlert:
                    string killedProcess = Encoding.UTF8.GetString(data, 1, data.Length - 1);
                    OnProcessKillAlert?.Invoke(session, killedProcess);
                    break;

                case 0x24:
                    string missingProcess = Encoding.UTF8.GetString(data, 1, data.Length - 1);
                    OnIsNotExistProcess?.Invoke(session, missingProcess);
                    break;

                case 0x25:
                    string raw = Encoding.UTF8.GetString(data, 1, data.Length - 1);
                    var parts = raw.Split('|');
                    bool allowed = parts[1] == "정상";
                    OnFocusGuardResult?.Invoke(session, parts[0], allowed);
                    break;

                case PacketType.CTPK_SEND_MESSAGE: // 클라→서버 메시지
                    string clientMsg = Encoding.UTF8.GetString(data, 1, data.Length - 1);
                    OnMessageReceived?.Invoke(session, clientMsg);
                    break;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Xml.Linq;

namespace WF_Server
{
    public class ClientSession
    {
        public string PcName { get; private set; }
        public string SessionId { get; private set; }
        public TcpClient TcpClient { get; private set; }
        public NetworkStream Stream { get; private set; }
        public bool IsConnected { get; private set; }
        public Bitmap CurrentImage { get; set; }
        public string CurrentProcess { get; set; }
        public bool IsFocusMode { get; set; } = false;

        public event Action<ClientSession, byte[]> OnPacketReceived; //패킷 이벤트
        public event Action<ClientSession> OnDisconnected; //연결 해제 이벤트
        public event Action<ClientSession> OnHandshakeCompleted; // 핸드쉐이크 완료 후 PC이름 확정 이벤트

        public ClientSession(TcpClient client) //생성자에서 현재 접속한 클라의 세션 등록
        {
            TcpClient = client;
            Stream = client.GetStream();
            SessionId = client.Client.RemoteEndPoint.ToString();
            IsConnected = true;
        }

        private async Task ReadExactAsync(byte[] buf, int size) //TCP에서 원하는 바이트 수 만큼 읽어줌
        {//TCP는 스트림 기반이라 한 번에 모든 바이트 수가 온다는 보장 없음
            int received = 0;
            while (received < size)
            {
                int read = await Stream.ReadAsync(buf, received, size - received);
                if (read == 0) throw new Exception("연결 종료");
                received += read;
            } //return이 없어요.. -> buf가 배열인데 뭔 상관임 님 천재임?
        }

        public void StartReceive()
        {
            Task.Run(() => HandshakeAndReceive());
        }

        private async Task HandshakeAndReceive()
        {
            try
            {
                // 1. PC 이름 먼저 읽기
                byte[] nameLenBuf = new byte[4];
                await ReadExactAsync(nameLenBuf, 4);
                int nameLen = BitConverter.ToInt32(nameLenBuf, 0); //배열을 다시 int로 바꿈(길이니까)

                byte[] nameBuf = new byte[nameLen];
                await ReadExactAsync(nameBuf, nameLen);
                PcName = System.Text.Encoding.UTF8.GetString(nameBuf); //이새끼는 문자열로 바꿔줘야함.

                Console.WriteLine($"[핸드쉐이크] {SessionId} / 이름: {PcName}");

                // 핸드쉐이크 완료 → 이름 중복 체크는 구독자(Form1)에서 처리
                OnHandshakeCompleted?.Invoke(this);

                // 2. 이후 패킷 수신 루프
                await ReceiveLoop();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[핸드쉐이크 실패] {ex.Message} / {ex.StackTrace}");
                Disconnect();
            }
        }

        private async Task ReceiveLoop()
        {
            byte[] headerBuf = new byte[4];

            try
            {
                while (IsConnected)
                {
                    await ReadExactAsync(headerBuf, 4);
                    int size = BitConverter.ToInt32(headerBuf, 0); //위랑 같음

                    byte[] data = new byte[size];
                    await ReadExactAsync(data, size);

                    OnPacketReceived?.Invoke(this, data);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ReceiveLoop 에러] {ex.Message}"); // 추가
                Disconnect();
            }
        }

        public async Task SendAsync(byte[] data)
        {
            if (!IsConnected) return;

            try
            {
                byte[] header = BitConverter.GetBytes(data.Length);
                await Stream.WriteAsync(header, 0, 4); // header 배열을 0번째부터 4바이트 전송 (길이 헤더)
                await Stream.WriteAsync(data, 0, data.Length); // data 배열을 0번째부터 data.Length만큼 전송 (실제 데이터)
            }
            catch
            {
                Disconnect();
            }
        }

        public void Disconnect()
        {
            if (!IsConnected) return;
            IsConnected = false;
            TcpClient.Close();
            OnDisconnected?.Invoke(this);
        }
    }
}

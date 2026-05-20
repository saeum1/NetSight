using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace WF_Server
{
    public class TcpServer
    {
        private TcpListener _listener;
        private bool _isRunning;

        public event Action<ClientSession> OnClientAccepted;

        public void Start(int port)
        {
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();
            _isRunning = true;
            Task.Run(() => AcceptLoop());
        }

        public void Stop()
        {
            _isRunning = false;
            _listener?.Stop();
        }

        private async Task AcceptLoop() //이거도 할 거 없잖음 dd
        {
            while (_isRunning)
            {
                TcpClient client = await _listener.AcceptTcpClientAsync();
                var session = new ClientSession(client);
                OnClientAccepted?.Invoke(session);
                session.StartReceive();
            }
        }
    }
}

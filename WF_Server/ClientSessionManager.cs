using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace WF_Server
{
    public class ClientSessionManager
    {
        private ConcurrentDictionary<string, ClientSession> _sessions // 세션 저장소, Key = SessionId(IP:Port), Value = ClientSession
            = new ConcurrentDictionary<string, ClientSession>(); // ConcurrentDictionary인 이유 = 여러 스레드에서 동시에 추가/삭제해도 안전하게

        public event Action<ClientSession> OnSessionAdded;
        public event Action<ClientSession> OnSessionRemoved;
        // 세션 추가/제거 시 외부에서 구독할 수 있는 이벤트
        // Form1에서 여기 구독해서 카드 추가/제거함

        public void Add(ClientSession session)
        {
            _sessions[session.SessionId] = session;
            session.OnDisconnected += Remove;
            OnSessionAdded?.Invoke(session);
        }

        public void Remove(ClientSession session)
        {
            _sessions.TryRemove(session.SessionId, out _);
            OnSessionRemoved?.Invoke(session);
        }

        public ClientSession Get(string sessionId)
        {
            _sessions.TryGetValue(sessionId, out var session); // SessionId로 특정 세션 찾아서 반환, 없으면 null
            return session;
        }

        public IEnumerable<ClientSession> GetAll() => _sessions.Values; // 현재 접속 중인 모든 세션 반환 (전체 집중모드 ON/OFF 할 때 씀)

        public bool IsPcNameDuplicate(string pcName) // 동일한 PC 이름이 이미 접속 중인지 확인
        {
            return _sessions.Values.Any(s => s.PcName != null &&
                s.PcName.Equals(pcName, StringComparison.OrdinalIgnoreCase));
        }
    }
}

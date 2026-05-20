using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using System.Diagnostics.Eventing.Reader;
using System.Text;

namespace WF_Server.DBManager
{
    internal class DB
    {
        private static readonly string dbFileName = "OperationHistory.db";
        public static string ConnectionString => $"Data Source={dbFileName}";

        // 6. 단일 통합 이벤트 로그 테이블 (EventLog) 생성 및 초기화
        public static void Initialize()
        {
            // 2. 연결을 열어서 테이블 생성 및 더미 데이터 삽입 처리
            using (var connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();

                // [순서 변경] 테이블을 먼저 완벽하게 생성합니다.
                string createEventLog = @"
                CREATE TABLE IF NOT EXISTS EventLog (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    StudentId TEXT,
                    StudentName TEXT,
                    EventType TEXT,
                    Result TEXT,
                    Message TEXT,
                    CreatedAt TEXT
                );";

                using (var command = new SqliteCommand(createEventLog, connection))
                {
                    command.ExecuteNonQuery();
                }

                // [순서 변경] 테이블이 만들어진 후, 데이터가 비어있는지 확인합니다.
                string checkEmpty = "SELECT COUNT(*) FROM EventLog";
                long count = 0;
                using (var checkCmd = new SqliteCommand(checkEmpty, connection))
                {
                    count = (long)checkCmd.ExecuteScalar();
                }

                // 데이터가 정말 아예 없다면 테스트용 더미 행 딱 1개 삽입
                if (count == 0)
                {
                    string insertInitial = @"
                    INSERT INTO EventLog (StudentId, StudentName, EventType, Result, Message, CreatedAt) VALUES 
                    ('192.168.0.50:50001', '홍길동 PC', 'StudentConnected', 'Success', '테스트용 학생 PC가 정상 접속되었습니다.', '2026-05-18 19:24:00');";

                    using (var insertCmd = new SqliteCommand(insertInitial, connection))
                    {
                        insertCmd.ExecuteNonQuery();
                    }
                }
            }
        }

        // 로그 저장용 통합 메서드
        public static void SaveLog(string studentId, string studentName, string eventType, string result, string message)
        {
            try
            {
                using (var conn = new SqliteConnection(ConnectionString))
                {
                    conn.Open();
                    string sql = "INSERT INTO EventLog (StudentId, StudentName, EventType, Result, Message, CreatedAt) VALUES (@sid, @sname, @type, @res, @msg, @time)";
                    using (var cmd = new SqliteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@sid", studentId);
                        cmd.Parameters.AddWithValue("@sname", studentName);
                        cmd.Parameters.AddWithValue("@type", eventType);
                        cmd.Parameters.AddWithValue("@res", result);
                        cmd.Parameters.AddWithValue("@msg", message);
                        cmd.Parameters.AddWithValue("@time", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DB 저장 실패 방어: {ex.Message}");
            }
        }

        // 7. 최근 로그 영역 매핑용 최신 로그 조회
        public static List<EventLogRecord> GetRecentLogs(int limit = 50)
        {
            var list = new List<EventLogRecord>();
            try
            {
                using (var conn = new SqliteConnection(ConnectionString))
                {
                    conn.Open();
                    string query = $"SELECT Id, StudentId, StudentName, EventType, Result, Message, CreatedAt FROM EventLog ORDER BY Id DESC LIMIT {limit};";
                    using (var cmd = new SqliteCommand(query, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new EventLogRecord
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                StudentId = reader["StudentId"].ToString(),
                                StudentName = reader["StudentName"].ToString(),
                                EventType = reader["EventType"].ToString(),
                                Result = reader["Result"].ToString(),
                                Message = reader["Message"].ToString(),
                                CreatedAt = reader["CreatedAt"].ToString()
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DB 조회 실패 방어: {ex.Message}");
            }
            return list;
        }
    }
}

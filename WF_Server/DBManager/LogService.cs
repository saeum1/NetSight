using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using System.Text;

namespace WF_Server.DBManager
{
    public static class LogService
    {
        // [ ] DB 저장 실패가 전체 프로그램 종료로 이어지지 않도록 철저한 try-catch 예외 처리 적용
        public static void SaveConnectionLog(string clientId, string clientName, string eventType, string message)
        {
            try
            {
                using (var conn = new SqliteConnection(DB.ConnectionString))
                {
                    conn.Open();
                    string sql = "INSERT INTO ConnectionLog (ClientId, ClientName, EventType, Message, CreatedAt) VALUES (@cid, @cname, @type, @msg, @time)";
                    using (var cmd = new SqliteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@cid", clientId);
                        cmd.Parameters.AddWithValue("@cname", clientName);
                        cmd.Parameters.AddWithValue("@type", eventType);
                        cmd.Parameters.AddWithValue("@msg", message);
                        cmd.Parameters.AddWithValue("@time", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine($"로그 저장 예외 방어: {ex.Message}"); }
        }

        public static void SaveCommandLog(string clientId, string clientName, string commandType, string result, string message)
        {
            try
            {
                using (var conn = new SqliteConnection(DB.ConnectionString))
                {
                    conn.Open();
                    string sql = "INSERT INTO CommandLog (ClientId, ClientName, CommandType, Result, Message, CreatedAt) VALUES (@cid, @cname, @type, @res, @msg, @time)";
                    using (var cmd = new SqliteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@cid", clientId);
                        cmd.Parameters.AddWithValue("@cname", clientName);
                        cmd.Parameters.AddWithValue("@type", commandType);
                        cmd.Parameters.AddWithValue("@res", result);
                        cmd.Parameters.AddWithValue("@msg", message);
                        cmd.Parameters.AddWithValue("@time", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine($"로그 저장 예외 방어: {ex.Message}"); }
        }

        public static void SaveErrorLog(string clientId, string clientName, string errorType, string message)
        {
            try
            {
                using (var conn = new SqliteConnection(DB.ConnectionString))
                {
                    conn.Open();
                    string sql = "INSERT INTO ErrorLog (ClientId, ClientName, ErrorType, Message, CreatedAt) VALUES (@cid, @cname, @type, @msg, @time)";
                    using (var cmd = new SqliteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@cid", clientId);
                        cmd.Parameters.AddWithValue("@cname", clientName);
                        cmd.Parameters.AddWithValue("@type", errorType);
                        cmd.Parameters.AddWithValue("@msg", message);
                        cmd.Parameters.AddWithValue("@time", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine($"로그 저장 예외 방어: {ex.Message}"); }
        }
    }
}

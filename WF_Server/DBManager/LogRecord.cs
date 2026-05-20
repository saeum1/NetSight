using System;
using System.Collections.Generic;
using System.Text;

namespace WF_Server.DBManager
{
    public class EventLogRecord
    {
        public int Id { get; set; }
        public string StudentId { get; set; }
        public string StudentName { get; set; }
        public string EventType { get; set; }
        public string Result { get; set; }
        public string Message { get; set; }
        public string CreatedAt { get; set; }
    }
}

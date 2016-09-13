using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevAudit.AuditLibrary
{
    public enum EventMessageType
    {
        SUCCESS = 0,
        ERROR = 1,
        INFO = 2,
        WARNING = 3
    }

    public class EnvironmentEventArgs
    {
        public EventMessageType MessageType { get; protected set; }

        public string Message { get; set; }

        public EnvironmentEventArgs(EventMessageType message_type, string message)
        {
            this.MessageType = message_type;
            this.Message = message;
        }
    }
}

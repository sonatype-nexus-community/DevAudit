using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevAudit.AuditLibrary
{
    public class EnvironmentEventArgs
    {
        public EventMessageType MessageType { get; protected set; }
        public string Message { get; protected set; }
        public DateTime DateTime { get; protected set; } = DateTime.UtcNow;
        public CallerInformation? Caller { get; protected set; }
        public string EnvironmentLocation { get; internal set; }

        public EnvironmentEventArgs(EventMessageType message_type, string message)
        {
            this.MessageType = message_type;
            this.Message = message;
        }

        public EnvironmentEventArgs(EventMessageType message_type, string message_format, object[] m)
        {
            this.MessageType = message_type;
            this.Message = string.Format(message_format, m);
        }

        public EnvironmentEventArgs(CallerInformation caller, EventMessageType message_type, string message_format, object[] m)
        {
            this.Caller = caller;
            this.MessageType = message_type;
            this.Message = string.Format(message_format, m);
        }
    }
}

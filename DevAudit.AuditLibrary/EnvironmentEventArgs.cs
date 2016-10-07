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
        public OperationProgress? Progress { get; protected set; }
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
        public EnvironmentEventArgs(OperationProgress p)
        {
            this.MessageType = EventMessageType.PROGRESS;
            this.Progress = p;
            this.Message = string.Format("{0} {1} of {2}", p.Operation, p.Complete, p.Total);
            if (p.Time.HasValue)
            {
                this.Message += string.Format(" in {0} ms.", p.Time.Value.Milliseconds);
            }
        }
    }
}

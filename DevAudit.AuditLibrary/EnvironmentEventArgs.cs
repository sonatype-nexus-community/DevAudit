using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace DevAudit.AuditLibrary
{
    public class EnvironmentEventArgs
    {
        public EventMessageType MessageType { get; protected set; }
        public Thread CurrentThread;
        public string Message { get; protected set; }
        public DateTime DateTime { get; protected set; } = DateTime.UtcNow;
        public CallerInformation? Caller { get; protected set; }
        public Exception Exception { get; protected set; }
        public OperationProgress? Progress { get; protected set; }
        public string EnvironmentLocation { get; internal set; }

        public EnvironmentEventArgs(EventMessageType message_type, string message)
        {
            this.CurrentThread = Thread.CurrentThread;
            this.MessageType = message_type;
            this.Message = message;
        }

        public EnvironmentEventArgs(EventMessageType message_type, string message_format, object[] m)
        {
            this.CurrentThread = Thread.CurrentThread;
            this.MessageType = message_type;
            this.Message = string.Format(message_format, m);
        }

        public EnvironmentEventArgs(CallerInformation caller, EventMessageType message_type, string message_format, object[] m)
        {
            this.CurrentThread = Thread.CurrentThread;
            this.Caller = caller;
            this.MessageType = message_type;
            this.Message = string.Format(message_format, m);
        }

        public EnvironmentEventArgs(CallerInformation caller, Exception e)
        {
            this.CurrentThread = Thread.CurrentThread;
            this.Caller = caller;
            this.MessageType = EventMessageType.ERROR;
            this.Message = string.Format("Exception occurred.");
            this.Exception = e;
        }
        public EnvironmentEventArgs(OperationProgress p)
        {
            this.CurrentThread = Thread.CurrentThread;
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

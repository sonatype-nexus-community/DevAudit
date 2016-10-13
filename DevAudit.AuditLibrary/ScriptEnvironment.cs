using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevAudit.AuditLibrary
{
    public class ScriptEnvironment
    {
        #region Constructors
        public ScriptEnvironment(LocalEnvironment host_env)
        {
            this.HostEnvironment = host_env;
        }
        #endregion

        #region Events
        public event EventHandler<EnvironmentEventArgs> MessageHandler;
        #endregion

        #region Public properties
        private LocalEnvironment HostEnvironment { get; set; }
        
        public bool IsWindows
        {
            get
            {
                return this.HostEnvironment.IsWindows;
            }
        }
        public bool IsUnix
        {
            get
            {
                return this.HostEnvironment.IsWindows;
            }
        }
        #endregion

        #region Public methods
        public void Message(EventMessageType message_type, string message_format, params object[] message)
        {
            OnMessage(new EnvironmentEventArgs(message_type, message_format, message));
        }

        public void Info(string message_format, params object[] message)
        {
            OnMessage(new EnvironmentEventArgs(EventMessageType.INFO, message_format, message));
        }

        internal void Error(string message_format, params object[] message)
        {
            OnMessage(new EnvironmentEventArgs(EventMessageType.ERROR, message_format, message));
        }

        internal void Error(CallerInformation caller, string message_format, params object[] message)
        {
            OnMessage(new EnvironmentEventArgs(caller, EventMessageType.ERROR, message_format, message));
        }

        internal void Error(Exception e)
        {
            OnMessage(new EnvironmentEventArgs(EventMessageType.ERROR, "Exception: {0} at {1}.",
                new object[2] { e.Message, e.StackTrace }));
        }

        internal void Success(string message_format, params object[] message)
        {
            OnMessage(new EnvironmentEventArgs(EventMessageType.SUCCESS, message_format, message));
        }

        internal void Warning(string message_format, params object[] message)
        {
            OnMessage(new EnvironmentEventArgs(EventMessageType.WARNING, message_format, message));
        }

        #endregion

        #region Protected methods
        protected virtual void OnMessage(EnvironmentEventArgs e)
        {
            lock (script_message_lock)
            {
                MessageHandler?.Invoke(this, e);
            }
        }
        #endregion

        #region Private fields
        public object script_message_lock = new object();
        #endregion
    }
}

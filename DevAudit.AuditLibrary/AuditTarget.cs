using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevAudit.AuditLibrary
{
    public abstract class AuditTarget
    {
        #region Events
        protected event EventHandler<EnvironmentEventArgs> HostEnvironmentMessageHandler;
        protected event EventHandler<EnvironmentEventArgs> AuditEnvironmentMessageHandler;
        protected event EventHandler<EnvironmentEventArgs> ControllerMessageHandler;
        #endregion

        #region Public properties
        public string Id { get; protected set; }
        public string Label { get; protected set; }
        public Dictionary<string, object> AuditOptions { get; set; } = new Dictionary<string, object>();
        public AuditEnvironment HostEnvironment { get; protected set; }
        public AuditEnvironment AuditEnvironment { get; protected set; }
        #endregion

        #region Constructors
        public AuditTarget(Dictionary<string, object> audit_options, EventHandler<EnvironmentEventArgs> controller_message_handler = null)
        {
            if (ReferenceEquals(audit_options, null)) throw new ArgumentNullException("audit_options");
            this.AuditOptions = audit_options;
            this.ControllerMessageHandler = controller_message_handler;
            this.HostEnvironmentMessageHandler = AuditTarget_HostEnvironmentMessageHandler;
            this.HostEnvironment = new LocalEnvironment(this.HostEnvironmentMessageHandler);
            if (this.AuditOptions.Keys.Contains("RemoteHost"))
            {
                if (this.AuditOptions.Keys.Contains("RemoteUser") && this.AuditOptions.Keys.Contains("RemotePass"))
                {
                    this.AuditEnvironment = new SshEnvironment(this.HostEnvironmentMessageHandler, (string)this.AuditOptions["RemoteHost"],
                        (string)this.AuditOptions["RemoteUser"], this.AuditOptions["RemotePass"]);

                }

            }
        }

        private void AuditTarget_HostEnvironmentMessageHandler(object sender, EnvironmentEventArgs e)
        {
            e.Message = string.Format("Host environment message: ", e.Message);
            this.ControllerMessageHandler.Invoke(sender, e);
        }

        #endregion
    }
}

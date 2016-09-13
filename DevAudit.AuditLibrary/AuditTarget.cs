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
        public event EventHandler<EnvironmentEventArgs> HostEnvironmentMessageHandler;
        public event EventHandler<EnvironmentEventArgs> AuditEnvironmentMessageHandler;

        protected virtual void OnHostEnvironmentMessage(EnvironmentEventArgs e)
        {
            HostEnvironmentMessageHandler?.Invoke(this, e);
        }
        protected virtual void OnAuditEnvironmentMessage(EnvironmentEventArgs e)
        {
            AuditEnvironmentMessageHandler?.Invoke(this, e);
        }
        #endregion

        #region Public properties
        public string Id { get; protected set; }
        public string Label { get; protected set; }
        public Dictionary<string, object> AuditOptions { get; set; } = new Dictionary<string, object>();
        public AuditEnvironment HostEnvironment { get; protected set; } = new LocalEnvironment();
        public AuditEnvironment AuditEnvironment { get; protected set; }
        #endregion

        #region Constructors
        public AuditTarget() {}

        public AuditTarget (Dictionary<string, object> audit_options)
        {
            if (ReferenceEquals(audit_options, null)) throw new ArgumentNullException("audit_options");
            this.AuditOptions = audit_options;
            this.HostEnvironment = new LocalEnvironment();
            this.HostEnvironment.MessageHandler += HostEnvironment_MessageHandler;
            if (this.AuditOptions.Keys.Contains("RemoteHost"))
            {
                if (this.AuditOptions.Keys.Contains("RemoteUser") && this.AuditOptions.Keys.Contains("RemotePass"))
                {
                    this.AuditEnvironment = new SshEnvironment((string) this.AuditOptions["RemoteHost"],
                        (string)this.AuditOptions["RemoteUser"], this.AuditOptions["RemotePass"]);
                    this.AuditEnvironmentMessageHandler += AuditTarget_AuditEnvironmentMessageHandler;
                }

            }

        }

        private void AuditTarget_AuditEnvironmentMessageHandler(object sender, EnvironmentEventArgs e)
        {
            OnAuditEnvironmentMessage(e);
        }

        private void HostEnvironment_MessageHandler(object sender, EnvironmentEventArgs e)
        {
            OnHostEnvironmentMessage(e);
        }
        #endregion
    }
}

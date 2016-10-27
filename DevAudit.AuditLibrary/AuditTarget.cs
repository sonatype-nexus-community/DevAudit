using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevAudit.AuditLibrary
{
    public abstract class AuditTarget
    {
        public enum AuditResult
        {
            SUCCESS = 0,
            INVALID_AUDIT_TARGET_OPTIONS,
            ERROR_CREATING_AUDIT_TARGET,
            ERROR_SCANNING_MODULES,
            ERROR_SCANNING_PACKAGES,
            ERROR_SEARCHING_ARTIFACTS,
            ERROR_SEARCHING_VULNERABILITIES,
            ERROR_EVALUATING_VULNERABILITIES,
            ERROR_SCANNING_VERSION,
            ERROR_SCANNING_DEFAULT_CONFIGURATION_RULES,
            ERROR_SEARCHING_CONFIGURATION_RULES,
            ERROR_EVALUATING_CONFIGURATION_RULES,
            ERROR_SCANNING_PROJECTS,
            ERROR_ANALYZING,
            ERROR_SCANNING_WORKSPACE,
            ERROR_SCANNING_ANALYZERS,
            ERROR_RUNNING_ANALYZERS,
        }

        #region Events
        protected event EventHandler<EnvironmentEventArgs> HostEnvironmentMessageHandler;
        protected event EventHandler<EnvironmentEventArgs> AuditEnvironmentMessageHandler;
        protected event EventHandler<EnvironmentEventArgs> ControllerMessageHandler;
        #endregion

        #region Public properties
        public string Id { get; protected set; }
        public string Label { get; protected set; }
        public Dictionary<string, object> AuditOptions { get; set; } = new Dictionary<string, object>();
        public LocalEnvironment HostEnvironment { get; protected set; }
        public AuditEnvironment AuditEnvironment { get; protected set; }
        public bool HostEnvironmentInitialised { get; private set; } = false;
        public bool AuditEnvironmentIntialised { get; private set; } = false;
        public bool UseAsyncMethods { get; private set; } = false;
        #endregion

        #region Constructors
        public AuditTarget(Dictionary<string, object> audit_options, EventHandler<EnvironmentEventArgs> controller_message_handler = null)
        {
            if (ReferenceEquals(audit_options, null)) throw new ArgumentNullException("audit_options");
            this.AuditOptions = audit_options;
            this.ControllerMessageHandler = controller_message_handler;
            this.HostEnvironmentMessageHandler = AuditTarget_HostEnvironmentMessageHandler;
            this.HostEnvironment = new LocalEnvironment(this.HostEnvironmentMessageHandler);
            this.HostEnvironment.ScriptEnvironment.MessageHandler += this.AuditTarget_ScriptEnvironmentMessageHandler;
            this.HostEnvironmentInitialised = true;
            if (this.AuditOptions.Keys.Contains("RemoteHost"))
            {
                string client;
                if (this.HostEnvironment.OS.Platform == PlatformID.Win32NT)
                {
                    client = this.AuditOptions.Keys.Contains("WindowsUsePlink") ? "plink" : this.AuditOptions.Keys.Contains("WindowsUsePlink") ? "openssh" : "ssh";
                }
                else
                {
                    client = "ssh";
                }
                if (this.AuditOptions.Keys.Contains("RemoteUser") && this.AuditOptions.Keys.Contains("RemotePass"))
                {

                    SshAuditEnvironment ssh_environment = new SshAuditEnvironment(this.HostEnvironmentMessageHandler, client, (string)this.AuditOptions["RemoteHost"],
                        (string)this.AuditOptions["RemoteUser"], this.AuditOptions["RemotePass"], new OperatingSystem(PlatformID.Unix, new Version(0, 0)), this.HostEnvironment);
                    if (ssh_environment.IsConnected)
                    {
                        this.AuditEnvironment = ssh_environment;
                        this.AuditEnvironmentIntialised = true;
                        this.AuditEnvironmentMessageHandler = AuditTarget_AuditEnvironmentMessageHandler;
                        this.AuditEnvironment.MessageHandler -= HostEnvironmentMessageHandler;
                        this.AuditEnvironment.MessageHandler += this.AuditEnvironmentMessageHandler;
                    }
                    else
                    {
                        ssh_environment = null;
                        this.AuditEnvironmentIntialised = false;
                        throw new Exception("Failed to initialise audit environment.");
                    }
                }
            }
            else if (this.AuditOptions.Keys.Contains("RemoteUser") || this.AuditOptions.Keys.Contains("RemotePass"))
            {
                throw new Exception("A remote host name must be specified.");
            }
            else
            {
                this.AuditEnvironmentMessageHandler = AuditTarget_AuditEnvironmentMessageHandler;
                this.AuditEnvironment = new LocalEnvironment(this.AuditEnvironmentMessageHandler);
                this.AuditEnvironmentIntialised = true;
            }
        }

        private void AuditTarget_HostEnvironmentMessageHandler(object sender, EnvironmentEventArgs e)
        {
            e.EnvironmentLocation = "HOST";
            this.ControllerMessageHandler.Invoke(sender, e);
        }

        private void AuditTarget_AuditEnvironmentMessageHandler(object sender, EnvironmentEventArgs e)
        {
            e.EnvironmentLocation = "AUDIT";
            this.ControllerMessageHandler.Invoke(sender, e);
        }

        private void AuditTarget_ScriptEnvironmentMessageHandler(object sender, EnvironmentEventArgs e)
        {
            e.EnvironmentLocation = "SCRIPT";
            this.ControllerMessageHandler.Invoke(sender, e);
        }
        #endregion

        #region Internal properties
        internal Stopwatch Stopwatch { get; set; } = new Stopwatch();
        #endregion

        #region Public methods
        #endregion
    }
}

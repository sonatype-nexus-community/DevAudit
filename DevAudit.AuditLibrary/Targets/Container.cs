
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Threading;

namespace DevAudit.AuditLibrary
{
    public abstract class Container : AuditTarget
    {
        #region Constructors
        public Container(Dictionary<string, object> container_options, EventHandler<EnvironmentEventArgs> message_handler) : base(container_options, message_handler)
        {
            if (this.AuditOptions.ContainsKey("DockerContainer"))
            {
                this.ContainerId = (string)this.AuditOptions["DockerContainer"];
            }
            else throw new ArgumentNullException("The DockerContainer option is not specified.");
            this.Processes = this.AuditEnvironment.GetAllRunningProcesses();
            this.EnvironmentVars = this.AuditEnvironment.GetEnvironmentVars();
        }
        #endregion

        #region Properties
        public string ContainerType { get; protected set; }
        public string ContainerId { get; protected set; }
        public string OSName { get; protected set; }
        public string OSVersion { get; protected set; }
        public List<ProcessInfo> Processes { get; protected set; }
        public Dictionary<string, string> EnvironmentVars { get; protected set; }
        public PackageSource OSPackageSource { get; set; }
        public AuditFileInfo BuildFile { get; protected set; }
        public List<PackageSource> DevPackageSources { get; protected set; }
        public List<Application> Applications { get; protected set; }
        public List<Application> Servers { get; protected set; }
        #endregion

        #region Methods
        public virtual AuditResult Audit(CancellationToken ct)
        {
            return AuditResult.SUCCESS;
        }

        public List<ApplicationServer> DetectRunningServers()
        {
            if (this.Processes != null)
            {
                if (this.Processes.Any(p => p.CommandLine.Contains("postgres")))
                {
                    /*
                    try
                    {
                        PostgreSQLServer pgsql = new PostgreSQLServer(this.AuditOptions, ContainerAuditTarget_EnvironmentMessageHandler);
                    }
                    */
                }
            }
            return null;
        }
        #endregion

        private void ContainerAuditTarget_EnvironmentMessageHandler(object sender, EnvironmentEventArgs e)
        {
            e.EnvironmentLocation = "HOST";
            
        }



    }
}

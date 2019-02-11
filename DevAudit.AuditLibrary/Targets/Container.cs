
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
                this.ContainerType = "docker";
            }
            else throw new ArgumentNullException("The DockerContainer option is not specified.");
            this.AuditOptions.Add("HostEnvironment", this.HostEnvironment);
            this.AuditOptions.Add("AuditEnvironment", this.AuditEnvironment);
            this.Processes = this.AuditEnvironment.GetAllRunningProcesses();
            if (this.Processes == null) this.Processes = new List<ProcessInfo>();
            this.EnvironmentVars = this.AuditEnvironment.GetEnvironmentVars();
        }
        #endregion

        #region Properties
        public string ContainerType { get; protected set; }
        public string ContainerId { get; protected set; }
        public List<ProcessInfo> Processes { get; protected set; } = new List<ProcessInfo>();
        public Dictionary<string, string> EnvironmentVars { get; protected set; } = new Dictionary<string, string>();
        public PackageSource OSPackageSource { get; set; }
        public AuditFileInfo BuildFile { get; protected set; }
        public List<PackageSource> DevPackageSources { get; protected set; }
        public AuditResult OSPackageSourceAuditResult { get; protected set; }
        #endregion

        #region Methods
        public virtual AuditResult Audit(CancellationToken ct)
        {
            this.DetectOSPackageSource();
            List<Task> container_audit_tasks = new List<Task>();
            if (this.OSPackageSource != null)
            {
                container_audit_tasks.Add(Task.Run(() => this.OSPackageSourceAuditResult = this.OSPackageSource.Audit(ct)));
            }
            try
            {
                Task.WaitAll(container_audit_tasks.ToArray(), ct);
            }
            catch (AggregateException ae)
            {
                this.AuditEnvironment.Error(ae, "An error occurred auditing an application server audit target inside the container.");
            }
            return AuditResult.SUCCESS;
        }

        protected PackageSource DetectOSPackageSource()
        {
            string os = this.AuditEnvironment.GetOSName();
            if (os == "debian" || os == "ubuntu")
            {
                this.AuditEnvironment.Success("Detected dpkg package source audit target for container operating system.");
                try
                {
                    this.OSPackageSource = new DpkgPackageSource(this.AuditOptions, null);
                }
                catch (Exception e)
                {
                    this.AuditEnvironment.Error(e, "Error creating dpkg audit target.");
                }
            }
            else if (os == "centos" || os == "oraclelinux" || os == "rhel")
            {
                this.AuditEnvironment.Success("Detected rpm package source audit target for container operating system.");
                try
                {
                    this.OSPackageSource = new RpmPackageSource(this.AuditOptions, null);
                }
                catch (Exception e)
                {
                    this.AuditEnvironment.Error(e, "Error creating rpm audit target.");
                }
            }
            if (this.OSPackageSource == null)
            {
                this.AuditEnvironment.Warning("Could not auto-detect operating system package source.");
            }
            return this.OSPackageSource;
        }
        #endregion
    }
}

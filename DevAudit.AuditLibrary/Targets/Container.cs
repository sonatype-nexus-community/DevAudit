
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
        public List<Application> Applications { get; protected set; }
        public List<ApplicationServer> Servers { get; protected set; } = new List<ApplicationServer>();
        public AuditResult OSPackageSourceAuditResult { get; protected set; }
        public Dictionary<ApplicationServer, AuditResult> ApplicationServerAuditResults { get; protected set; } = new Dictionary<ApplicationServer, AuditResult>();
        #endregion

        #region Methods
        public virtual AuditResult Audit(CancellationToken ct)
        {
            this.DetectOSPackageSource();
            this.DetectRunningServers();
            List<Task> container_audit_tasks = new List<Task>();
            if (this.OSPackageSource != null)
            {
                container_audit_tasks.Add(Task.Run(() => this.OSPackageSourceAuditResult = this.OSPackageSource.Audit(ct)));
            }
            if (this.Servers.Count > 0)
            {
                foreach (ApplicationServer s in this.Servers)
                {
                    Task t = Task.Run(() => this.ApplicationServerAuditResults.Add(s, s.Audit(ct)));
                    container_audit_tasks.Add(t);
                }
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

        protected List<ApplicationServer> DetectRunningServers()
        {
            if (this.Processes.Any(p => p.CommandLine.Contains("sshd")))
            {
                this.AuditEnvironment.Success("Detected OpenSSH sshd server audit target in container.");
                try
                {
                    SSHDServer sshd = new SSHDServer(this.AuditOptions, null);
                    Servers.Add(sshd);
                }
                catch (Exception e)
                {
                    this.AuditEnvironment.Error(e, "Error creating SSHD server audit target.");
                }

            }
            if (this.Processes.Any(p => p.CommandLine.Contains("postgres")))
            {
                this.AuditEnvironment.Success("Detected PostgreSQL server audit target in container.");
                if (!this.AuditOptions.ContainsKey("OSUser"))
                {
                    if (this.EnvironmentVars.ContainsKey("User"))
                    {
                        this.AuditEnvironment.Debug("Setting OSUser option to {0} from User environment variable.", this.EnvironmentVars["User"]);
                        this.AuditOptions.Add("OSUser", this.EnvironmentVars["User"]);
                    }
                    else if (this.EnvironmentVars.ContainsKey("USER"))
                    {
                        this.AuditEnvironment.Debug("Setting OSUser option to {0} from User environment variable.", this.EnvironmentVars["USER"]);
                        this.AuditOptions.Add("OSUser", this.EnvironmentVars["USER"]);
                    }
                }
                try
                {
                    PostgreSQLServer pgsql = new PostgreSQLServer(this.AuditOptions, null);
                    Servers.Add(pgsql);
                }
                catch (Exception e)
                {
                    this.AuditEnvironment.Error(e, "Error creating PostgreSQL server audit target.");
                }
                    
            }
            if (this.Processes.Any(p => p.CommandLine.Contains("mysqld")) || this.EnvironmentVars.Keys.Select(k => k.ToLower()).Any(k => k.Contains("mysql")))
            {
                this.AuditEnvironment.Info("Detected MySQL server audit target.");
                if (!this.AuditOptions.ContainsKey("OSUser"))
                {
                    if (this.EnvironmentVars.ContainsKey("User"))
                    {
                        this.AuditEnvironment.Debug("Setting OSUser option to {0} from User environment variable.", this.EnvironmentVars["User"]);
                        this.AuditOptions.Add("OSUser", this.EnvironmentVars["User"]);
                    }
                    else if (this.EnvironmentVars.ContainsKey("USER"))
                    {
                        this.AuditEnvironment.Debug("Setting OSUser option to {0} from User environment variable.", this.EnvironmentVars["USER"]);
                        this.AuditOptions.Add("OSUser", this.EnvironmentVars["USER"]);
                    }

                    else if (this.EnvironmentVars.ContainsKey("MYSQL_ROOT_PASSWORD"))
                    {
                        this.AuditOptions.Add("AppUser", "root");
                        this.AuditOptions.Add("AppPass", this.EnvironmentVars["MYSQL_ROOT_PASSWORD"]);
                        this.AuditEnvironment.Debug("Setting AppUser option to {0} and AppPass option to {1} from MYSQL_ROOT_PASSWORD environment variable.", "root", this.EnvironmentVars["MYSQL_ROOT_PASSWORD"]);
                    }
                }
                try
                {
                    MySQLServer mysql = new MySQLServer(this.AuditOptions, null);
                    Servers.Add(mysql);
                }
                catch (Exception e)
                {
                    this.AuditEnvironment.Error(e, "Error creating MySQL server audit target.");
                }

            }
            
            if (this.Servers.Count == 0)
            {
                this.AuditEnvironment.Warning("Could not auto-detect container servers.");
            }
            return this.Servers;
        }
        #endregion

 



    }
}

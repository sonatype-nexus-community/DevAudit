using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Threading;

namespace DevAudit.AuditLibrary
{
    public class Container : AuditTarget
    {
        #region Constructors
        public Container(Dictionary<string, object> container_options, EventHandler<EnvironmentEventArgs> message_handler) : base(container_options, message_handler)
        {
            if (this.AuditOptions.ContainsKey("DockerContainer"))
            {
                this.ContainerId = (string)this.AuditOptions["DockerContainer"];
            }
            else throw new ArgumentNullException("The DockerContainer option is not specified.");
        }
        #endregion

        #region Properties
        public string ContainerType { get; protected set; } = "docker";
        public string ContainerId { get; protected set; }
        public string OSName { get; protected set; }
        public string OSVersion { get; protected set; }
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
        #endregion
    }
}

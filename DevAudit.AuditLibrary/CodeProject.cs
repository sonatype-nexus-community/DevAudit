using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevAudit.AuditLibrary
{
    public abstract class CodeProject : AuditTarget
    {
        #region Public abstract properties
        public abstract string CodeProjectId { get; }

        public abstract string CodeProjectLabel { get; }

        public abstract bool IsConfigurationRuleVersionInCodeProjectVersionRange(string configuration_rule_version, string code_project_version);
        #endregion

        #region Public properties
        public Dictionary<string, object> CodeProjectOptions { get; set; } = new Dictionary<string, object>();

        public Dictionary<string, AuditFileSystemInfo> CodeProjectFileSystemMap { get; } = new Dictionary<string, AuditFileSystemInfo>();

        public AuditDirectoryInfo RootDirectory
        {
            get
            {
                return (AuditDirectoryInfo)this.CodeProjectFileSystemMap["RootDirectory"];
            }
        }

        public AuditFileInfo ApplicationBinary { get; protected set; }

        public Dictionary<string, string> RequiredFileLocations { get; protected set; }

        public Dictionary<string, string> RequiredDirectoryLocations { get; protected set; }

        public AuditFileInfo ProjectConfigurationFile { get; protected set; }

        public PackageSource CodeProjectPackageSource { get; protected set; }
        #endregion

        #region Constructors
        public CodeProject(Dictionary<string, object> project_options, EventHandler<EnvironmentEventArgs> message_handler = null) : base(project_options, message_handler)
        {
            this.CodeProjectOptions = project_options;

            if (!this.CodeProjectOptions.ContainsKey("RootDirectory"))
            {
                throw new ArgumentException(string.Format("The root application directory was not specified in the project_options dictionary."), "project_options");
            }
            else if (!this.AuditEnvironment.DirectoryExists((string)this.CodeProjectOptions["RootDirectory"]))
            {
                throw new ArgumentException(string.Format("The root application directory {0} was not found.", this.CodeProjectOptions["RootDirectory"]), "application_options");
            }
            else
            {
                this.CodeProjectFileSystemMap.Add("RootDirectory", this.AuditEnvironment.ConstructDirectory((string)this.CodeProjectOptions["RootDirectory"]));
            }

            if (this.CodeProjectOptions.ContainsKey("File"))
            {
                this.ProjectConfigurationFile = this.AuditEnvironment.ConstructFile((string)this.CodeProjectOptions["File"]);
                if (!this.ProjectConfigurationFile.Exists) throw new ArgumentException("Could not find the file " + (string)this.CodeProjectOptions["File"] + ".", "project_options");
            }
            else
            {
                throw new ArgumentException("File option is required for this audit target", "project_options");
            }
        }
        #endregion

    }
}

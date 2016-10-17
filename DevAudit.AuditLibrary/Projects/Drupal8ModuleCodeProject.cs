using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
namespace DevAudit.AuditLibrary
{
    public class Drupal8ModuleCodeProject : PHPCodeProject
    {
        #region Constructors
        public Drupal8ModuleCodeProject(Dictionary<string, object> project_options, EventHandler<EnvironmentEventArgs> message_handler) : base(project_options, message_handler, "Drupal8Module")
        {
            AuditFileInfo wf = this.AuditEnvironment.ConstructFile(this.CombinePath("@", this.CodeProjectName + ".info" + ".yml"));
            if (!wf.Exists)
            {
                throw new ArgumentException(string.Format("The Drupal 8 module file {0} does not exist.", wf.FullName), "project_options");
            }
            else
            {
                this.WorkspaceFilePath = this.CodeProjectName + ".info" + ".yml";
            }
        }
        #endregion

        #region Overriden methods
        public override async Task<bool> GetWorkspace()
        {
            CallerInformation here = this.AuditEnvironment.Here();
            if (!await base.GetWorkspace())
            {
                return false;
            }
            this.WorkspaceFile = this.HostEnvironment.ConstructFile(this.WorkspaceFilePath) as LocalAuditFileInfo;
            this.HostEnvironment.Status("Loading Drupal 8 module files.");
            DirectoryInfo d = this.WorkspaceDirectory.GetAsSysDirectoryInfo();
            FileInfo wf = d.GetFiles(this.WorkspaceFilePath)?.First();
            if (wf == null)
            {
                this.AuditEnvironment.Error(here, "Could not find local Drupal 8 file {0} in local directory {1}.", wf.Name,
                    d.FullName);
                return false;
            }
            try
            {
                Deserializer yaml_deserializer = new Deserializer(namingConvention: new CamelCaseNamingConvention(), ignoreUnmatched: true);
                this.DrupalModuleInfo = yaml_deserializer.Deserialize<DrupalModuleInfo>(new StringReader(File.ReadAllText(wf.FullName)));
                this.DrupalModuleInfo.ShortName = wf.Name.Split('.')[0];
            }
            catch (Exception e)
            {
                this.HostEnvironment.Error("Exception thrown reading {0} as YAML file.", wf.FullName);
                this.HostEnvironment.Error(e);
            }
            return true;
        }
        #endregion

        #region Public Properties
        public DrupalModuleInfo DrupalModuleInfo { get; protected set; }
        #endregion
    }
}

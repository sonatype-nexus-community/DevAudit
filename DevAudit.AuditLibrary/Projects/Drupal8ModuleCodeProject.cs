using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Alpheus;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

using Versatile;
using System.Threading;

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

        #region Overriden properties
        public override string CodeProjectId { get; } = "drupal";

        public override string CodeProjectLabel { get; } = "Drupal 8 module";
        #endregion

        #region Overriden methods
        protected override async Task GetWorkspaceAsync()
        {
            CallerInformation here = this.AuditEnvironment.Here();
            await base.GetWorkspaceAsync();
            this.WorkspaceFile = this.HostEnvironment.ConstructFile(this.WorkspaceFilePath) as LocalAuditFileInfo;
            this.HostEnvironment.Status("Loading Drupal 8 module files.");
            DirectoryInfo d = this.WorkspaceDirectory.GetAsSysDirectoryInfo();
            FileInfo wf = d.GetFiles(this.WorkspaceFilePath)?.First();
            if (wf == null)
            {
                this.AuditEnvironment.Error(here, "Could not find local Drupal 8 file {0} in local directory {1}.", wf.Name,
                    d.FullName);
                throw new Exception(string.Format("Could not find local Drupal 8 file {0} in local directory {1}.", wf.Name,
                    d.FullName));
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
                throw;
            }
        }

        protected override string GetVersion()
        {
            return this.DrupalModuleInfo.Version;
        }

        protected override Dictionary<string, IEnumerable<OSSIndexQueryObject>> GetModules()
        {
            if (this.Modules == null)
            { 
                this.Modules = new Dictionary<string, IEnumerable<OSSIndexQueryObject>>(1)
                {
                    {"all", new List<OSSIndexQueryObject>(1)
                        { new OSSIndexQueryObject(this.PackageManagerId, this.DrupalModuleInfo.ShortName, 
                            !string.IsNullOrEmpty(this.DrupalModuleInfo.Version) ? this.DrupalModuleInfo.Version : 
                            this.DrupalModuleInfo.Core) }
                    }
                };
            }
            return this.Modules;
        }

        protected override IConfiguration GetConfiguration()
        {
            throw new NotSupportedException();
        }

        public override bool IsConfigurationRuleVersionInServerVersionRange(string configuration_rule_version, string server_version)
        {
            throw new NotSupportedException();
        }

        public override IEnumerable<OSSIndexQueryObject> GetPackages(params string[] o)
        {

            throw new NotSupportedException();
        }

        public override AuditResult Audit(CancellationToken ct)
        {
            CallerInformation caller = this.AuditEnvironment.Here();            
            this.GetWorkspaceTask = this.GetWorkspaceAsync();
            try
            {
                this.GetWorkspaceTask.Wait();
            }
            catch (AggregateException ae)
            {
                this.HostEnvironment.Error(caller, ae, "Exception throw during GetWorkspace task.");
                return AuditResult.ERROR_SCANNING_WORKSPACE;
            }

            this.GetAnalyzersTask = Task.Run(() => this.GetAnalyzers());
            try
            {
                this.GetAnalyzersTask.Wait();
            }
            catch (AggregateException ae)
            {
                this.HostEnvironment.Error(caller, ae, "Exception throw during GetAnalyzers task.");
                return AuditResult.ERROR_SCANNING_ANALYZERS;
            }
            if (this.ListCodeProjectAnalyzers || this.GetAnalyzersTask.Status != TaskStatus.RanToCompletion || this.Analyzers.Count == 0)
            {
                this.GetAnalyzerResultsTask = Task.CompletedTask;
            }
            else
            {
                this.GetAnalyzerResultsTask = Task.Run(() => this.GetAnalyzerResults());
            }
            try
            {
                this.GetAnalyzerResultsTask.Wait();
            }
            catch (AggregateException ae)
            {
                if (ae.TargetSite.Name == "GetAnalyzerResults")
                {
                    this.HostEnvironment.Error(caller, ae, "Exception throw during GetAnalyzerResults task.");
                    return AuditResult.ERROR_RUNNING_ANALYZERS;
                }
                else
                {
                    throw;
                }
            }
            return AuditResult.SUCCESS;
        }

        public override bool IsVulnerabilityVersionInPackageVersionRange(string vulnerability_version, string package_version)
        {
            string message = "";
            bool r = Drupal.RangeIntersect(vulnerability_version, package_version, out message);
            if (!r && !string.IsNullOrEmpty(message))
            {
                throw new Exception(message);
            }
            else return r;
        }
        #endregion

        #region Public properties
        public DrupalModuleInfo DrupalModuleInfo { get; protected set; }
        #endregion
    }
}

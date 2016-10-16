using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Devsense.PHP.Syntax;
namespace DevAudit.AuditLibrary
{
    public class PHPCodeProject : CodeProject
    {
        #region Constructors
        public PHPCodeProject(Dictionary<string, object> project_options, EventHandler<EnvironmentEventArgs> message_handler) : base(project_options, message_handler, "PHP")
        { }
        #endregion

        #region Overriden properties
        public override string CodeProjectId { get { return "php"; } }

        public override string CodeProjectLabel { get { return "PHP"; } }
        #endregion

        #region Overriden methods
        public override Task<bool> GetPackageSource()
        {
            throw new NotImplementedException();
        }

        public override async Task<bool> GetWorkspace()
        {
            if (! await base.GetWorkspace())
            {
                return false;
            }
            this.HostEnvironment.Status("Parsing PHP source files.");
            this.Stopwatch.Start();
            DirectoryInfo wd = this.WorkspaceDirectory.GetAsSysDirectoryInfo();
            List<FileInfo> PHPFiles = wd.GetFiles("*.php", SearchOption.AllDirectories).ToList();
            List<FileInfo> ModuleFiles = wd.GetFiles("*.module", SearchOption.AllDirectories).ToList();
            List<FileInfo> YAMLFiles = wd.GetFiles("*.yml", SearchOption.AllDirectories).ToList();
            Dictionary<FileInfo, PHPAuditSourceUnit> PHPSourceUnits = new Dictionary<FileInfo, PHPAuditSourceUnit>(PHPFiles.Count);
            foreach (FileInfo f in PHPFiles.Concat(ModuleFiles))
            {
                using (StreamReader r = f.OpenText())
                {
                    try
                    {
                        PHPAuditSourceUnit su = new PHPAuditSourceUnit(this.HostEnvironment, await r.ReadToEndAsync(), f);
                        if (su != null)
                        {
                            PHPSourceUnits.Add(f, su);
                            this.HostEnvironment.Info("Parsed {0} class declarations with {1} method declarations from {2}.", su.STV.NamedTypeDeclarationCount, su.STV.MethodDeclarationCount, f.FullName);
                        }
                        else
                        {
                            this.HostEnvironment.Warning("Could not parse PHP file {0}.", f.FullName);
                        }
                    }
                    catch (IOException ioe)
                    {
                        this.HostEnvironment.Error("I/O exception thrown attempting to read PHP file {0}.", f.FullName);
                        this.HostEnvironment.Error(ioe);
                    }
                }
            }
            this.WorkSpace = new Dictionary<string, List<FileInfo>>(3)
            {
                {"PHPFiles", PHPFiles },
                {"YAMLFiles", YAMLFiles },
                {"ModuleFiles", ModuleFiles }
            };
            this.Project = PHPSourceUnits;
            this.Stopwatch.Stop();
            this.HostEnvironment.Success("Parsed {0} PHP files and {1} YAML files. in {2} ms.", PHPSourceUnits.Count(), YAMLFiles.Count, this.Stopwatch.ElapsedMilliseconds);
            return true;
        }
        #endregion
    }
}

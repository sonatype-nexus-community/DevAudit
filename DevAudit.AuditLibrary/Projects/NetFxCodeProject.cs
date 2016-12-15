using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Alpheus;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace DevAudit.AuditLibrary
{
    public class NetFxCodeProject : CodeProject
    {
        #region Constructors
        public NetFxCodeProject(Dictionary<string, object> project_options, EventHandler<EnvironmentEventArgs> message_handler) : base(project_options, message_handler, "Roslyn")
        {
            if (this.CodeProjectOptions.ContainsKey("CodeProjectName"))
            {
                string fn_1 = this.CombinePath(this.RootDirectory.FullName, (string)this.CodeProjectOptions["CodeProjectName"], (string)this.CodeProjectOptions["CodeProjectName"]); //CodeProjectName/CodeProjectName.xxx
                string fn_2 = this.CombinePath(this.RootDirectory.FullName, "src", (string)this.CodeProjectOptions["CodeProjectName"], (string)this.CodeProjectOptions["CodeProjectName"]); //CodeProjectName/src/CodeProjectName.xxx

                AuditFileInfo wf_11 = this.AuditEnvironment.ConstructFile(fn_1 + ".csproj");
                AuditFileInfo wf_12 = this.AuditEnvironment.ConstructFile(fn_1 + ".xproj");
                AuditFileInfo wf_21 = this.AuditEnvironment.ConstructFile(fn_2 + ".csproj");
                AuditFileInfo wf_22 = this.AuditEnvironment.ConstructFile(fn_2 + ".xproj");

                if (wf_11.Exists)
                {
                    this.WorkspaceFilePath = Path.Combine((string)this.CodeProjectOptions["CodeProjectName"], (string)this.CodeProjectOptions["CodeProjectName"]) + ".csproj";
                }
                else if (wf_12.Exists)
                {
                    this.WorkspaceFilePath = Path.Combine((string)this.CodeProjectOptions["CodeProjectName"], (string)this.CodeProjectOptions["CodeProjectName"]) + ".xproj";
                }
                else if (wf_22.Exists)
                {
                    this.WorkspaceFilePath = Path.Combine("src", (string)this.CodeProjectOptions["CodeProjectName"], (string)this.CodeProjectOptions["CodeProjectName"]) + ".xproj";
                }
                else
                {
                    throw new ArgumentException(string.Format("Could not find the project file at {0} or {1} or {2}.", wf_11.FullName, wf_12.FullName, wf_21.FullName), "project_options");
                }
            }

            if (!string.IsNullOrEmpty(this.WorkspaceFilePath))
            {
                if (!(this.WorkspaceFilePath.EndsWith(".csproj") || this.WorkspaceFilePath.EndsWith(".xproj")))
                {
                    throw new ArgumentException("Only .csproj ot .xproj projects are supported for this audit target.", "project_options");
                }
            }
            //Ensure CSharp assembly gets pulled into build.
            var _ = typeof(Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions);

            AuditFileInfo packages_config = this.AuditEnvironment.ConstructFile(this.CombinePath(this.RootDirectory.FullName, this.CodeProjectName, "packages.config"));
            if (packages_config.Exists)
            {
                this.AuditEnvironment.Debug("Found NuGet v2 package manager configuration file {0}", packages_config.FullName);
                this.PackageManagerConfigurationFile = packages_config.FullName;
                nuget_package_source = new NuGetPackageSource(new Dictionary<string, object>(1) { { "File", this.PackageManagerConfigurationFile } }, message_handler);
                PackageSourceInitialized = true;
            }
            else
            {
                this.AuditEnvironment.Debug("NuGet v2 package manager configuration file {0} does not exist.", packages_config.FullName);
                PackageSourceInitialized = false;
            }
        }
        #endregion

        #region Public overriden properties
        public override string CodeProjectId { get { return "netfx"; } }

        public override string CodeProjectLabel { get { return ".NET Framework"; } }
        #endregion

        #region Public overriden methods
        public override IEnumerable<OSSIndexQueryObject> GetPackages(params string[] o)
        {
            if (this.nuget_package_source == null)
            {
                throw new NotSupportedException();
            }
            else
            {
                return this.nuget_package_source.GetPackages(o);
            }
        }

        public override bool IsVulnerabilityVersionInPackageVersionRange(string vulnerability_version, string package_version)
        {
            if (this.nuget_package_source == null)
            {
                throw new NotSupportedException();
            }
            else
            {
                return this.nuget_package_source.IsVulnerabilityVersionInPackageVersionRange(vulnerability_version, package_version);
            }
        }
        #endregion

        #region Protected overriden methods
        protected override async Task GetWorkspaceAsync()
        {
            CallerInformation here = this.AuditEnvironment.Here();
            await base.GetWorkspaceAsync();
            this.HostEnvironment.Status("Compiling workspace projects.");
            DirectoryInfo d = this.WorkspaceDirectory.GetAsSysDirectoryInfo();
            FileInfo wf = d.GetFiles(this.WorkspaceFilePath)?.First();
            if (wf == null)
            {
                this.AuditEnvironment.Error(here, "Could not find local workspace file {0} in local directory {1}.", wf.Name,
                    d.FullName);
                throw new Exception(string.Format("Could not find local workspace file {0} in local directory {1}.", wf.Name,
                    d.FullName));
            }
            if (this.HostEnvironment.IsMonoRuntime)
            {
                this.HostEnvironment.Error("Using the MSBuild workspace is not yet supported on Mono. See " + @"https://gitter.im/dotnet/roslyn/archives/2016/09/25");
                throw new Exception("Using the MSBuild workspace is not yet supported on Mono. See " + @"https://gitter.im/dotnet/roslyn/archives/2016/09/25");
            }
            this.HostEnvironment.Status("Compiling project file {0}.", wf.FullName);
            this.Stopwatch.Start();
            try
            {
                this.MSBuildWorkspace = MSBuildWorkspace.Create();
                Project p = this.MSBuildWorkspace.OpenProjectAsync(wf.FullName).Result;
                Compilation c = await p.GetCompilationAsync();
                this.Project = p;
                this.Compilation = c;
                this.WorkSpace = this.MSBuildWorkspace;
                this.Stopwatch.Stop();
                this.HostEnvironment.Success("Roslyn compiled {2} file(s) in project {0} in {1} ms.", p.Name, this.Stopwatch.ElapsedMilliseconds, c.SyntaxTrees.Count());
            }
            catch (Exception e)
            {
                this.HostEnvironment.Error(here, e);
                this.MSBuildWorkspace.Dispose();
                throw;
            }
            foreach (SyntaxTree st in (this.Compilation as Compilation).SyntaxTrees)
            {
                this.HostEnvironment.Debug("Compiled {0}", st.FilePath);
            }
            return;
        }
       
        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            // TODO If you need thread safety, use a lock around these 
            // operations, as well as in your methods that use the resource. 
            try
            {
                if (!this.IsDisposed)
                {
                    // Explicitly set root references to null to expressly tell the GarbageCollector 
                    // that the resources have been disposed of and its ok to release the memory 
                    // allocated for them.
                    if (isDisposing)
                    {
                        // Release all managed resources here 
                        // Need to unregister/detach yourself from the events. Always make sure 
                        // the object is not null first before trying to unregister/detach them! 
                        // Failure to unregister can be a BIG source of memory leaks 
                        //if (someDisposableObjectWithAnEventHandler != null)
                        //{ someDisposableObjectWithAnEventHandler.SomeEvent -= someDelegate; 
                        //someDisposableObjectWithAnEventHandler.Dispose(); 
                        //someDisposableObjectWithAnEventHandler = null; } 
                        // If this is a WinForm/UI control, uncomment this code 
                        //if (components != null) //{ // components.Dispose(); //} } 
                        // Release all unmanaged resources here 
                        // (example) if (someComObject != null && Marshal.IsComObject(someComObject)) { Marshal.FinalReleaseComObject(someComObject); someComObject = null; 
                        if (this.MSBuildWorkspace != null)
                        {
                            this.MSBuildWorkspace.Dispose();
                            this.MSBuildWorkspace = null;
                        }
                    }
                }
            }
            finally
            {
                this.IsDisposed = true;
            }

        }
        #endregion

        #region Public properties
        public MSBuildWorkspace MSBuildWorkspace { get; protected set; }
        #endregion

        #region Private fields
        bool IsDisposed = false;
        NuGetPackageSource nuget_package_source = null;
        #endregion
    }

}

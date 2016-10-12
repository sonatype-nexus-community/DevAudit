using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;
//using Microsoft.b

namespace DevAudit.AuditLibrary
{
    
    public class NetFxProject : CodeProject
    {
        #region Overriden properties
        public override string CodeProjectId { get { return "netfx"; } }

        public override string CodeProjectLabel { get { return ".NET Framework"; } }
        #endregion

        #region Overriden methods
        public override async Task<bool> GetWorkspace()
        {
            CallerInformation here = this.AuditEnvironment.Here();
            if (this.CodeProjectOptions.ContainsKey("CodeProjectName"))
            {

                string fn_1 = this.CombinePath(this.RootDirectory.FullName, (string)this.CodeProjectOptions["CodeProjectName"], (string)this.CodeProjectOptions["CodeProjectName"]); //CodeProjectName/CodeProjectName.xxx
                AuditFileInfo wf_11 = this.AuditEnvironment.ConstructFile(fn_1 + ".csproj");
                AuditFileInfo wf_12 = this.AuditEnvironment.ConstructFile(fn_1 + ".xproj");
                if (wf_11.Exists)
                {
                    this.WorkspaceFilePath = Path.Combine((string)this.CodeProjectOptions["CodeProjectName"], (string)this.CodeProjectOptions["CodeProjectName"]) + ".csproj";
                }
                else if (wf_12.Exists)
                {
                    this.WorkspaceFilePath = Path.Combine((string)this.CodeProjectOptions["CodeProjectName"], (string)this.CodeProjectOptions["CodeProjectName"]) + ".xproj";
                }
                else
                {
                    this.AuditEnvironment.Error(here, "Could not find the project file at {0} or {1}.", wf_11.FullName, wf_12.FullName);
                    return false;
                }

            }
         
            if (! (this.AuditEnvironment is LocalEnvironment))
            {
                this.HostEnvironment.Status("Downloading {0} as local directory for code analysis.", this.RootDirectory.FullName);
            }
            this.WorkspaceDirectory = await Task.Run(() => this.RootDirectory.GetAsLocalDirectory());
            if (this.WorkspaceDirectory == null && !(this.AuditEnvironment is LocalEnvironment))
            {
                this.HostEnvironment.Error(here, "Could not download {0} as local directory.", this.WorkspaceDirectory);
                return false;
            }
            else if (this.WorkspaceDirectory == null)
            {
                this.HostEnvironment.Error(here, "Could not get {0} as local directory.", this.WorkspaceDirectory);
                return false;
            }
            else if (this.WorkspaceDirectory != null && !(this.AuditEnvironment is LocalEnvironment))
            {
                this.HostEnvironment.Success("Using {0} as workspace directory for code analysis.", this.WorkspaceDirectory.FullName);
            }
            else if (this.AuditEnvironment is LocalEnvironment)
            {
                this.HostEnvironment.Status("Using local directory {0} for code analysis.", this.WorkspaceDirectory.FullName);
            }
            DirectoryInfo d = this.WorkspaceDirectory.GetAsSysDirectoryInfo();
            FileInfo wf = d.GetFiles(this.WorkspaceFilePath)?.First();
            if (wf == null)
            {
                this.AuditEnvironment.Error(here, "Could not find local workspace file {0} in local directory {1}.", wf.Name,
                    d.FullName);
                return false;
            }
            if (this.HostEnvironment.IsMonoRuntime)
            {
                this.HostEnvironment.Warning("Using the MSBuild workspace is not yet supported on Mono. See " + @"https://gitter.im/dotnet/roslyn/archives/2016/09/25");
                return false;
            }
            this.HostEnvironment.Status("Compiling project file {0}.", wf.FullName);
            this.MSBuildWorkspace = MSBuildWorkspace.Create(); ;
            this.Stopwatch.Start();
            try
            {
                Project p = this.MSBuildWorkspace.OpenProjectAsync(wf.FullName).Result;
                this.Compilation = await p.GetCompilationAsync();
                this.Stopwatch.Stop();
                this.WorkSpace = this.Compilation;
                this.HostEnvironment.Success("Roslyn compiled {2} files in project {0} in {1}.", p.Name, this.Stopwatch.ElapsedMilliseconds, this.Compilation.SyntaxTrees.Count());
                return true;
            }
            catch (Exception e)
            {
                this.HostEnvironment.Error(here, e);
                this.MSBuildWorkspace.Dispose();
                d.Delete();
                return false;
            }
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
                    this.Compilation = null;
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
        public Compilation Compilation { get; protected set; }
        #endregion

        #region Constructors
        public NetFxProject(Dictionary<string, object> project_options, EventHandler<EnvironmentEventArgs> message_handler) : base(project_options, message_handler)
        {
            //Ensure CSharp assembly gets pulled into build.
            var _ = typeof(Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions); 
            if (!string.IsNullOrEmpty(this.WorkspaceFilePath))
            {
                if (!(this.WorkspaceFilePath.EndsWith(".csproj") || this.WorkspaceFilePath.EndsWith(".xproj")))
                {
                    throw new ArgumentException("Only .csproj ot .xproj projects are supported for this audit target.");
                }
            }
        }
        #endregion

        #region Private fields
        bool IsDisposed = false;
        #endregion
    }

}

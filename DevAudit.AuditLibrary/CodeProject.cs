using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevAudit.AuditLibrary
{
    public abstract class CodeProject : AuditTarget, IDisposable
    {
        #region Public enums
        public enum AuditResult
        {
            SUCCESS = 0,
            ERROR_SCANNING_WORKSPACE,
        }
        #endregion

        #region Public abstract properties
        public abstract string CodeProjectId { get; }

        public abstract string CodeProjectLabel { get; }
        #endregion

        #region Public abstract methods
        public abstract Task<bool> GetWorkspace();
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

        public string WorkspaceFilePath { get; protected set; }

        public LocalAuditFileInfo WorkspaceFile { get; protected set; }

        public LocalAuditDirectoryInfo WorkspaceDirectory { get; protected set; }

        public object WorkSpace { get; protected set; }

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
                string fn = (string) this.CodeProjectOptions["File"];
                if (fn.StartsWith("@"))
                {
                    this.WorkspaceFilePath = fn.Substring(1);
                }
                else
                {
                    throw new ArgumentException("The workspace file parameter must be relative to the root directory for this audit target.");
                }
            }
        }
        #endregion

        #region Public virtual methods
        public async Task<AuditResult> Audit()
        {
            if(!await this.GetWorkspace())
            {
                return AuditResult.ERROR_SCANNING_WORKSPACE;
            }
            return AuditResult.SUCCESS;  
        }
        #endregion

        #region Protected methods
        protected string CombinePath(params string[] paths)
        {
            if (paths == null || paths.Count() == 0)
            {
                throw new ArgumentOutOfRangeException("paths", "paths must be non-null or at least length 1.");
            }
            if (this.AuditEnvironment.OS.Platform == PlatformID.Unix || this.AuditEnvironment.OS.Platform == PlatformID.MacOSX)
            {
                List<string> paths_list = new List<string>(paths.Length + 1);
                if (paths.First() == "@")
                {
                    paths[0] = this.RootDirectory.FullName == "/" ? "" : this.RootDirectory.FullName;
                }
                paths_list.AddRange(paths);
                return paths_list.Aggregate((p, n) => p + "/" + n);
            }
            else
            {
                if (paths.First() == "@")
                {
                    paths[0] = this.RootDirectory.FullName;
                    return Path.Combine(paths);
                }
                else
                {
                    return Path.Combine(paths);
                }
            }
        }

        protected string LocatePathUnderRoot(params string[] paths)
        {
            if (this.AuditEnvironment.OS.Platform == PlatformID.Unix || this.AuditEnvironment.OS.Platform == PlatformID.MacOSX)
            {
                return "@" + paths.Aggregate((p, n) => p + "/" + n);
            }
            else
            {

                return "@" + Path.Combine(paths);
            }

        }
        #endregion

        #region Disposer
        private bool IsDisposed { get; set; }
        /// <summary> 
        /// /// Implementation of Dispose according to .NET Framework Design Guidelines. 
        /// /// </summary> 
        /// /// <remarks>Do not make this method virtual. 
        /// /// A derived class should not be able to override this method. 
        /// /// </remarks>         
        public void Dispose()
        {
            Dispose(true); // This object will be cleaned up by the Dispose method. // Therefore, you should call GC.SupressFinalize to // take this object off the finalization queue // and prevent finalization code for this object // from executing a second time. // Always use SuppressFinalize() in case a subclass // of this type implements a finalizer. GC.SuppressFinalize(this); }
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing)
        {
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
                    }
                }
            }
            finally
            {
                this.IsDisposed = true;
            }
        }
        #endregion
    }
}

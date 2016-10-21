using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using CSScriptLibrary;
using csscript;

namespace DevAudit.AuditLibrary
{
    public abstract class CodeProject : AuditTarget, IDisposable
    {
        #region Public enums
        public enum AuditResult
        {
            SUCCESS = 0,
            ERROR_SCANNING_WORKSPACE,
            ERROR_SCANNING_ANALYZERS,
            ERROR_ANALYZING
        }
        #endregion

        #region Constructors
        public CodeProject(Dictionary<string, object> project_options, EventHandler<EnvironmentEventArgs> message_handler, string analyzer_type) : base(project_options, message_handler)
        {
            CallerInformation here = this.AuditEnvironment.Here();
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

            if (this.CodeProjectOptions.ContainsKey("CodeProjectName"))
            {
                this.CodeProjectName = (string)CodeProjectOptions["CodeProjectName"];
            }

            if (this.CodeProjectOptions.ContainsKey("File"))
            {
                string fn = (string)this.CodeProjectOptions["File"];
                if (!fn.StartsWith("@"))
                {
                    throw new ArgumentException("The workspace file parameter must be relative to the root directory for this audit target.");
                }
                AuditFileInfo wf = this.AuditEnvironment.ConstructFile(this.CombinePath("@", fn.Substring(1)));
                if (wf.Exists)
                {
                    this.WorkspaceFilePath = wf.FullName;
                }
                else
                {
                    throw new ArgumentException(string.Format("The workspace file {0} was not found.", wf.FullName));

                }
            }
            this.AnalyzerType = analyzer_type;
        }
        #endregion

        #region Public abstract properties
        public abstract string CodeProjectId { get; }

        public abstract string CodeProjectLabel { get; }
        #endregion

        #region Public properties
        public Dictionary<string, object> CodeProjectOptions { get; set; } = new Dictionary<string, object>();

        public Dictionary<string, AuditFileSystemInfo> CodeProjectFileSystemMap { get; protected set; } = new Dictionary<string, AuditFileSystemInfo>();

        public AuditDirectoryInfo RootDirectory
        {
            get
            {
                return (AuditDirectoryInfo)this.CodeProjectFileSystemMap["RootDirectory"];
            }
        }

        public string CodeProjectName { get; protected set; }

        public string WorkspaceFilePath { get; protected set; }

        public LocalAuditFileInfo WorkspaceFile { get; protected set; }

        public LocalAuditDirectoryInfo WorkspaceDirectory { get; protected set; }

        public object WorkSpace { get; protected set; }

        public object Project { get; protected set; }

        public object Compilation { get; protected set; }

        public string AnalyzerType { get; protected set; }

        public List<FileInfo> AnalyzerScripts { get; protected set; } = new List<FileInfo>();

        public List<Analyzer> Analyzers { get; protected set; } = new List<Analyzer>();

        public List<AnalyzerResult> AnalyzerResults { get; protected set; } = new List<AnalyzerResult>();

        public PackageSource PackageSource { get; protected set; }
        #endregion

        #region Public methods
        public virtual async Task<bool> GetWorkspaceAsync()
        {
            CallerInformation here = this.AuditEnvironment.Here();
            if (!(this.AuditEnvironment is LocalEnvironment))
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
                return true;
            }
            else // (this.AuditEnvironment is LocalEnvironment)
            {
                this.HostEnvironment.Info("Using local directory {0} for code analysis.", this.WorkspaceDirectory.FullName);
                return true;
            }
        }

        public Task<AuditResult> Audit()
        {
            CancellationToken ct = new CancellationToken();
            Task<bool> get_package_source_audit = Task.FromResult(false);//this.PackageSource != null ? Task.Run(() => this.PackageSource.Audit()) : Task.FromResult(false);
            Task<bool> get_workspace = this.GetWorkspaceAsync();
            Task<bool> get_analyzers;
            Task<List<AnalyzerResult>> get_analyzer_results = null;
            get_workspace.Wait();
            if (get_workspace.Status == TaskStatus.RanToCompletion && get_workspace.Result == true)
            {
                get_analyzers = this.GetAnalyzers();
            }
            else
            {
                return Task.FromResult(AuditResult.ERROR_SCANNING_WORKSPACE);
            }
            get_analyzers.Wait();
            if (get_analyzers.Status == TaskStatus.RanToCompletion && get_analyzers.Result == true)
            {
                get_analyzer_results = this.GetAnalyzerResults();
            }
            else
            {
                return Task.FromResult(AuditResult.ERROR_SCANNING_ANALYZERS);
            }           
            Task.WaitAll(get_analyzer_results, get_package_source_audit);
            if (get_analyzer_results.Status == TaskStatus.RanToCompletion && get_analyzer_results.Result != null)
            {
                this.AnalyzerResults = get_analyzer_results.Result;
                if (this.AnalyzerResults.Count(ar => ar.Succeded) > 0)
                {
                    this.HostEnvironment.Success("Code analysis by {0} analyzers succeded.", this.AnalyzerResults.Count(ar => ar.Succeded));
                }
                if (get_analyzer_results.Result.Count(ar => !ar.Succeded) > 0)
                {
                    this.HostEnvironment.Warning("Code analysis by {0} analyzers did not succed.", this.AnalyzerResults.Count(ar => !ar.Succeded));
                }
                return Task.FromResult(AuditResult.SUCCESS);
            }
            else
            {
                return Task.FromResult(AuditResult.ERROR_ANALYZING);
            }
        }
     
        public async Task<bool> GetAnalyzers()
        {
            this.Stopwatch.Restart();
            // Just in case clear AlternativeCompiler so it is not set to Roslyn or anything else by 
            // the CS-Script installed (if any) on the host OS
            CSScript.GlobalSettings.UseAlternativeCompiler = null;
            CSScript.EvaluatorConfig.Engine = EvaluatorEngine.Mono;
            CSScript.CacheEnabled = false; //Script caching is broken on Mono: https://github.com/oleg-shilo/cs-script/issues/10
            CSScript.KeepCompilingHistory = true;
            DirectoryInfo analyzers_dir = new DirectoryInfo(Path.Combine("Analyzers", this.AnalyzerType));
            this.AnalyzerScripts = analyzers_dir.GetFiles("*.cs", SearchOption.AllDirectories).ToList();
            foreach (FileInfo f in this.AnalyzerScripts)
            {
                string script = string.Empty;
                using (FileStream fs = f.OpenRead())
                using (StreamReader r = new StreamReader(fs))
                {
                    script = await r.ReadToEndAsync();
                }
                try
                {
                    Analyzer a = (Analyzer)await CSScript.Evaluator.LoadCodeAsync(script, this.HostEnvironment.ScriptEnvironment,
                        this.WorkSpace, this.Project, this.Compilation);
                    this.Analyzers.Add(a);
                    this.HostEnvironment.Info("Loaded {0} analyzer from {1}.", a.Name, f.FullName);
                }
                catch (CompilerException ce)
                { 
                    HostEnvironment.Error("Compiler error(s) compiling analyzer {0}.", f.FullName);
                    IDictionaryEnumerator en = ce.Data.GetEnumerator();
                    while (en.MoveNext())
                    {
                        List<string> v = (List<string>)en.Value;
                        if (v.Count > 0)
                        {
                            if ((string)en.Key == "Errors")
                            {
                                HostEnvironment.ScriptEnvironment.Error(v.Aggregate((s1, s2) => s1 + Environment.NewLine + s2));
                            }
                            else if((string) en.Key == "Warnings")
                            {
                                HostEnvironment.ScriptEnvironment.Warning(v.Aggregate((s1, s2) => s1 + Environment.NewLine + s2));
                            }
                            else
                            {
                                HostEnvironment.ScriptEnvironment.Error("{0} : {1}", en.Key, v.Aggregate((s1, s2) => s1 + Environment.NewLine + s2));
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    HostEnvironment.Error("Unknown error compiling analyzer {0}.", f.FullName);
                    HostEnvironment.Error(e);
                }
            }
            this.Stopwatch.Stop();
            if (this.AnalyzerScripts.Count == 0)
            {
                this.HostEnvironment.Info("No {0} analyzers found in {1}.", this.AnalyzerType, analyzers_dir.FullName);
                return false;
            }
            else if (this.AnalyzerScripts.Count > 0 && this.Analyzers.Count > 0)
            {
                if (this.Analyzers.Count < this.AnalyzerScripts.Count)
                {
                    this.HostEnvironment.Warning("Failed to load {0} of {1} analyzer(s).", this.AnalyzerScripts.Count - this.Analyzers.Count, this.AnalyzerScripts.Count);
                }
                this.HostEnvironment.Success("Loaded {0} out of {1} analyzer(s) in {2} ms.", this.Analyzers.Count, this.AnalyzerScripts.Count, this.Stopwatch.ElapsedMilliseconds);
                return true;
            }
            else
            {
                this.HostEnvironment.Error("Failed to load {0} analyzer(s).", this.AnalyzerScripts.Count);
                return false;
            }
        }

        public async Task<List<AnalyzerResult>> GetAnalyzerResults()
        {
            List<AnalyzerResult> results = new List<AnalyzerResult>(this.Analyzers.Count);
            foreach (Analyzer a in this.Analyzers)
            {
                this.HostEnvironment.Status("{0} analyzing.", a.Name);
                AnalyzerResult ar = new AnalyzerResult() { Analyzer = a };
                try
                {
                    ar = await a.Analyze();
                }
                catch(AggregateException ae)
                {
                    ar.Exceptions = ae.InnerExceptions.ToList();
                    ar.Succeded = false;
                }
                finally
                {
                    results.Add(ar);
                }
                
            }
            return results;
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
                    this.CodeProjectOptions = null;
                    this.CodeProjectFileSystemMap = null;
                    this.WorkspaceFile = null;
                    this.WorkspaceDirectory = null;
                    this.WorkSpace = null;
                    this.Project = null;
                    this.Compilation = null;
                    this.AnalyzerScripts = null;
                    this.Analyzers = null;
                    this.AnalyzerResults = null;
                    this.PackageSource = null;
                    this.Stopwatch = null;
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

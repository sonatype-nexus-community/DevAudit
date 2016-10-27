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
    public abstract class CodeProject : Application
    {
        #region Constructors
        public CodeProject(Dictionary<string, object> project_options, EventHandler<EnvironmentEventArgs> message_handler, string analyzer_type) : base(project_options, new Dictionary<string, string[]>(), new Dictionary<string, string[]>(), message_handler)
        {
            CallerInformation here = this.AuditEnvironment.Here();
            this.CodeProjectOptions = project_options;
           
            if (this.CodeProjectOptions.ContainsKey("CodeProjectName"))
            {
                this.CodeProjectName = (string)CodeProjectOptions["CodeProjectName"];
            }

            if (this.CodeProjectOptions.ContainsKey("File"))
            {
                string fn = (string)this.CodeProjectOptions["File"];
                if (!fn.StartsWith("@"))
                {
                    throw new ArgumentException("The workspace file parameter must be relative to the root directory for this audit target.", "project_options");
                }
                AuditFileInfo wf = this.AuditEnvironment.ConstructFile(this.CombinePath("@", fn.Substring(1)));
                if (wf.Exists)
                {
                    this.WorkspaceFilePath = wf.FullName;
                }
                else
                {
                    throw new ArgumentException(string.Format("The workspace file {0} was not found.", wf.FullName), "project_options");

                }
            }
            this.AnalyzerType = analyzer_type;
            if (this.CodeProjectOptions.ContainsKey("ListCodeProjectAnalyzers"))
            {
                this.ListCodeProjectAnalyzers = true;
            }
        }
        #endregion

        #region Public abstract properties
        public abstract string CodeProjectId { get; }

        public abstract string CodeProjectLabel { get; }
        #endregion

        #region Public overriden properties
        public override string ApplicationId => this.CodeProjectId;

        public override string ApplicationLabel => this.CodeProjectLabel;

        public override string PackageManagerId => this.PackageSource?.PackageManagerId;

        public override OSSIndexHttpClient HttpClient { get; } = new OSSIndexHttpClient("1.1");

        public override string PackageManagerLabel => this.PackageSource?.PackageManagerLabel;

        public override Func<List<OSSIndexArtifact>, List<OSSIndexArtifact>> ArtifactsTransform => this.PackageSource?.ArtifactsTransform;
        #endregion

        #region Protected overriden methods
        public override IEnumerable<OSSIndexQueryObject> GetPackages(params string[] o) => this.PackageSource?.GetPackages(o);

        public override bool IsVulnerabilityVersionInPackageVersionRange(string vulnerability_version, string package_version) => this.PackageSource?.IsVulnerabilityVersionInPackageVersionRange(vulnerability_version, package_version) ?? false;

        protected override Dictionary<string, IEnumerable<OSSIndexQueryObject>> GetModules()
        {
            if (this.PackageSource != null)
            {
                return new Dictionary<string, IEnumerable<OSSIndexQueryObject>>()
                {
                    ["all"] = this.PackageSource.Packages
                };
            }
            else return null;
        }

        protected override void Dispose(bool isDisposing)
        {
            // TODO If you need thread safety, use a lock around these 
            // operations, as well as in your methods that use the resource. 
            try
            {
                base.Dispose();
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

        #region Public properties
        public Dictionary<string, object> CodeProjectOptions { get; set; } = new Dictionary<string, object>();

        public PackageSource PackageSource { get; protected set; }
        public Dictionary<string, AuditFileSystemInfo> CodeProjectFileSystemMap { get; protected set; } = new Dictionary<string, AuditFileSystemInfo>();

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

        public List<AnalyzerResult> AnalyzerResults { get; protected set; }

        public bool ListCodeProjectAnalyzers { get; protected set; }

        public Task GetWorkspaceTask { get; protected set; }

        public Task GetAnalyzersTask { get; protected set; }

        public Task GetAnalyzerResultsTask { get; protected set; }
        #endregion

        #region Public methods
        public override AuditResult Audit(CancellationToken ct)
        {
            CallerInformation caller = this.AuditEnvironment.Here();
            Task audit_application = Task.Run(() => base.Audit(ct), ct);
            this.GetWorkspaceTask = this.GetWorkspaceAsync();
            try
            {
                this.GetWorkspaceTask.Wait();
            }
            catch (AggregateException ae)
            {
                this.HostEnvironment.Error(caller, ae, "Exception throw during GetWorkspace task.");
            }

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
                this.GetAnalyzerResultsTask = Task.Run(() => this.GetAnalyzers());
            }
            try
            {
                Task.WaitAll(audit_application, this.GetAnalyzerResultsTask);
            }
            catch (AggregateException ae)
            {
                if (ae.TargetSite.Name == "GetAnalyzers")
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
        #endregion

        #region Protected methods
        protected virtual async Task GetWorkspaceAsync()
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
                throw new Exception(string.Format("Could not download {0} as local directory.", this.WorkspaceDirectory));
            }
            else if (this.WorkspaceDirectory == null)
            {
                this.HostEnvironment.Error(here, "Could not get {0} as local directory.", this.WorkspaceDirectory);
                throw new Exception(string.Format("Could not get {0} as local directory.", this.WorkspaceDirectory));
            }
            else if (this.WorkspaceDirectory != null && !(this.AuditEnvironment is LocalEnvironment))
            {
                this.HostEnvironment.Success("Using {0} as workspace directory for code analysis.", this.WorkspaceDirectory.FullName);
                return;
            }
            else // (this.AuditEnvironment is LocalEnvironment)
            {
                this.HostEnvironment.Info("Using local directory {0} for code analysis.", this.WorkspaceDirectory.FullName);
                return;
            }
        }

        protected async Task GetAnalyzers()
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
                    throw ce;
                }
            }
            this.Stopwatch.Stop();
            if (this.AnalyzerScripts.Count == 0)
            {
                this.HostEnvironment.Info("No {0} analyzers found in {1}.", this.AnalyzerType, analyzers_dir.FullName);
                return;
            }
            else if (this.AnalyzerScripts.Count > 0 && this.Analyzers.Count > 0)
            {
                if (this.Analyzers.Count < this.AnalyzerScripts.Count)
                {
                    this.HostEnvironment.Warning("Failed to load {0} of {1} analyzer(s).", this.AnalyzerScripts.Count - this.Analyzers.Count, this.AnalyzerScripts.Count);
                }
                this.HostEnvironment.Success("Loaded {0} out of {1} analyzer(s) in {2} ms.", this.Analyzers.Count, this.AnalyzerScripts.Count, this.Stopwatch.ElapsedMilliseconds);
                return;
            }
            else
            {
                this.HostEnvironment.Error("Failed to load {0} analyzer(s).", this.AnalyzerScripts.Count);
                return;
            }
        }

        protected async Task GetAnalyzerResults()
        {
            this.AnalyzerResults = new List<AnalyzerResult>(this.Analyzers.Count);
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
                    this.AnalyzerResults.Add(ar);
                }
                
            }
            return;
        }
        #endregion

        #region Private fields
        private bool IsDisposed { get; set; }
        #endregion

    }
}

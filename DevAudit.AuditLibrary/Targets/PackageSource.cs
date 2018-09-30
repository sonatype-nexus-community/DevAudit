using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Sprache;
using Alpheus.IO;

namespace DevAudit.AuditLibrary
{
    public abstract class PackageSource : AuditTarget
    {
        #region Constructors
        public PackageSource(Dictionary<string, object> package_source_options, EventHandler<EnvironmentEventArgs> message_handler) : base(package_source_options, message_handler)
        {
            this.PackageSourceOptions = this.AuditOptions;
            if (this.PackageSourceOptions.ContainsKey("File"))
            {
                this.PackageManagerConfigurationFile = (string)this.PackageSourceOptions["File"];
                if (!this.AuditEnvironment.FileExists(this.PackageManagerConfigurationFile))
                {
                    throw new ArgumentException("Could not find the file " + this.PackageManagerConfigurationFile + ".", "package_source_options");
                }
            }
            else if (!this.PackageSourceOptions.ContainsKey("File") && this.DefaultPackageManagerConfigurationFile != string.Empty)
            {
                if (this.AuditEnvironment.FileExists(this.DefaultPackageManagerConfigurationFile))
                {
                    this.AuditEnvironment.Info("Using default {0} package manager configuration file {1}", this.PackageManagerLabel, this.DefaultPackageManagerConfigurationFile);
                    this.PackageManagerConfigurationFile = this.DefaultPackageManagerConfigurationFile;
                }
                else
                {
                    throw new ArgumentException(string.Format("No file option was specified and the default {0} package manager configuration file {1} was not found.", this.PackageManagerLabel, this.DefaultPackageManagerConfigurationFile));
                }
            }

            if (!string.IsNullOrEmpty(this.PackageManagerConfigurationFile))
            {
                AuditFileInfo cf = this.AuditEnvironment.ConstructFile(this.PackageManagerConfigurationFile);
                AuditDirectoryInfo d = this.AuditEnvironment.ConstructDirectory(cf.DirectoryName);
                IFileInfo[] pf;
                if ((pf = d.GetFiles("devaudit.yml")) != null)
                this.AuditProfile = new AuditProfile(this.AuditEnvironment, this.AuditEnvironment.ConstructFile(pf.First().FullName));
            }

            if (this.PackageSourceOptions.ContainsKey("ListPackages"))
            {
                this.ListPackages = true;
            }

            if (this.PackageSourceOptions.ContainsKey("ListArtifacts"))
            {
                this.ListArtifacts = true;
            }

            if (this.PackageSourceOptions.ContainsKey("SkipPackagesAudit"))
            {
                this.SkipPackagesAudit = true;
            }

            if (this.PackageSourceOptions.ContainsKey("WithPackageInfo"))
            {
                this.WithPackageInfo = true;
            }

            if (this.PackageSourceOptions.ContainsKey("HttpsProxy"))
            {
                if (this.AuditOptions.ContainsKey("HttpsProxy"))
                {
                    DataSourceOptions.Add("HttpsProxy", (Uri)this.PackageSourceOptions["HttpsProxy"]);
                }
            }

            string[] ossi_pms = { "bower", "composer", "choco", "msi", "nuget", "oneget", "yarn" };
            if (this.DataSources.Count == 0 && ossi_pms.Contains(this.PackageManagerId))
            {
                this.HostEnvironment.Info("Using OSS Index as default package vulnerabilities data source for {0} package source.", this.PackageManagerLabel);
                // this.DataSources.Add(new OSSIndexDataSource(this, this.DataSourceOptions));
                this.DataSources.Add(new OSSIndexApiv3DataSource(this, DataSourceOptions));
            }
        }
        #endregion

        #region Abstract properties
        public abstract string PackageManagerId { get; }
        public abstract string PackageManagerLabel { get; }
        public abstract string DefaultPackageManagerConfigurationFile { get; }
        #endregion

        #region Abstract methods
        public abstract IEnumerable<Package> GetPackages(params string[] o);
        public abstract bool IsVulnerabilityVersionInPackageVersionRange(string vulnerability_version, string package_version);
        #endregion

        #region Properties
        public Dictionary<string, object> PackageSourceOptions { get; set; } = new Dictionary<string, object>();

        public bool ListPackages { get; protected set; } = false;

        public bool WithPackageInfo { get; protected set; } = false;

        public bool ListArtifacts { get; protected set; } = false;

        public bool SkipPackagesAudit { get; protected set; } = false;

        public string PackageManagerConfigurationFile { get; set; }

        public IEnumerable<Package> Packages { get; protected set; }

        public List<VulnerableCredentialStorage> CredentialStorageCandidates { get; protected set; }

        public Dictionary<IPackage, List<IArtifact>> Artifacts { get; } = new Dictionary<IPackage, List<IArtifact>>();

        public Dictionary<IPackage, List<IVulnerability>> Vulnerabilities { get; } = new Dictionary<IPackage, List<IVulnerability>>();
         
        public ConcurrentDictionary<IPackage, Exception> GetVulnerabilitiesExceptions { get; protected set; }

        public Task PackagesTask { get; protected set; }

        public Task VulnerableCredentialStorageTask { get; protected set; }

        public Task ArtifactsTask { get; protected set; }

        public Task VulnerabilitiesTask { get; protected set; }

        public Task EvaluateVulnerabilitiesTask { get; protected set; }

        public Task ReportAuditTask { get; protected set; }
        #endregion

        #region Methods
        public virtual AuditResult Audit(CancellationToken ct)
        {
            CallerInformation here = this.AuditEnvironment.Here();
            this.GetPackagesTask(ct);
            this.GetVulnerableCredentialStorageTask(ct);
            try
            {
                Task.WaitAll(this.PackagesTask, this.VulnerableCredentialStorageTask);
                if (!this.SkipPackagesAudit) AuditEnvironment.Success("Scanned {0} {1} packages.", this.Packages.Count(), this.PackageManagerLabel);
            }
            catch (AggregateException ae)
            {
                this.AuditEnvironment.Error(here, ae, "Error in {0} method in GetPackages task.", ae.InnerException.TargetSite.Name);
                return AuditResult.ERROR_SCANNING_PACKAGES;
            }

            this.Packages = this.FilterPackagesUsingProfile();


            this.GetArtifactsTask(ct);
            
            try
            {
                this.ArtifactsTask.Wait();
            }
            catch (AggregateException ae)
            {
                this.AuditEnvironment.Error("Error in GetArtifacts task.", ae);
                return AuditResult.ERROR_SEARCHING_ARTIFACTS;
            }

            this.GetVulnerabilitiesTask(ct);
            try
            {
                this.VulnerabilitiesTask.Wait();
            }
            catch (AggregateException ae)
            {
                this.AuditEnvironment.Error(here, ae, "Error in GetVulnerabilities task");
                return AuditResult.ERROR_SEARCHING_VULNERABILITIES;
            }

            if (this.Vulnerabilities.Count == 0)
            {
                this.EvaluateVulnerabilitiesTask = Task.CompletedTask;
            }
            else
            {
                this.EvaluateVulnerabilitiesTask = Task.Run(() => this.EvaluateVulnerabilities(), ct);
            }
            try
            {
                this.EvaluateVulnerabilitiesTask.Wait();
            }
            catch (AggregateException ae)
            {
                this.AuditEnvironment.Error(here, ae.InnerException, "Error in {0} task.", ae.InnerException.TargetSite.Name);
                return AuditResult.ERROR_EVALUATING_VULNERABILITIES;
            }

            if (this.Vulnerabilities.Count == 0)
            {
                this.ReportAuditTask = Task.CompletedTask;
                this.AuditEnvironment.Info("Not reporting package source audit with zero vulnerabilities.");
            }
            else
            {
                this.ReportAuditTask = Task.Run(() => this.ReportAudit(), ct);
            }
            try
            {
                this.ReportAuditTask.Wait();
            }
            catch (AggregateException ae)
            {
                this.AuditEnvironment.Error(here, ae, "Error in {0} task.", ae.InnerException.TargetSite.Name);
            }

            return AuditResult.SUCCESS;
        }

        internal virtual Task GetPackagesTask(CancellationToken ct)
        {
            if (this.SkipPackagesAudit)
            {
                this.PackagesTask = Task.CompletedTask;
                this.Packages = new List<Package>();
            }
            else
            {
                this.AuditEnvironment.Status("Scanning {0} packages.", this.PackageManagerLabel);
                this.PackagesTask = Task.Run(() => this.Packages = this.GetPackages(), ct);
            }
            return this.PackagesTask;
        }

        protected virtual Task GetArtifactsTask(CancellationToken ct)
        {
            if (!this.ListArtifacts || this.Packages.Count() == 0)
            {
                this.ArtifactsTask = Task.CompletedTask;
            }
            else
            {
                List<Task> tasks = new List<Task>();
                foreach (IDataSource ds in this.DataSources.Where(d => d.IsEligibleForTarget(this) && d.Initialised))
                {
                    Task t = Task.Factory.StartNew(async () =>
                    {
                        Dictionary<IPackage, List<IArtifact>> artifacts = await ds.SearchArtifacts(this.Packages.ToList());
                        lock (artifacts_lock)
                        {
                            foreach (KeyValuePair<IPackage, List<IArtifact>> kv in artifacts)
                            {
                                this.Artifacts.Add(kv.Key, kv.Value);
                            }
                        }
                    }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default).Unwrap();
                    tasks.Add(t);
                }
                this.ArtifactsTask = Task.WhenAll(tasks);
            }
            return this.ArtifactsTask;
        }

        protected virtual Task GetVulnerabilitiesTask(CancellationToken ct)
        {
            if (this.SkipPackagesAudit || this.ListPackages || this.Packages.Count() == 0 || this.ListArtifacts)
            {
                this.VulnerabilitiesTask = Task.CompletedTask;
                return this.VulnerabilitiesTask;
            }
            List<Task> tasks = new List<Task>();
            IEnumerable<IDataSource> eligible_datasources = this.DataSources.Where(d => d.Initialised && d.IsEligibleForTarget(this));
            if (eligible_datasources.Count() == 0)
            {
                this.HostEnvironment.Warning("No eligible initialised vulnerabilities data sources found for audit target {0}.", this.PackageManagerLabel);
                this.VulnerabilitiesTask = Task.CompletedTask;
                return this.VulnerabilitiesTask;
            }
            else
            {
                foreach (IDataSource ds in eligible_datasources)
                {
                    Task t = Task.Factory.StartNew(async () =>
                    {
                        Dictionary<IPackage, List<IVulnerability>> vulnerabilities = await ds.SearchVulnerabilities(this.Packages.ToList());
                        if (vulnerabilities != null)
                        {
                            lock (vulnerabilities_lock)
                            {
                                foreach (KeyValuePair<IPackage, List<IVulnerability>> kv in vulnerabilities)
                                {
                                    this.Vulnerabilities.Add(kv.Key, kv.Value);
                                }
                                if (this.Vulnerabilities.Sum(v => v.Value.Count()) > 0)
                                {
                                    this.HostEnvironment.Success("Got {0} total vulnerabilities for {1} packages from data source {2}.",
                                        vulnerabilities.Values.Sum(vu => vu.Count), vulnerabilities.Keys.Count, ds.Info.Name);
                                }
                                else
                                {
                                    this.HostEnvironment.Warning("Got {0} total vulnerabilities for none of {1} packages from data source {2}.",
                                        vulnerabilities.Values.Sum(vu => vu.Count), this.Packages.Count(), ds.Info.Name);
                                }
                            }
                        }
                    }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default).Unwrap();
                    tasks.Add(t);
                }
                this.VulnerabilitiesTask = Task.WhenAll(tasks);
            }
            return this.VulnerabilitiesTask;
        }

        protected virtual Task GetVulnerableCredentialStorageTask(CancellationToken ct)
        {
            if (!(this is IVulnerableCredentialStore) || this.ListPackages || this.ListArtifacts)
            {
                return this.VulnerableCredentialStorageTask = Task.CompletedTask;
            }
            else
            {
                IVulnerableCredentialStore credentials_store = this as IVulnerableCredentialStore;
                this.AuditEnvironment.Status("Scanning for vulnerable credential storage candidates");
                return this.VulnerableCredentialStorageTask = Task.Run(() => credentials_store.GetVulnerableCredentialStorage());
            }

        }
        protected IEnumerable<Package> FilterPackagesUsingProfile()
        {
            if (this.Packages == null || this.Packages.Count() == 0 || this.AuditProfile == null || this.AuditProfile.Rules == null)
            {
                return this.Packages;
            }
            else if (this.AuditProfile.Rules.Any(r => r.Category == "exclude" && r.Target == this.PackageManagerId))
            {
                List<Package> packages = this.Packages.ToList();
                List<AuditProfileRule> exclude_rules = this.AuditProfile.Rules.Where(r => r.Category == "exclude" && r.Target == this.PackageManagerId).ToList();
                foreach (AuditProfileRule r in exclude_rules.Where(r => !string.IsNullOrEmpty(r.MatchName)))
                {
                    try
                    {
                        if (packages.Any(p => Regex.IsMatch(p.Name, r.MatchName)))
                        {
                            int c = packages.RemoveAll(p => Regex.IsMatch(p.Name, r.MatchName) &&
                                (string.IsNullOrEmpty(r.MatchVersion) || (!string.IsNullOrEmpty(r.MatchVersion) && this.IsVulnerabilityVersionInPackageVersionRange(r.MatchVersion, p.Version))));
                            AuditEnvironment.Info("Excluded {0} package(s) using audit profile rules.", c);

                        }
                    }
                    catch (Exception e)
                    {
                        AuditEnvironment.Warning("Error attempting to match name {0} with a package name: {1}. Skipping rule.", r.MatchName, e.Message);
                    }
                }
                return packages;
            }
            else return this.Packages;
        }
 
  
        protected void EvaluateVulnerabilities()
        {
            if (this.Vulnerabilities.Count == 0 || this.Vulnerabilities.Sum(kv => kv.Value.Count()) == 0) return;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            this.Vulnerabilities.AsParallel().ForAll((pv) =>
            {
                pv.Value.AsParallel().ForAll((vulnerability) =>
                {
                    List<Package> packages = this.Packages.Where(p => p.PackageManager == vulnerability.Package.PackageManager && p.Name == vulnerability.Package.Name).ToList();
                    foreach (Package p in packages)
                    {
                        try
                        {
                            if (vulnerability.Versions.Any(version => !string.IsNullOrEmpty(version) && this.IsVulnerabilityVersionInPackageVersionRange(version, p.Version)))
                            {
                                vulnerability.PackageVersionIsInRange = true;
                                vulnerability.Package = p;
                            }
                        }
                        catch (Exception e)
                        {
                            this.AuditEnvironment.Warning("Error determining vulnerability version range ({0}) in package {3} version range ({1}): {2}.",
                                vulnerability.Versions.Aggregate((f, s) => { return f + "," + s; }), p.Version, e.Message, vulnerability.Package.Name);
                        }
                    }
                });
            });
            sw.Stop();
            this.AuditEnvironment.Info("Evaluated {0} vulnerabilities with {1} matches to package version in {2} ms.", this.Vulnerabilities
                .Sum(pv => pv.Value.Count()), this.Vulnerabilities
                .Sum(pv => pv.Value.Count(v => v.PackageVersionIsInRange)), sw.ElapsedMilliseconds);
        }

        protected void ReportAudit()
        {
            if (this.AuditOptions.ContainsKey("GitHubReportName"))
            {
                this.AuditEnvironment.Status("Creating GitHub report for {0} package source audit.", this.PackageManagerLabel);
                AuditReporter reporter = new GitHubIssueReporter(this);
                if (reporter.ReportPackageSourceAudit().Result == true)
                {
                    this.AuditEnvironment.Info("Created GitHub issue audit report.");
                }
                else
                {
                    this.AuditEnvironment.Error("Failed to create GitHub audit report");
                }
            }
            else if (this.AuditOptions.ContainsKey("BitBucketReportName"))
            {
                this.AuditEnvironment.Status("Creating BitBucket report for {0} package source audit.", this.PackageManagerLabel);
                AuditReporter reporter = new BitBucketIssueReporter(this);
                if (reporter.ReportPackageSourceAudit().Result)
                {
                    this.AuditEnvironment.Info("Created BitBucket issue audit report.");
                }
                else
                {
                    this.AuditEnvironment.Error("Failed to create BitBucket audit report");
                }
            }
            else if (this.AuditOptions.ContainsKey("GitLabReportName"))
            {
                this.AuditEnvironment.Status("Creating GitLab report for {0} package source audit.", this.PackageManagerLabel);
                AuditReporter reporter = new GitLabIssueReporter(this);
                if (reporter.ReportPackageSourceAudit().Result)
                {
                    this.AuditEnvironment.Info("Created GitLab issue audit report.");
                }
                else
                {
                    this.AuditEnvironment.Error("Failed to create GitLab audit report");
                }
            }
        }




        #endregion

        #region Fields
        public object vulnerabilities_lock = new object();
        public object artifacts_lock = new object();
        #endregion

    }
}

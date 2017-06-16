using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DevAudit.AuditLibrary
{
    public class OSSIndexDataSource : HttpDataSource
    {
        #region Constructors
        public OSSIndexDataSource(AuditTarget target, AuditEnvironment host_env, Dictionary<string, object> options) : base(target, host_env, options)
        {
            this.PackageSource = target as PackageSource;
            this.Initialised = true;
            this.Info = new DataSourceInfo("OSS Index", "https://ossindex.net", "OSS Index is a free index of software information, focusing on vulnerabilities. The data has been made available to the community through a REST API as well as several open source tools (with more in development!). Particular focus is being made on software packages, both those used for development libraries as well as installation packages.");
        }
        #endregion

        #region Overriden methods
        public override Task<Dictionary<IPackage, List<IArtifact>>> SearchArtifacts(List<Package> packages)
        {
            this.Packages = packages;
            this.GetArtifacts();
            return Task.FromResult(this.ArtifactsForQuery.Select(kv => new KeyValuePair<IPackage, List<IArtifact>>(kv.Key as IPackage, kv.Value
                .Select(a => a as IArtifact)
                .ToList())).ToDictionary(x => x.Key, x => x.Value));
        }

        public override Task<Dictionary<IPackage, List<IVulnerability>>> SearchVulnerabilities(List<Package> packages)
        {
            this.Packages = packages;
            this.GetVulnerabiltiesApiv2();
            return Task.FromResult(this.Vulnerabilities.Select(kv => new KeyValuePair<IPackage, List<IVulnerability>>(kv.Key as IPackage, kv.Value.Select(v => v as IVulnerability)
                .ToList())).ToDictionary(x => x.Key, x => x.Value)); 
        }

        public override bool IsEligibleForTarget(AuditTarget target)
        {
            if (target is ApplicationServer)
            {
                ApplicationServer server = target as ApplicationServer;
                string[] eligible_servers = { "ossi" };
                return eligible_servers.Contains(server.PackageManagerId);
            }
            else if (target is Application)
            {
                Application application = target as Application;
                string[] eligible_applications = { "drupal" };
                return eligible_applications.Contains(application.PackageManagerId);
            }
            else if (target is PackageSource)
            {
                PackageSource source = target as PackageSource;
                string[] eligible_sources = { "nuget", "bower", "composer", "choco", "msi", "yarn", "oneget" };
                return eligible_sources.Contains(source.PackageManagerId);
            }
 

            else return false;
        }
        #endregion

        #region Overriden properties
        public override int MaxConcurrentSearches
        {
            get
            {
                return 15;
            }
        }
        #endregion

        #region Methods
        protected Tuple<int, int> GetArtifacts()
        {
            CallerInformation here = this.HostEnvironment.Here();
            this.HostEnvironment.Status("Searching OSS Index for artifacts for {0} packages.", this.Packages.Count());
            Stopwatch sw = new Stopwatch();
            sw.Start();
            object artifacts_lock = new object();
            int package_count = 0, artifact_count = 0;
            int i = 0;
            IGrouping<int, Package>[] packages_groups = this.Packages.GroupBy(x => i++ / 100).ToArray();
            IEnumerable<Package>[] queries = packages_groups.Select(group => packages_groups.Where(g => g.Key == group.Key).SelectMany(g => g)).ToArray();
            List<Task> tasks = new List<Task>();
            foreach (IEnumerable<Package> q in queries)
            {
                Task t = Task.Factory.StartNew(async (o) =>
                {
                    IEnumerable<OSSIndexArtifact> artifacts = await this.HttpClient.SearchAsync(this.PackageSource.PackageManagerId, q, this.ArtifactsTransform);
                    lock (artifacts_lock)
                    {
                        var aa = AddArtifact(q, artifacts);
                        package_count += aa.Key.Count();
                        artifact_count += aa.Value.Count();
                    }
                }, q, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default).Unwrap();
                tasks.Add(t);
            }
            try
            {
                Task.WaitAll(tasks.ToArray());
            }
            catch (AggregateException ae)
            {
                this.HostEnvironment.Error(here, "Error occured searching for artifacts on OSS Index.", ae);
            }
            finally
            {
                sw.Stop();
            }
            if (artifact_count > 0)
            {
                this.HostEnvironment.Success("Found {0} artifacts on OSS Index in {1} ms.", artifact_count, sw.ElapsedMilliseconds);
            }
            else
            {
                this.HostEnvironment.Warning("Found 0 artifacts on OSS Index in {0} ms.", sw.ElapsedMilliseconds);
            }
            return new Tuple<int, int>(package_count, artifact_count);
        }


        private async Task<KeyValuePair<IEnumerable<Package>, IEnumerable<OSSIndexArtifact>>> AddArtifactAsync(IEnumerable<Package> query, Task<IEnumerable<OSSIndexArtifact>> artifact_task)
        {
            var artifact = await artifact_task;
            lock (artifacts_lock)
            {
                this._ArtifactsForQuery.Add(query, artifact);
                return new KeyValuePair<IEnumerable<Package>, IEnumerable<OSSIndexArtifact>>(query, artifact);
            }
        }

        private KeyValuePair<IEnumerable<Package>, IEnumerable<OSSIndexArtifact>> AddArtifact(IEnumerable<Package> query, IEnumerable<OSSIndexArtifact> artifact)
        {
            lock (artifacts_lock)
            {
                this._ArtifactsForQuery.Add(query, artifact);
                return new KeyValuePair<IEnumerable<Package>, IEnumerable<OSSIndexArtifact>>(query, artifact);
            }
        }

        private void AddVulnerability(Package package, List<OSSIndexApiv2Vulnerability> vulnerabilities)
        {
            foreach(OSSIndexApiv2Vulnerability v in vulnerabilities)
            {
                v.DataSource = this.Info;
            }
            lock (vulnerabilities_lock)
            {
                this._Vulnerabilities.Add(package, vulnerabilities);
            }
        }

        protected void GetVulnerabiltiesApiv2()
        {
            CallerInformation here = this.HostEnvironment.Here();
            this.HostEnvironment.Status("Searching OSS Index for vulnerabilities for {0} packages.", this.Packages.Count());
            Stopwatch sw = new Stopwatch();
            sw.Start();
            int i = 0;
            IGrouping<int, Package>[] packages_groups = this.Packages.GroupBy(x => i++ / 100).ToArray();
            IEnumerable<Package>[] queries = packages_groups.Select(group => packages_groups.Where(g => g.Key == group.Key).SelectMany(g => g)).ToArray();
            List<Task> tasks = new List<Task>();
            foreach (IEnumerable<Package> q in queries)
            {
                Task t = Task.Factory.StartNew(async (o) =>
                {
                    try
                    {
                        List<OSSIndexApiv2Result> results = await this.HttpClient.SearchVulnerabilitiesAsync(q, this.VulnerabilitiesResultsTransform);
                        foreach (OSSIndexApiv2Result r in results)
                        {
                            if (r.Vulnerabilities != null && r.Vulnerabilities.Count > 0)
                            {
                                this.AddVulnerability(r.Package, r.Vulnerabilities);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        if (e is HttpException)
                        {
                            this.HostEnvironment.Error(here, e, "An HTTP error occured attempting to query the OSS Index API for the following {1} packages: {0}.",
                                q.Select(query => query.Name).Aggregate((q1, q2) => q1 + "," + q2), this.PackageSource.PackageManagerLabel);
                        }
                        else
                        {
                            this.HostEnvironment.Error(here, e, "An error occurred attempting to query the OSS Index API for the following {1} packages: {0}.",
                                q.Select(query => query.Name).Aggregate((q1, q2) => q1 + "," + q2), this.PackageSource.PackageManagerLabel);

                        }
                    }
                }, i, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default).Unwrap();
                tasks.Add(t);
            }
            try
            {
                Task.WaitAll(tasks.ToArray());
            }
            catch (AggregateException ae)
            {
                this.HostEnvironment.Error(here, ae, "An error occurred waiting for SearchVulnerabilitiesAsync task to complete in {0}.", ae.InnerException.TargetSite.Name);
            }
            finally
            {
                sw.Stop();
            }
            return;
        }

        #endregion

        #region Properties
        protected OSSIndexHttpClient HttpClient { get; set; } = new OSSIndexHttpClient("2.0");

        protected PackageSource PackageSource { get; set; }

        protected List<Package> Packages { get; set; }

        protected Dictionary<IEnumerable<Package>, IEnumerable<OSSIndexArtifact>> ArtifactsForQuery
        {
            get
            {
                return _ArtifactsForQuery;
            }
        }

        protected Dictionary<OSSIndexArtifact, OSSIndexProject> ArtifactProject
        {
            get
            {
                return this._ArtifactProject;
            }
        }

        protected virtual Func<List<OSSIndexArtifact>, List<OSSIndexArtifact>> ArtifactsTransform { get; } = (artifacts) =>
        {
            List<OSSIndexArtifact> o = artifacts;
            foreach (OSSIndexArtifact a in o)
            {
                if (a.Search == null || a.Search.Count() != 4)
                {
                    throw new Exception("Did not receive expected Search field properties for artifact name: " + a.PackageName + " id: " +
                        a.PackageId + " project id: " + a.ProjectId + ".");
                }
                else
                {
                    Package package = new Package(a.Search[0], a.Search[1], a.Search[3], "");
                    a.Package = package;
                }
            }
            return o.ToList();
        };

        public IEnumerable<OSSIndexArtifact> Artifacts
        {
            get
            {
                return this._ArtifactsForQuery.Values.SelectMany(a => a).Where(a => string.IsNullOrEmpty(a.Version) || (!string.IsNullOrEmpty(a.Version)
                    && this.PackageSource.IsVulnerabilityVersionInPackageVersionRange(a.Version, a.Package.Version)));//.GroupBy(a => new { a.PackageName}).Select(d => d.First());
            }
        }

        protected Func<List<Package>, List<Package>> VulnerabilitiesQueryTransform { get; } = (packages) =>
        {
            List<Package> o = packages;
            foreach (Package p in o)
            {
                Package package = new Package(p.PackageManager, p.Name, "*", p.Group);
            }
            return o.ToList();
        };


        protected Func<List<OSSIndexApiv2Result>, List<OSSIndexApiv2Result>> VulnerabilitiesResultsTransform { get; } = (results) =>
        {
            List<OSSIndexApiv2Result> o = results;
            foreach (OSSIndexApiv2Result r in o)
            {
                if (string.IsNullOrEmpty(r.PackageManager) || string.IsNullOrEmpty(r.PackageName))
                {
                    throw new Exception("Did not receive expected fields for result with id: " + r.Id);
                }
                else
                {
                    Package package = new Package(r.PackageManager, r.PackageName, r.PackageVersion, "");
                    r.Package = package;
                    if (r.Vulnerabilities != null)
                    {
                        r.Vulnerabilities.ForEach(v => v.Package = package);
                    }
                    else
                    {
                        r.Vulnerabilities = new List<OSSIndexApiv2Vulnerability>();
                    }
                }
            }
            return o.ToList();
        };

        protected List<OSSIndexArtifact> ArtifactsWithProjects
        {
            get
            {
                return this.Artifacts.Where(a => !string.IsNullOrEmpty(a.ProjectId)).ToList();
            }
        }


        protected Dictionary<Package, List<OSSIndexApiv2Vulnerability>> Vulnerabilities
        {
            get
            {
                return this._Vulnerabilities;
            }
        }

        #endregion

        #region Fields
        private readonly object artifacts_lock = new object(), vulnerabilities_lock = new object();
        private Dictionary<IEnumerable<Package>, IEnumerable<OSSIndexArtifact>> _ArtifactsForQuery =new Dictionary<IEnumerable<Package>, IEnumerable<OSSIndexArtifact>>();
        private Dictionary<OSSIndexArtifact, OSSIndexProject> _ArtifactProject = new Dictionary<OSSIndexArtifact, OSSIndexProject>();
        private Dictionary<Package, List<OSSIndexApiv2Vulnerability>> _Vulnerabilities = new Dictionary<Package, List<OSSIndexApiv2Vulnerability>>();
        #endregion

    }
}

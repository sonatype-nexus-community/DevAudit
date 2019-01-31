using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Threading;
using PackageUrl;
using System.Runtime.Caching;
using System.Reflection;
using System.IO;

namespace DevAudit.AuditLibrary
{
    public class OSSIndexApiv3DataSource : HttpDataSource
    {
        #region Properties
        protected PackageSource PackageSource { get; set; }

        private string HOST = "https://ossindex.sonatype.org/api/";

        private FileCache cache = null;

        private long cacheExpiration { get; set; } = 43200; // Seconds in 12 hours

        #endregion

        #region Constructors
        public OSSIndexApiv3DataSource(AuditTarget target, Dictionary<string, object> options) : base(target, options)
        {
            this.ApiUrl = new Uri(HOST);
            this.PackageSource = target as PackageSource;
            this.Initialised = true;
            this.Info = new DataSourceInfo("OSS Index", "https://ossindex.sonatype.org", "OSS Index is a free index of software information, focusing on vulnerabilities. The data has been made available to the community through a REST API as well as several open source tools. Particular focus is being made on software packages, both those used for development libraries as well as installation packages.");

            // Get an appropriate place for the cache and initialize it
            OperatingSystem os = Environment.OSVersion;
            PlatformID pid = os.Platform;
            switch (pid)
            {
                case PlatformID.Win32NT:
                case PlatformID.Win32S:
                case PlatformID.Win32Windows:
                case PlatformID.WinCE:
                    {
                        var directory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                        string path = Path.Combine(directory, "OSSIndex", "cache");
                        cache = new FileCache(path, new ObjectBinder());
                        break;
                    }
                case PlatformID.Unix:
                case PlatformID.MacOSX:
                    {
                        string home = Environment.GetEnvironmentVariable("HOME");
                        string path = Path.Combine(home, ".ossindex", "cache");
                        cache = new FileCache(path, new ObjectBinder());
                        break;
                    }

                default:
                    cache = new FileCache(new ObjectBinder());
                    break;
            }
        }
        #endregion

        #region Overriden methods
        public override Task<Dictionary<IPackage, List<IArtifact>>> SearchArtifacts(List<Package> packages)
        {
            throw new NotImplementedException();
        }

        public override async Task<Dictionary<IPackage, List<IVulnerability>>> SearchVulnerabilities(List<Package> inPackages)
        {
            CallerInformation here = this.HostEnvironment.Here();
            this.HostEnvironment.Status("Searching OSS Index for vulnerabilities for {0} packages.", inPackages.Count());
            Stopwatch sw = new Stopwatch();
            sw.Start();
            int i = 0;

            List<Package> doPackages = new List<Package>();
            foreach(var package in inPackages)
            {
                string purl = package.getPurl();
                try
                {
                    OSSIndexApiv3Package cachedPkg = (OSSIndexApiv3Package)cache[purl];
                    if (cachedPkg != null)
                    {
                        long now = DateTime.UtcNow.Ticks / TimeSpan.TicksPerSecond;
                        long diff = now - cachedPkg.CachedAt;
                        if (diff < cacheExpiration)
                        {
                            this.AddVulnerability(cachedPkg.Package, cachedPkg.Vulnerabilities);
                        }
                        else
                        {
                            doPackages.Add(package);
                        }
                    }
                    else
                    {
                        doPackages.Add(package);
                    }
                }
                catch (Exception ignore)
                {
                    cache.Remove(purl);
                }
            }

            IGrouping<int, Package>[] packages_groups = doPackages.GroupBy(x => i++ / 100).ToArray();
            IEnumerable<Package>[] queries = packages_groups.Select(group => packages_groups.Where(g => g.Key == group.Key).SelectMany(g => g)).ToArray();
            List<Task> tasks = new List<Task>();
            foreach (IEnumerable<Package> q in queries)
            {
                Task t = Task.Factory.StartNew(async (o) =>
                {
                    try
                    {
                        this.HostEnvironment.Info("------------------------------- Performing query on {0} packages", q.Count());

                        List<OSSIndexApiv3Package> results = await SearchVulnerabilitiesAsync(q, this.VulnerabilitiesResultsTransform);
                        foreach (OSSIndexApiv3Package r in results)
                        {
                            r.CachedAt = DateTime.UtcNow.Ticks / TimeSpan.TicksPerSecond;
                            cache[r.Coordinates] = r;
                            if (r.Vulnerabilities != null && r.Vulnerabilities.Count > 0)
                            {
                                this.AddVulnerability(r.Package, r.Vulnerabilities);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());

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

            return await Task.FromResult(this._Vulnerabilities.Select(kv => new KeyValuePair<IPackage, List<IVulnerability>>(kv.Key as IPackage, kv.Value.Select(v => v as IVulnerability)
                 .ToList())).ToDictionary(x => x.Key, x => x.Value));
        }

        public override bool IsEligibleForTarget(AuditTarget target)
        {
            if (target is PackageSource)
            {
                PackageSource source = target as PackageSource;
                string[] eligible_sources = {"nuget", "bower", "composer", "chocolatey", "yarn", "oneget", "netcore" };
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
                return 1;
            }
        }
        #endregion

        #region Methods
        public async Task<List<OSSIndexApiv3Package>> SearchVulnerabilitiesAsync(IEnumerable<Package> packages,
            Func<List<OSSIndexApiv3Package>, List<OSSIndexApiv3Package>> transform)
        {
            string server_api_version = "3";
            IEnumerable<Package> packages_for_query = packages.Select(p => new Package(p.PackageManager, p.Name, p.Version, string.Empty, p.Group));
            OSSIndexApiv3Query query = new OSSIndexApiv3Query();

            // Convert the packages into a list of string
            foreach (Package pkg in packages_for_query)
            {
                query.addCoordinate(pkg.getPurl());
            }

            using (HttpClient client = CreateHttpClient())
            {
                string url = "v" + server_api_version + "/component-report";
                string content = JsonConvert.SerializeObject(query);

                HttpResponseMessage response = await client.PostAsync(url,
                    new StringContent(content, Encoding.UTF8, "application/json"));
                if (response.IsSuccessStatusCode)
                {
                    string r = await response.Content.ReadAsStringAsync();
                    List<OSSIndexApiv3Package> results = JsonConvert.DeserializeObject<List<OSSIndexApiv3Package>>(r);
                    if (results != null && results.Count > 0 && transform != null)
                    {
                        return transform(results);
                    }
                    else
                    {
                        return results;
                    }
                }
                else
                {
                    throw new HttpException("packages", response.StatusCode, response.ReasonPhrase, response.RequestMessage);
                }
            }
        }

        protected Func<List<Package>, List<Package>> VulnerabilitiesQueryTransform { get; } = (packages) =>
        {
            List<Package> o = packages;
            foreach (Package p in o)
            {
                Package package = new Package(p.PackageManager, p.Name, p.Version, p.Group);
            }
            return o.ToList();
        };


        protected Func<List<OSSIndexApiv3Package>, List<OSSIndexApiv3Package>> VulnerabilitiesResultsTransform { get; } = (results) =>
        {
            List<OSSIndexApiv3Package> o = results;
            foreach (OSSIndexApiv3Package r in o)
            {
                if (string.IsNullOrEmpty(r.Coordinates))
                {
                    throw new Exception("Did not receive expected coordinate for result");
                }
                else
                {
                    PackageURL purl = r.GetPackageURL();
                    Package package = null;
                    if (string.IsNullOrEmpty(purl.Namespace))
                    {
                        package = new Package(purl.Type, purl.Name, purl.Version, "");
                    }
                    else
                    {
                        package = new Package(purl.Type, purl.Name, purl.Version, purl.Namespace);
                    }
                    r.Package = package;
                    if (r.Vulnerabilities != null)
                    {
                        r.Vulnerabilities.ForEach(v => v.Package = package);
                    }
                    else
                    {
                        r.Vulnerabilities = new List<OSSIndexApiv3Vulnerability>();
                    }
                }
            }
            return o.ToList();
        };

        private void AddVulnerability(Package package, List<OSSIndexApiv3Vulnerability> vulnerabilities)
        {
            foreach (OSSIndexApiv3Vulnerability v in vulnerabilities)
            {
                v.DataSource = this.Info;
                v.addVersion(package.Version);
            }
            lock (vulnerabilities_lock)
            {
                this._Vulnerabilities.Add(package, vulnerabilities);
            }
        }

        #endregion

        #region Properties
        #endregion

        #region Fields
        private readonly object artifacts_lock = new object(), vulnerabilities_lock = new object();
        private Dictionary<Package, List<OSSIndexApiv3Vulnerability>> _Vulnerabilities = new Dictionary<Package, List<OSSIndexApiv3Vulnerability>>();
        #endregion

    }

    /** https://github.com/acarteas/FileCache
     */
    public sealed class ObjectBinder : System.Runtime.Serialization.SerializationBinder
    {
        public override Type BindToType(string assemblyName, string typeName)
        {
            Type typeToDeserialize = null;
            String currentAssembly = Assembly.GetExecutingAssembly().FullName;

            // In this case we are always using the current assembly
            assemblyName = currentAssembly;

            // Get the type using the typeName and assemblyName
            typeToDeserialize = Type.GetType(String.Format("{0}, {1}",
            typeName, assemblyName));

            return typeToDeserialize;
        }
    }
}

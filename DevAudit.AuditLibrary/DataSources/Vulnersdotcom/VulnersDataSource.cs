using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace DevAudit.AuditLibrary
{
    public class VulnersDataSource : HttpDataSource
    {
        #region Constructors
        public VulnersDataSource(AuditTarget target, Dictionary<string, object> options) : base(target, options)
        {
            this.ApiUrl = new Uri("https://vulners.com");
            if (this.DataSourceOptions.ContainsKey("OSName"))
            {
                this.OSName = (string)this.DataSourceOptions["OSName"];
            }
            else if (!string.IsNullOrEmpty(this.AuditEnvironment.GetOSName()))
            {
                this.OSName = this.AuditEnvironment.OSName;
            }
            else
            {
                this.HostEnvironment.Error("The audit environment OS name could not be determined. Data source cannot be initialised.");
                return;
            }

            if (this.DataSourceOptions.ContainsKey("OSVersion"))
            {
                this.OSVersion = (string)this.DataSourceOptions["OSVersion"];
            }
            else if (!string.IsNullOrEmpty(this.AuditEnvironment.GetOSVersion()))
            {
                this.OSVersion = this.AuditEnvironment.OSVersion;
            }
            else
            {
                this.HostEnvironment.Error("The audit environment OS version could not be determined. Data source cannot be initialised.");
                return;
            }

            switch(this.OSName)
            {
                case "ubuntu":
                case "debian":
                    PackageToSearchName = (p) =>
                    {
                        return p.Name + " " + p.Version + " " + p.Architecture;
                    };
                    break;
                case "centos":
                    PackageToSearchName = (p) =>
                    {
                        return p.Name + "-" + p.Version + "." + p.Architecture;
                    };
                    break;
                default:
                    throw new NotSupportedException("Unknown OS type.");
            }
            this.Info = new DataSourceInfo("Vulners", "https://vulners.com", "Vulners.com is the security database containing descriptions for large amount of software vulnerabilities in machine-readable format. Cross-references between bulletins and continuously updating of database keeps you abreast of the latest information security threats.");
            this.Initialised = true;
        }
        #endregion

        #region Overriden methods
        public override bool IsEligibleForTarget(AuditTarget target)
        {
            if (target is PackageSource)
            {
                PackageSource source = target as PackageSource;
                string[] eligible_sources = { "dpkg", "rpm", "yum" };
                return eligible_sources.Contains(source.PackageManagerId);
            }
            else return false;
        }
      
        public override async Task<Dictionary<IPackage, List<IVulnerability>>> SearchVulnerabilities(List<Package> packages)
        {
            CallerInformation here = this.HostEnvironment.Here();
            VulnersAuditQuery q = new VulnersAuditQuery(this.OSName, this.OSVersion, packages.Select(p => this.PackageToSearchName(p)).ToArray());
            this.HostEnvironment.Debug("Vulners query OS name is set to {0}.", q.os);
            this.HostEnvironment.Debug("Vulners query OS version is set to {0}.", q.version);
            this.HostEnvironment.Debug("Vulners query packages are {0}.", q.package.Aggregate((s1, s2) => s1 + "," + s2));
            VulnersAuditResult audit_result;
            this.HostEnvironment.Status("Searching Vulners for vulnerabilities for {0} packages.", packages.Count);
            using (HttpClient client = CreateHttpClient())
            {
                HttpResponseMessage response = await client.PostAsync("/api/v3/audit/audit/",
                    new StringContent(JsonConvert.SerializeObject(q), Encoding.UTF8, "application/json"));
                if (response.IsSuccessStatusCode)
                {
                    string r = await response.Content.ReadAsStringAsync();
                    try
                    {
                         audit_result = JsonConvert.DeserializeObject<VulnersAuditResult>(r);
                        
                    }
                    catch (Exception e)
                    {
                        this.HostEnvironment.Error(e, "An error occurred deserializing the data returned from the Vulners API.");
                        return null;
                    }
                }
                else
                {
                    throw new HttpException("/api/v3/audit/audit/", response.StatusCode, response.ReasonPhrase, response.RequestMessage);
                }
            }
            if (audit_result == null)
            {
                this.HostEnvironment.Error(here, "Vulners API returned null.");
                return null;
            }
            else if (audit_result.result != "OK")
            {
                this.HostEnvironment.Error(here, "Vulners API did not return result OK: {0}", audit_result.result);
                return null;
            }
            else if (audit_result.data == null)
            {
                this.HostEnvironment.Error(here, "Vulners API returned null data in result.");
                return null;
            }
            this.HostEnvironment.Debug("Vulners API response ID is {0}.", audit_result.data.id);
            Dictionary<IPackage, List<IVulnerability>> vulnerabilities = new Dictionary<IPackage, List<IVulnerability>>();
            if (audit_result.data.packages.Count == 0)
            {
                this.HostEnvironment.Info("Vulners API returned 0 vulnerable packages.");
                return vulnerabilities;
            }
            List<Package> vulnerable_packages = packages.Where(p => audit_result.data.packages.Keys.Contains(this.PackageToSearchName(p))).ToList();
            Dictionary<IPackage, string> package_vulnerability_map = new Dictionary<IPackage, string>();
            foreach (KeyValuePair<string, Dictionary<string, VulnersAuditResultPackage[]>> kv in audit_result.data.packages)
            {
                Package vulnerable_package = vulnerable_packages.Where(p => this.PackageToSearchName(p) == kv.Key).Single();
                List<IVulnerability> package_vulnerabilities = new List<IVulnerability>(kv.Value.Count);
                foreach (KeyValuePair<string, VulnersAuditResultPackage[]> kv2 in kv.Value)
                {
                    Vulnerability v = new Vulnerability
                    {
                        Id = kv2.Key,
                        Package = vulnerable_package,
                        DataSource = this.Info
                    };
                    package_vulnerabilities.Add(v);
                }
                vulnerabilities.Add(vulnerable_package, package_vulnerabilities);
            }
            if (vulnerabilities.Count == 0)
            {
                this.HostEnvironment.Info("Vulners returned 0 vulnerability records for packages.");
                return vulnerabilities;
            }
            else
            {
                this.HostEnvironment.Success("Got {0} distinct and {2} total vulnerability records for {1} packages from Vulners.", vulnerabilities.Values.SelectMany(v => v)
                    .Select(v => v.Id).Distinct().Count(), vulnerabilities.Keys.Count, vulnerabilities.Values.Sum(v => v.Count));
            }
            string[] vids = vulnerabilities.Values.SelectMany(v => v).Select(v => v.Id).Distinct().ToArray();
            VulnersSearchQuery q2 = new VulnersSearchQuery(vids, false);
            VulnersIdSearchResult search_result;
            this.HostEnvironment.Status("Searching Vulners for details for {0} vulnerabilities.", vids.Count());
            using (HttpClient client = CreateHttpClient())
            {
                HttpResponseMessage response = await client.PostAsync("/api/v3/search/id",
                    new StringContent(JsonConvert.SerializeObject(q2), Encoding.UTF8, "application/json"));
                if (response.IsSuccessStatusCode)
                {
                    string r = await response.Content.ReadAsStringAsync();
                    try
                    {
                        search_result = JsonConvert.DeserializeObject<VulnersIdSearchResult>(r);

                    }
                    catch (Exception e)
                    {
                        this.HostEnvironment.Error(e, "An error occurred deserializing the data returned from the Vulners API.");
                        return null;
                    }
                }
                else
                {
                    throw new HttpException("/api/v3/audit/audit/", response.StatusCode, response.ReasonPhrase, response.RequestMessage);
                }
            }
            if (search_result == null)
            {
                this.HostEnvironment.Error(here, "Vulners API returned null.");
                return null;
            }
            else if (search_result.result != "OK")
            {
                this.HostEnvironment.Error(here, "Vulners API did not return result OK: {0}", search_result.result);
                return null;
            }
            else if (search_result.data == null)
            {
                this.HostEnvironment.Error(here, "Vulners API returned null data in result.");
                return null;
            }
            else if (search_result.data.documents.Count == 0)
            {
                this.HostEnvironment.Error(here, "Vulners API returned 0 documents with vulnerability details in result.");
                return null;
            }
            this.HostEnvironment.Success("Got {0} documents with vulnerability details from Vulners.", search_result.data.documents.Count);
            foreach(KeyValuePair<string, VulnersIdSearchResultDocument> d in search_result.data.documents)
            {
                IEnumerable<Vulnerability> doc_vulnerabilities = vulnerabilities.Values.SelectMany(v => v).Where(v => v.Id == d.Key).Select(v => v as Vulnerability);
                foreach (Vulnerability v in doc_vulnerabilities)
                {
                    v.CVE = d.Value.cvelist;
                    v.Reporter = d.Value.reporter;
                    v.Description = d.Value.description;
                    v.Title = d.Value.title;
                    v.Versions = d.Value.affectedPackage
                        .Select(ap => MapVulnersOperatorToSymbol(ap._operator) + ap.packageVersion).ToArray();
                    v.Published = d.Value.published;
                    v.CVSS = new VulnerabilityCVSS() { Score = d.Value.cvss.score.ToString(), Vector = d.Value.cvss.vector };

                }
            }
            return vulnerabilities;
        }

        public override Task<Dictionary<IPackage, List<IArtifact>>> SearchArtifacts(List<Package> packages)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Overriden properties
        public override int MaxConcurrentSearches
        {
            get
            {
                return 10;
            }
        }
        #endregion

        #region Properties
        public string OSName { get; protected set; }
        public string OSVersion { get; protected set; }
        public VulnersAuditResult AuditResult { get; protected set; }
        #endregion

        #region Methods
        private string MapVulnersOperatorToSymbol(string op)
        {
            switch (op)
            {
                case "lt":
                    return "<";
                case "gt":
                    return ">";
                case "eq":
                    return "=";
                case "lte":
                    return "<=";
                case "gte":
                    return ">=";
                default:
                    this.HostEnvironment.Warning("Unknown Vulners operator: " + op);
                    return string.Empty;
            }
        }
        #endregion
    }
}

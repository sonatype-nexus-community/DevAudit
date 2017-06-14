using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace DevAudit.AuditLibrary
{
    public class VulnersdotcomDataSource : HttpDataSource
    {
        #region Constructors
        public VulnersdotcomDataSource(AuditTarget target, AuditEnvironment host_env, Dictionary<string, object> options) : base(target, host_env, options)
        {
            this.ApiUrl = new Uri("https://vulners.com");
            if (this.DataSourceOptions.ContainsKey("OSName"))
            {
                this.OSName = (string)this.DataSourceOptions["OSName"];
            }
            else
            {
                this.HostEnvironment.Error("The audit environment OS name was not specified. Data source cannot be initialised.");
                return;
            }
            if (this.DataSourceOptions.ContainsKey("OSVersion"))
            {
                this.OSVersion = (string)this.DataSourceOptions["OSVersion"];
                this.Initialised = true;
            }
            else
            {
                this.HostEnvironment.Error("The audit environment OS version was not specified. Data source cannot be initialised.");
                return;
            }
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
            VulnersdotcomAuditQuery q = new VulnersdotcomAuditQuery(this.OSName, this.OSVersion, packages);
            VulnersdotcomAuditResult audit_result;
            using (HttpClient client = CreateHttpClient())
            {
                HttpResponseMessage response = await client.PostAsync("/api/v3/audit/audit/",
                    new StringContent(JsonConvert.SerializeObject(q), Encoding.UTF8, "application/json"));
                if (response.IsSuccessStatusCode)
                {
                    string r = await response.Content.ReadAsStringAsync();
                    try
                    {
                         audit_result = JsonConvert.DeserializeObject<VulnersdotcomAuditResult>(r);
                        
                    }
                    catch (Exception e)
                    {
                        this.HostEnvironment.Error(e, "An error occurred deserializing the data returned from the vulners.com API.");
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
                this.HostEnvironment.Error(here, "vulners.com API returned null.");
                return null;
            }
            else if (audit_result.result != "OK")
            {
                this.HostEnvironment.Error(here, "vulners.com API did not return result OK: {0}", audit_result.result);
                return null;
            }
            else if (audit_result.data == null)
            {
                this.HostEnvironment.Error(here, "vulners.com API returned null data.");
                return null;
            }
            this.HostEnvironment.Debug("vulners.com API response ID is {0}.", audit_result.data.id);
            return null;
        }

        public override Task<Dictionary<IPackage, List<IArtifact>>> SearchArtifacts(List<Package> packages)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Overriden properties
        public override string Name
        {
            get
            {
                return "Vulners.com";
            }
        }

        public override string Description
        {
            get
            {
                return "https://vulners.com Vulners.com is the security database containing descriptions for large amount of software vulnerabilities in machine-readable format. Cross-references between bulletins and continuously updating of database keeps you abreast of the latest information security threats.";
            }
        }
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
        public VulnersdotcomAuditResult AuditResult { get; protected set; }
        #endregion
    }
}

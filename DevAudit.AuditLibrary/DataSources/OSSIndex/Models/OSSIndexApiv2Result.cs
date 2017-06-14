using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace DevAudit.AuditLibrary
{
    public class OSSIndexApiv2Result
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("pm")]
        public string PackageManager { get; set; }

        [JsonProperty("name")]
        public string PackageName { get; set; }

        [JsonProperty("version")]
        public string PackageVersion { get; set; }

        [JsonProperty("vulnerability-total")]
        public int VulnerabilityTotal { get; set; }

        [JsonProperty("vulnerability-matches")]
        public int VulnerabilityMatches { get; set; }

        [JsonProperty("vulnerabilities")]
        public List<OSSIndexApiv2Vulnerability> Vulnerabilities { get; set; }

        [JsonIgnore]
        public string PackageId { get; set; }

        [JsonIgnore]
        public Package Package { get; set; }

        [JsonIgnore]
        public bool CurrentPackageVersionIsInRange { get; set; }
    }

}

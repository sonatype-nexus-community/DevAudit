using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace DevAudit.AuditLibrary
{
    public class OSSIndexProject
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("uri")]
        public string Uri { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("Description")]
        public string Description { get; set; }

        [JsonProperty("hasVulnerability")]
        public bool HasVulnerability { get; set; }

        [JsonProperty("vulnerabilities")]
        public string Vulnerabilities { get; set; }

        [JsonIgnore]
        public OSSIndexQueryObject Package { get; set; }
    }

}

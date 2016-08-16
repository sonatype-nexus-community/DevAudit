using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;

using Newtonsoft.Json;

namespace DevAudit.AuditLibrary
{
    public class OSSIndexProjectConfigurationRule
    {
        [JsonProperty("uri")]
        public List<string> Urls { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("summary")]
        public string Summary { get; set; }

        [JsonProperty("details")]
        public string Details { get; set; }
        [JsonProperty("versions")]
        public List<string> Versions { get; set; }

        [JsonIgnore]
        public string ProjectId { get; set; }

        [JsonIgnore]
        public OSSIndexProject Project { get; set; }

        [JsonProperty]
        public string XPathTest { get; set; }

        [JsonProperty]
        public List<string> RequiredFiles { get; set; }
    }
}

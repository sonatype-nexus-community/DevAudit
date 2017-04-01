using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;

using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace DevAudit.AuditLibrary
{
    public class OSSIndexProjectConfigurationRule
    {

        [YamlMember(Alias = "tags")]
        [JsonProperty("tags")]
        public List<string> Tags { get; set; }

        [YamlMember(Alias = "category")]
        [JsonProperty("category")]
        public string Category { get; set; }

        [YamlMember(Alias = "severity")]
        [JsonProperty("severity")]
        public int Severity { get; set; }

        [YamlMember(Alias = "enable-for-app-dev")]
        [JsonProperty("enable-for-app-dev")]
        public bool EnableForAppDevelopment { get; set; } = true;

        [YamlMember(Alias = "urls")]
        [JsonProperty("urls")]
        public List<string> Urls { get; set; }

        [YamlMember(Alias = "id")]
        [JsonProperty("id")]
        public string Id { get; set; }

        [YamlMember(Alias = "title")]
        [JsonProperty("title")]
        public string Title { get; set; }

        [YamlMember(Alias = "summary")]
        [JsonProperty("summary")]
        public string Summary { get; set; }

        [YamlMember(Alias = "details")]
        [JsonProperty("details")]
        public string Details { get; set; }

        [YamlMember(Alias = "versions")]
        [JsonProperty("versions")]
        public List<string> Versions { get; set; }

        [JsonIgnore]
        public string ProjectId { get; set; }

        [JsonIgnore]
        public OSSIndexProject Project { get; set; }

        [YamlMember(Alias = "xpathtest")]
        [JsonProperty]
        public string XPathTest { get; set; }

        [YamlMember(Alias = "diagnostic")]
        [JsonProperty("diagnostic")]
        public string Diagnostic { get; set; }

        [YamlMember(Alias = "resolution")]
        [JsonProperty]
        public string Resolution { get; set; }

        [YamlMember(Alias = "required_files")]
        [JsonProperty]
        public List<string> RequiredFiles { get; set; }


    }
}

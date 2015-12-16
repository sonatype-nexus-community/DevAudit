using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace WinAudit.AuditLibrary
{
    public class OSSIndexQueryResultObject
    {
        [JsonProperty("name")]
        public string PackageName { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("project_id")]
        public string ProjectId { get; set; }

        [JsonProperty("scm_id")]
        public string SCMId { get; set; }

        public OSSIndexQueryResultObject() {}

    }
}

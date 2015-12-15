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
        public string PackageVersion { get; set; }

        [JsonProperty("scm_id")]
        public string PackageSCMId { get; set; }

        public OSSIndexQueryResultObject() {}

    }
}

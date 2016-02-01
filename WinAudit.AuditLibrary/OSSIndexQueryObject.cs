using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace WinAudit.AuditLibrary
{
    public class OSSIndexQueryObject
    {
        [JsonProperty("pm")]
        public string PackageManager { get; set; }

        [JsonProperty("group")]
        public string Group { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("vendor")]
        public string Vendor { get; set; }


        public OSSIndexQueryObject(string package_manager, string application_name, string version, string vendor, string group = null)
        {
            this.PackageManager = package_manager;
            this.Name = application_name;
            this.Version = version;
            this.Vendor = vendor;
            if (!string.IsNullOrEmpty(group)) this.Group = group;
        }
    }
}



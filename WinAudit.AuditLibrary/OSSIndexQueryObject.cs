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

        [JsonProperty("name")]
        public string ApplicationName { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("vendor")]
        public string Vendor { get; set; }


        public OSSIndexQueryObject(string package_manager, string application_name, string version, string vendor)
        {
            this.PackageManager = package_manager;
            this.ApplicationName = application_name;
            this.Version = version;
            this.Vendor = vendor;
        }
    }
}



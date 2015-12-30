using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Win32;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WinAudit.AuditLibrary
{
    public class BowerPackagesSource : PackageSource
    {
        public override OSSIndexHttpClient HttpClient { get; } = new OSSIndexHttpClient("1.1");

        public override string PackageManagerId { get { return "bower"; } }

        public override string PackageManagerLabel { get { return "Bower"; } }

        //Get bower packages from reading bower.json
        public override IEnumerable<OSSIndexQueryObject> GetPackages(params string[] o)
        {
            string file_name = @".\bower.json";
            if (!File.Exists(file_name)) file_name = @".\bower.json.example";
            using (JsonTextReader r = new JsonTextReader(new StreamReader(
                        file_name)))
            {
                JObject json = (JObject)JToken.ReadFrom(r);
                JObject dependencies = (JObject)json["dependencies"];
                return dependencies.Properties().Select(d => new OSSIndexQueryObject("bower", d.Name, d.Value.ToString(), ""));
            }
        }

    }
}

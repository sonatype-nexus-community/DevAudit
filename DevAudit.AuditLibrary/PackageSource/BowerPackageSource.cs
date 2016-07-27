using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Versatile;


namespace DevAudit.AuditLibrary
{
    public class BowerPackageSource : PackageSource
    {
        public override OSSIndexHttpClient HttpClient { get; } = new OSSIndexHttpClient("1.1");

        public override string PackageManagerId { get { return "bower"; } }

        public override string PackageManagerLabel { get { return "Bower"; } }

        //Get bower packages from reading bower.json
        public override IEnumerable<OSSIndexQueryObject> GetPackages(params string[] o)
        {
            using (JsonTextReader r = new JsonTextReader(new StreamReader(
                        this.PackageManagerConfigurationFile)))
            {
                JObject json = (JObject)JToken.ReadFrom(r);
                JObject dependencies = (JObject)json["dependencies"];
                JObject dev_dependencies = (JObject)json["devDependencies"];
                if (dev_dependencies != null)
                {
                    return dependencies.Properties().Select(d => new OSSIndexQueryObject("bower", d.Name, d.Value.ToString(), ""))
                        .Concat(dev_dependencies.Properties().Select(d => new OSSIndexQueryObject("bower", d.Name, d.Value.ToString(), "")));
                }
                else
                {
                    return dependencies.Properties().Select(d => new OSSIndexQueryObject("bower", d.Name, d.Value.ToString(), ""));
                }
            }
        }

        public override bool IsVulnerabilityVersionInPackageVersionRange(string vulnerability_version, string package_version)
        {
            string message = "";
            bool r = SemanticVersion.RangeIntersect(vulnerability_version, package_version, out message);
            if (!r && !string.IsNullOrEmpty(message))
            {
                throw new Exception(message);
            }
            else return r;
        }

        public BowerPackageSource() : base() {}

        public BowerPackageSource(Dictionary<string, object> package_source_options) : base(package_source_options)
        {
            if (string.IsNullOrEmpty(this.PackageManagerConfigurationFile))
            {
                this.PackageManagerConfigurationFile = @"bower.json";
            }
        }
    }
}

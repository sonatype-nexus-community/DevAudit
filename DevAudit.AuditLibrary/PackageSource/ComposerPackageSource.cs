using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Win32;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using SemverSharp;

namespace DevAudit.AuditLibrary
{
    public class ComposerPackageSource : PackageSource
    {
        public override OSSIndexHttpClient HttpClient { get; } = new OSSIndexHttpClient("1.1");

        public override string PackageManagerId { get { return "composer"; } }

        public override string PackageManagerLabel { get { return "Composer"; } }

        //Get  packages from reading bower.json
        public override IEnumerable<OSSIndexQueryObject> GetPackages(params string[] o)
        {
            using (JsonTextReader r = new JsonTextReader(new StreamReader(
                        this.PackageManagerConfigurationFile)))
            {
                JObject json = (JObject)JToken.ReadFrom(r);
                JObject require = (JObject)json["require"];
                JObject require_dev = (JObject)json["require-dev"];
                if (require_dev != null)
                {
                    return require.Properties().Select(d => new OSSIndexQueryObject("composer", d.Name.Split('/').Last(), d.Value.ToString(), "", d.Name.Split('/').First()))
                        .Concat(require_dev.Properties().Select(d => new OSSIndexQueryObject("composer", d.Name.Split('/').Last(), d.Value.ToString(), "", d.Name.Split('/').First())));
                }
                else
                {
                    return require.Properties().Select(d => new OSSIndexQueryObject("composer", d.Name.Split('/').Last(), d.Value.ToString(), "", d.Name.Split('/').First()));
                }
            }
        }

        public override bool IsVulnerabilityVersionInPackageVersionRange(string vulnerability_version, string package_version)
        {
            return SemanticVersion.RangeIntersect(vulnerability_version, package_version);
        }

        public ComposerPackageSource() : base() { }
        public ComposerPackageSource(Dictionary<string, object> package_source_options) : base(package_source_options)
        {            
            if (string.IsNullOrEmpty(this.PackageManagerConfigurationFile))
            {
                this.PackageManagerConfigurationFile = @"composer.json";
            }
            if (!File.Exists(this.PackageManagerConfigurationFile))
            {
                throw new Exception("Package manager configuration file not found.");
            }
        }
    }
}

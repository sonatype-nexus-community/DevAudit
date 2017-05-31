using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Versatile;

namespace DevAudit.AuditLibrary
{
    public class YarnPackageSource : PackageSource
    {
        #region Constructors
        public YarnPackageSource(Dictionary<string, object> package_source_options, EventHandler<EnvironmentEventArgs> message_handler = null) : base(package_source_options, message_handler)
        {
            if (string.IsNullOrEmpty(this.PackageManagerConfigurationFile))
            {
                this.PackageManagerConfigurationFile = @"package.json";
            }
        }
        #endregion

        #region Overriden properties
        public override string PackageManagerId { get { return "yarn"; } }

        public override string PackageManagerLabel { get { return "Yarn"; } }
        #endregion

        #region Overriden methods
        public override IEnumerable<OSSIndexQueryObject> GetPackages(params string[] o)
        {
            List<OSSIndexQueryObject> packages = new List<OSSIndexQueryObject>();
            AuditFileInfo config_file = this.AuditEnvironment.ConstructFile(this.PackageManagerConfigurationFile);
            JObject json = (JObject)JToken.Parse(config_file.ReadAsText());
            JObject dependencies = (JObject)json["dependencies"];
            JObject dev_dependencies = (JObject)json["devDependencies"];
            JObject peer_dependencies = (JObject)json["peerDependencies"];
            JObject optional_dependencies = (JObject)json["optionalDependencies"];
            JObject bundled_dependencies = (JObject)json["bundledDependencies"];
            if (dependencies != null)
            {
                packages.AddRange(dependencies.Properties().Select(d => new OSSIndexQueryObject("npm", d.Name, d.Value.ToString(), "")));
            }
            if (dev_dependencies != null)
            {
                packages.AddRange(dev_dependencies.Properties().Select(d => new OSSIndexQueryObject("npm", d.Name, d.Value.ToString(), "")));
            }
            if (peer_dependencies != null)
            {
                packages.AddRange(peer_dependencies.Properties().Select(d => new OSSIndexQueryObject("npm", d.Name, d.Value.ToString(), "")));
            }
            if (optional_dependencies != null)
            {
                packages.AddRange(optional_dependencies.Properties().Select(d => new OSSIndexQueryObject("npm", d.Name, d.Value.ToString(), "")));
            }
            if (bundled_dependencies != null)
            {
                packages.AddRange(bundled_dependencies.Properties().Select(d => new OSSIndexQueryObject("npm", d.Name, d.Value.ToString(), "")));
            }
            return packages;
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
        #endregion
    }
}

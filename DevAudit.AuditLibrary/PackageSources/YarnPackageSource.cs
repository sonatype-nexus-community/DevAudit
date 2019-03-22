using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Versatile;

using Alpheus;

namespace DevAudit.AuditLibrary
{
    public class YarnPackageSource : PackageSource, IDeveloperPackageManager
    {
        #region Constructors
        public YarnPackageSource(Dictionary<string, object> package_source_options, EventHandler<EnvironmentEventArgs> message_handler = null) : base(package_source_options, message_handler)
        {

        }
        #endregion

        #region Overriden members
        public override string PackageManagerId { get { return "yarn"; } }

        public override string PackageManagerLabel { get { return "Yarn"; } }

        public override string DefaultPackageManagerConfigurationFile { get { return "package.json"; } }
        
        public override IEnumerable<Package> GetPackages(params string[] o)
        {
            List<Package> packages = new List<Package>();
            AuditFileInfo config_file = this.AuditEnvironment.ConstructFile(this.PackageManagerConfigurationFile);
            JObject json = (JObject)JToken.Parse(config_file.ReadAsText());
            JObject dependencies = (JObject)json["dependencies"];
            JObject dev_dependencies = (JObject)json["devDependencies"];
            JObject peer_dependencies = (JObject)json["peerDependencies"];
            JObject optional_dependencies = (JObject)json["optionalDependencies"];
            JObject bundled_dependencies = (JObject)json["bundledDependencies"];
            if (dependencies != null)
            {
                packages.AddRange(dependencies.Properties().Select(d => new Package("npm", d.Name.Replace("@", ""), 
                    GetMinimumPackageVersion(d.Value.ToString()), "")));
            }
            if (dev_dependencies != null)
            {
                packages.AddRange(dev_dependencies.Properties().Select(d => new Package("npm", d.Name.Replace("@", ""),
                    GetMinimumPackageVersion(d.Value.ToString()), "")));
            }
            if (peer_dependencies != null)
            {
                packages.AddRange(peer_dependencies.Properties().Select(d => new Package("npm", d.Name.Replace("@", ""),
                    GetMinimumPackageVersion(d.Value.ToString()), "")));
            }
            if (optional_dependencies != null)
            {
                packages.AddRange(optional_dependencies.Properties().Select(d => new Package("npm", d.Name.Replace("@", ""),
                    GetMinimumPackageVersion(d.Value.ToString()), "")));
            }
            if (bundled_dependencies != null)
            {
                packages.AddRange(bundled_dependencies.Properties().Select(d => new Package("npm", d.Name.Replace("@", ""),
                    GetMinimumPackageVersion(d.Value.ToString()), "")));
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

        #region Properties
        public string PackageManagerLockFile {get; set;}

        public string DefaultPackageManagerLockFile {get; } = "yarn.lock";
        #endregion
        
        #region Methods
        internal static string GetMinimumPackageVersion(string version)
        {
            var c = version[0];
            if (Char.IsDigit(c))
            {
                return version;
            }
            else if (c == '~' || c == '^')
            {
                return version.Remove(0, 1);
            }
            else
            {
                return version;
            }

        }
        #endregion
    }
}

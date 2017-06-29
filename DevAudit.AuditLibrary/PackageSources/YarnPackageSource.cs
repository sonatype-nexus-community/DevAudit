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
    public class YarnPackageSource : PackageSource, IVulnerableCredentialStore
    {
        #region Constructors
        public YarnPackageSource(Dictionary<string, object> package_source_options, EventHandler<EnvironmentEventArgs> message_handler = null) : base(package_source_options, message_handler)
        {

        }
        #endregion

        #region Overriden properties
        public override string PackageManagerId { get { return "yarn"; } }

        public override string PackageManagerLabel { get { return "Yarn"; } }

        public override string DefaultPackageManagerConfigurationFile { get { return "package.json"; } }
        #endregion

        #region Overriden methods
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
                packages.AddRange(dependencies.Properties().Select(d => new Package("npm", d.Name, d.Value.ToString(), "")));
            }
            if (dev_dependencies != null)
            {
                packages.AddRange(dev_dependencies.Properties().Select(d => new Package("npm", d.Name, d.Value.ToString(), "")));
            }
            if (peer_dependencies != null)
            {
                packages.AddRange(peer_dependencies.Properties().Select(d => new Package("npm", d.Name, d.Value.ToString(), "")));
            }
            if (optional_dependencies != null)
            {
                packages.AddRange(optional_dependencies.Properties().Select(d => new Package("npm", d.Name, d.Value.ToString(), "")));
            }
            if (bundled_dependencies != null)
            {
                packages.AddRange(bundled_dependencies.Properties().Select(d => new Package("npm", d.Name, d.Value.ToString(), "")));
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

        #region Methods
        public List<VulnerableCredentialStorage> GetVulnerableCredentialStorage()
        {
            AuditFileInfo config_file = this.AuditEnvironment.ConstructFile(this.PackageManagerConfigurationFile);
            JSONConfig json_config = new JSONConfig(config_file);
            if (!json_config.ParseSucceded)
            {
                this.AuditEnvironment.Error("Could not parse JSON from {0}.", json_config.FullFilePath);
                if (json_config.LastParseException != null) this.AuditEnvironment.Error(json_config.LastParseException);
                if (json_config.LastIOException != null) this.AuditEnvironment.Error(json_config.LastIOException);
                return null;
            }
            this.AuditEnvironment.Status("Scanning for git credential storage candidates in {0}", config_file.FullName);
            IEnumerable<XElement> candidate_elements =
                from e in json_config.XmlConfiguration.Root.Descendants()
                    //where e.Value.Trim().StartsWith("git+https") || e.Value.Trim().StartsWith("git") || e.Value.Trim().StartsWith("https")
                where e.Elements().Count() == 0 && Utility.CalculateEntropy(e.Value) > 4.5
                select e;
            if (candidate_elements != null && candidate_elements.Count() > 0)
            {
                this.CredentialStorageCandidates = new List<VulnerableCredentialStorage>();
                foreach (XElement e in candidate_elements)
                {
                    this.CredentialStorageCandidates.Add(new VulnerableCredentialStorage
                    {
                        File = config_file.FullName,
                        Contents = json_config,
                        Location = e.AncestorsAndSelf().Reverse().Select(a => a.Name.LocalName).Aggregate((a1, a2) => a1 + "/" + a2)
                            .Replace("Container/", string.Empty),
                        Entropy = Utility.CalculateEntropy(e.Value),
                        Value = e.Value
                    });
                }
                this.AuditEnvironment.Success("Found {0} credential storage candidates.", this.CredentialStorageCandidates.Count);
                return this.CredentialStorageCandidates;
            }
            else
            {
                this.AuditEnvironment.Info("No credential storage candidates found.");
                return null;
            }

            #endregion

        }
    }
}

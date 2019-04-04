using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Sprache;
using Versatile;

using Alpheus;

namespace DevAudit.AuditLibrary
{
    public class YarnPackageSource : PackageSource, IDeveloperPackageSource
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
                packages.AddRange(dependencies.Properties()
                    .SelectMany(d => GetDeveloperPackages(d.Name.Replace("@", ""), d.Value.ToString())));
                //packages.AddRange(dependencies.Properties().Select(d => new Package("npm", d.Name.Replace("@", ""), 
                //    GetMinimumPackageVersion(d.Value.ToString()), "")));
            }
            if (dev_dependencies != null)
            {
                packages.AddRange(dev_dependencies.Properties()
                    .SelectMany(d => GetDeveloperPackages(d.Name.Replace("@", ""), d.Value.ToString())));

                //packages.AddRange(dev_dependencies.Properties().Select(d => new Package("npm", d.Name.Replace("@", ""),
                //    GetMinimumPackageVersion(d.Value.ToString()), "")));
            }
            if (peer_dependencies != null)
            {
                packages.AddRange(peer_dependencies.Properties()
                    .SelectMany(d => GetDeveloperPackages(d.Name.Replace("@", ""), d.Value.ToString())));

                //packages.AddRange(peer_dependencies.Properties().Select(d => new Package("npm", d.Name.Replace("@", ""),
                //    GetMinimumPackageVersion(d.Value.ToString()), "")));
            }
            if (optional_dependencies != null)
            {
                packages.AddRange(optional_dependencies.Properties()
                    .SelectMany(d => GetDeveloperPackages(d.Name.Replace("@", ""), d.Value.ToString())));

                //packages.AddRange(optional_dependencies.Properties().Select(d => new Package("npm", d.Name.Replace("@", ""),
                //    GetMinimumPackageVersion(d.Value.ToString()), "")));
            }
            if (bundled_dependencies != null)
            {
                packages.AddRange(bundled_dependencies.Properties()
                    .SelectMany(d => GetDeveloperPackages(d.Name.Replace("@", ""), d.Value.ToString())));
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
        public string PackageSourceLockFile {get; set;}

        public string DefaultPackageSourceLockFile {get; } = "yarn.lock";
        #endregion
        
        #region Methods
        public bool PackageVersionIsRange(string version)
        {
            var lcs = SemanticVersion.Grammar.Range.Parse(version);
            if (lcs.Count > 1) 
            {
                return true;
            }
            else if (lcs.Count == 1)
            {
                var cs = lcs.Single();
                if (cs.Count == 1 && cs.Single().Operator == ExpressionType.Equal)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else throw new ArgumentException($"Failed to parser {version} as a Yarn version.");
        }
        
        public List<string> GetMinimumPackageVersions(string version)
        {
            if (version == "*")
            {
                this.AuditEnvironment.Info("Using {0} package version {1} which satisfies range {2}.", 
                            this.PackageManagerLabel, "0.1", version);
                return new List<string>(1) {"0.1"};
            }
            var lcs = SemanticVersion.Grammar.Range.Parse(version);
            List<string> minVersions = new List<string>();
            foreach(ComparatorSet<SemanticVersion> cs in lcs)
            {
                if (cs.Count == 1 && cs.Single().Operator == ExpressionType.Equal)
                {
                    minVersions.Add(cs.Single().Version.ToNormalizedString());
                }
                else
                {
                    var gt = cs.Where(c => c.Operator == ExpressionType.GreaterThan || c.Operator == ExpressionType.GreaterThanOrEqual).Single();
                    if (gt.Operator == ExpressionType.GreaterThan)
                    {
                        var v = gt.Version;
                        minVersions.Add((v++).ToNormalizedString());
                        this.AuditEnvironment.Info("Using {0} package version {1} which satisfies range {2}.", 
                            this.PackageManagerLabel, (v++).ToNormalizedString(), version);
                    }
                    else
                    {
                        minVersions.Add(gt.Version.ToNormalizedString());
                        this.AuditEnvironment.Debug("Using {0} package version {1} which satisfies range {2}.", 
                            this.PackageManagerLabel, gt.Version.ToNormalizedString(), version);

                    }
                }
            }
            return minVersions;
        }

        public List<Package> GetDeveloperPackages(string name, string version, string vendor = null, string group = null,
            string architecture = null)
        {
            return GetMinimumPackageVersions(version).Select(v => new Package(PackageManagerId, name, v, vendor, group, architecture)).ToList();
        }
        #endregion
    }
}

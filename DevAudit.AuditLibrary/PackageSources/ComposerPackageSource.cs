using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Win32;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Sprache;
using Versatile;


namespace DevAudit.AuditLibrary
{
    public class ComposerPackageSource : PackageSource, IDeveloperPackageSource
    {
        #region Constructors
        public ComposerPackageSource(Dictionary<string, object> package_source_options, EventHandler<EnvironmentEventArgs> message_handler = null) : base(package_source_options, message_handler)
        {            
           
        }
        #endregion

        #region Overriden members
        public override string PackageManagerId { get { return "composer"; } }

        public override string PackageManagerLabel { get { return "Composer"; } }

        public override string DefaultPackageManagerConfigurationFile { get { return "composer.json"; } }
        
        public override IEnumerable<Package> GetPackages(params string[] o)
        {
            AuditFileInfo config_file = this.AuditEnvironment.ConstructFile(this.PackageManagerConfigurationFile);
            JObject json = (JObject)JToken.Parse(config_file.ReadAsText());
            JObject require = (JObject)json["require"];
            JObject require_dev = (JObject)json["require-dev"];
            if (require != null)
            {
                if (require_dev != null)
                {
                    return 
                        require.Properties()
                        .SelectMany(d => GetDeveloperPackages(d.Name.Split('/').Last(), d.Value.ToString(), "", d.Name.Split('/').First()))
                        .Concat(require_dev.Properties().SelectMany(d => GetDeveloperPackages(d.Name.Split('/').Last(), d.Value.ToString(), "", d.Name.Split('/').First())));
                }
                else
                {
                    return 
                        require.Properties()
                        .SelectMany(d => GetDeveloperPackages(d.Name.Split('/').Last(), d.Value.ToString(), "", d.Name.Split('/').First()));
                }
            }
            else if (require_dev != null)
            {
                return require_dev.Properties()
                .SelectMany(d => GetDeveloperPackages(d.Name.Split('/').Last(), d.Value.ToString(), "", d.Name.Split('/').First()));
            }
            else
            {
                this.AuditEnvironment.Warning("{0} file does not contain a require or require_dev element.", config_file.FullName);
                return new List<Package>();
            }
        }

        public override bool IsVulnerabilityVersionInPackageVersionRange(string vulnerability_version, string package_version)
        {
            string message = "";
            bool r = Composer.RangeIntersect(vulnerability_version, package_version, out message);
            if (!r && !string.IsNullOrEmpty(message))
            {
                throw new Exception(message);
            }
            else return r;

        }
        #endregion

        #region Properties
        public string PackageSourceLockFile {get; set;}

        public string DefaultPackageSourceLockFile {get;} = "composer.lock";
        #endregion

        #region Methods
        public bool PackageVersionIsRange(string version)
        {
            var lcs = Composer.Grammar.Range.Parse(version);
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
            else throw new ArgumentException($"Failed to parser {version} as a Composer version.");
        }

        public List<string> GetMinimumPackageVersions(string version)
        {
            var lcs = Composer.Grammar.Range.Parse(version);
            List<string> minVersions = new List<string>();
            foreach(ComparatorSet<Composer> cs in lcs)
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
                    }
                    else
                    {
                        minVersions.Add(gt.Version.ToNormalizedString());
                    }
                }

            }
            return minVersions;
        }

        public List<Package> GetDeveloperPackages(string name, string version, string vendor = null, string group = null, 
            string architecture=null)  
        {
            return GetMinimumPackageVersions(version).Select(v => new Package(PackageManagerId, name, v, vendor, group,
                architecture)).ToList();
        }
        #endregion
    }
}

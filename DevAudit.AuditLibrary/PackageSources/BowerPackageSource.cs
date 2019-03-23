using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Sprache;
using Versatile;

namespace DevAudit.AuditLibrary
{
    public class BowerPackageSource : PackageSource, IDeveloperPackageManager
    {
        #region Constructors
        public BowerPackageSource(Dictionary<string, object> package_source_options, EventHandler<EnvironmentEventArgs> message_handler = null) : base(package_source_options, message_handler)
        {
        }        
        #endregion

        #region Overriden properties
        public override string PackageManagerId { get { return "bower"; } }

        public override string PackageManagerLabel { get { return "Bower"; } }

        public override string DefaultPackageManagerConfigurationFile { get { return "bower.json"; } }
        #endregion

        #region Overriden methods
        //Get bower packages from reading bower.json
        public override IEnumerable<Package> GetPackages(params string[] o)
        {
            var packages = new List<Package>();

            AuditFileInfo config_file = this.AuditEnvironment.ConstructFile(this.PackageManagerConfigurationFile);
            JObject json = (JObject)JToken.Parse(config_file.ReadAsText());

            packages.Add(
                new Package("bower", 
                    json.Properties().First(j => j.Name == "name").Value.ToString(),
                    json.Properties().First(j => j.Name == "version").Value.ToString(),
                    "")
            );

            JObject dependencies = (JObject)json["dependencies"];
            JObject dev_dependencies = (JObject)json["devDependencies"];

            if (dev_dependencies != null)
            {
                packages.AddRange(dev_dependencies.Properties().Select(d => new Package("bower", d.Name, d.Value.ToString(), "")));
            }

            if (dependencies != null)
            {
                packages.AddRange(dependencies.Properties().Select(d => new Package("bower", d.Name, d.Value.ToString(), "")));
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
        public string DefaultPackageManagerLockFile {get; } = "";

        public string PackageManagerLockFile {get; set;}
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
            else throw new ArgumentException($"Failed to parser {version} as a version.");
        }
        #endregion
    }
}

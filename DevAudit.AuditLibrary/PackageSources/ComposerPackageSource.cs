using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Win32;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Versatile;

namespace DevAudit.AuditLibrary
{
    public class ComposerPackageSource : PackageSource, IDeveloperPackageManager
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
        
        //Get  packages from reading composer.json
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
                    return require.Properties().Select(d => new Package("composer", d.Name.Split('/').Last(), d.Value.ToString(), "", d.Name.Split('/').First()))
                        .Concat(require_dev.Properties().Select(d => new Package("composer", d.Name.Split('/').Last(), d.Value.ToString(), "", d.Name.Split('/').First())));
                }
                else
                {
                    return require.Properties().Select(d => new Package("composer", d.Name.Split('/').Last(), d.Value.ToString(), "", d.Name.Split('/').First()));
                }
            }
            else if (require_dev != null)
            {
                return require_dev.Properties().Select(d => new Package("composer", d.Name.Split('/').Last(), d.Value.ToString(), "", d.Name.Split('/').First()));
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
        public string PackageManagerLockFile {get; set;}

        public string DefaultPackageManagerLockFile {get;} = "composer.lock";
        #endregion
    }
}

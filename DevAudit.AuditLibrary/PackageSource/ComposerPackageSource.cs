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
                return require.Properties().Select(d => new OSSIndexQueryObject("composer", d.Name.Split('/').First(), d.Value.ToString(), "", d.Name.Split('/').Last()))
                    .Concat(require_dev.Properties().Select(d => new OSSIndexQueryObject("composer", d.Name.Split('/').First(), d.Value.ToString(), "", d.Name.Split('/').Last())));
            }
        }

        public override bool IsVulnerabilityVersionInPackageVersionRange(string vulnerability_version, string package_version)
        {
            return SemanticVersion.RangeIntersect(vulnerability_version, package_version);
        }

        public override Func<List<OSSIndexArtifact>, List<OSSIndexArtifact>> ArtifactsTransform { get; } = (artifacts) =>
        {            
            List<OSSIndexArtifact> o = artifacts.ToList();
            foreach (OSSIndexArtifact a in o)
            {                
                if (a.Search == null || a.Search.Count() != 4)
                {
                    throw new Exception("Did not receive expected Search field properties for artifact name: " + a.PackageName + " id: " +
                        a.PackageId + " project id: " + a.ProjectId + ".");
                }
                else
                {
                    OSSIndexQueryObject package = new OSSIndexQueryObject(a.Search[0], a.Search[1], a.Search[3], "");
                    a.Package = package;
                }
            }
            return o;
        };

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

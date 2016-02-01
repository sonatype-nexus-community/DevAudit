using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Microsoft.Win32;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using SemverSharp;


namespace WinAudit.AuditLibrary
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
                return dependencies.Properties().Select(d => new OSSIndexQueryObject("bower", d.Name, d.Value.ToString(), ""));
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
                a.GetProjectId = () =>
                {
                    string project_id = a.ProjectId;
                    return project_id;
                };
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

        /*
        public override Func<string, string, bool> PackageVersionInRange { get; } = (range, compare_to_range) =>
        {
            Regex parse_ex = new Regex(@"^(?<range>~+|<+=?|>+=?)?" +
                @"(?<ver>(\d+)" +
                @"(\.(\d+))?" +
                @"(\.(\d+))?" +
                @"(\-([0-9A-Za-z\-\.]+))?" +
                @"(\+([0-9A-Za-z\-\.]+))?)$", RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.ExplicitCapture);
            Match m = parse_ex.Match(range);
            if (!m.Success)
            {
                throw new ArgumentException("Could not parse package range: " + range + ".");
            }
            string range_version_str = "";
            string op = "";
            string[] legal_ops = { "", "~", "<", "<=", ">=", ">" };
            SemVersion range_version = null;
            SemVersion compare_to_range_version = null;
            op = m.Groups[1].Value;
            if (!legal_ops.Contains(op))
                throw new ArgumentException("Could not parse package version range operator: " + op + ".");
            range_version_str = m.Groups[2].Value;
            if (!SemVersion.TryParse(m.Groups[2].Value, out range_version))
            {
                throw new ArgumentException("Could not parse package semantic version: " + m.Groups[2].Value + ".");
            }
            if (!SemVersion.TryParse(compare_to_range, out compare_to_range_version))
            {
                throw new ArgumentException("Could not parse comparing semantic version: " + compare_to_range + ".");
            }
            switch (op)
            {
                case "":
                    return compare_to_range_version == range_version;
                case "<":
                    return compare_to_range_version < range_version;
                case "<=":
                    return compare_to_range_version <= range_version;
                case ">":
                    return compare_to_range_version > range_version;
                case ">=":
                    return compare_to_range_version >= range_version;
                //case "~":
                //if (range_version.Major != compare_to_range_version.Major) return false;
                //major matches
                //else if (range_version.Minor == 0 && compare_to_range_version > 0) return true;

                //else if (range_version.Minor == 0 && range_version.Patch == 0 && compare_to_range_version.Patch > 0) return true;
                //else if (range_version.Patch == )
                default:
                    throw new Exception("Unimplemented range operator: " + op + ".");
            }
        };
        */
        public BowerPackageSource() : base() {}
        public BowerPackageSource(Dictionary<string, object> package_source_options) : base(package_source_options)
        {
            if (string.IsNullOrEmpty(this.PackageManagerConfigurationFile))
            {
                this.PackageManagerConfigurationFile = @".\bower.json";
            }
        }
    }
}

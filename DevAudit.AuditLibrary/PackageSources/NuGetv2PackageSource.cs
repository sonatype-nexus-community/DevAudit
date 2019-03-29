using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Xml;
using System.Xml.Linq;

using Sprache;
using Versatile;

namespace DevAudit.AuditLibrary
{
    public class NuGetv2PackageSource : PackageSource, IDeveloperPackageSource
    {
        #region Constructors
        public NuGetv2PackageSource(Dictionary<string, object> package_source_options, EventHandler<EnvironmentEventArgs> message_handler = null) : base(package_source_options, message_handler)
        {
              
        }
        #endregion
        
        #region Overriden members
        public override string PackageManagerId { get { return "nuget"; } }

        public override string PackageManagerLabel { get { return "NuGet"; } }

        public override string DefaultPackageManagerConfigurationFile { get { return "packages.config"; } }
        
        public override IEnumerable<Package> GetPackages(params string[] o) ////Get NuGet packages from reading packages.config
        {
            AuditFileInfo configfile = this.AuditEnvironment.ConstructFile(this.PackageManagerConfigurationFile);
            string _byteOrderMarkUtf8 = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble());
            string xml = configfile.ReadAsText();
            if (xml.StartsWith(_byteOrderMarkUtf8, StringComparison.Ordinal))
            {
                var lastIndexOfUtf8 = _byteOrderMarkUtf8.Length;
                xml = xml.Remove(0, lastIndexOfUtf8);
            }
            XElement root = XElement.Parse(xml);
            
            IEnumerable<Package> packages;

            if (root.Name == "Project")
            {
                // dotnet core csproj file
                packages = 
                    root
                    .Descendants()
                    .Where(x => x.Name == "PackageReference")
                    .SelectMany(r => GetDeveloperPackages(r.Attribute("Include").Value, r.Attribute("Version").Value)).ToList();
            }
            else
            {
                packages =
                        root
                        .Elements("package")
                        .SelectMany(el => GetDeveloperPackages(el.Attribute("id").Value, el.Attribute("version").Value));
            }
            return packages;
        }

        public override bool IsVulnerabilityVersionInPackageVersionRange(string vulnerability_version, string package_version)
        {
            string message = "";
            bool r = NuGetv2.RangeIntersect(vulnerability_version, package_version, out message);
            if (!r && !string.IsNullOrEmpty(message))
            {
                throw new Exception(message);
            }
            else return r;           
        }
        #endregion

        #region Properties
        public string PackageSourceLockFile { get; set; }

        public string DefaultPackageSourceLockFile {get; } = null;
        #endregion
    
        #region Methods
        public bool PackageVersionIsRange(string version)
        {
            var lcs = NuGetv2.Grammar.Range.Parse(version);
            if (lcs.Count > 1) 
            {
                return true;
            }
            else if (lcs.Count == 1)
            {
                var cs = lcs.Single();
                if (cs.Operator == ExpressionType.Equal)
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

        public List<string> GetMinimumPackageVersions(string version)
        {
            var cs = NuGetv2.Grammar.Range.Parse(version);
            List<string> minVersions = new List<string>();
            
            if (cs.Count == 1 && cs.Single().Operator == ExpressionType.Equal)
            {
                minVersions.Add(cs.Single().Version.ToNormalizedString());
            }
            else
            {
                var gt = cs.Where(c => c.Operator == ExpressionType.GreaterThan || c.Operator == ExpressionType.GreaterThanOrEqual).Single();
                if (gt.Operator == ExpressionType.GreaterThan)
                {
                    var v = new NuGetv2(gt.Version.Version.Major, gt.Version.Version.Minor, gt.Version.Version.Revision + 1, gt.Version.Version.Build);
                    minVersions.Add((v).ToNormalizedString());
                    this.AuditEnvironment.Info("Using {0} package version {1} which satisfies range {2}.", 
                        this.PackageManagerLabel, v.ToNormalizedString(), version);

                }
                else
                {
                    minVersions.Add(gt.Version.ToNormalizedString());
                    this.AuditEnvironment.Info("Using {0} package version {1} which satisfies range {2}.", 
                        this.PackageManagerLabel, gt.Version.ToNormalizedString(), version);

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

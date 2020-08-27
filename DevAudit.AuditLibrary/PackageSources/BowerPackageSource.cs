﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Sprache;
using Versatile;

namespace DevAudit.AuditLibrary
{
    public class BowerPackageSource : PackageSource, IDeveloperPackageSource
    {
        #region Constructors

        string _LocalBowerFolder;
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
            if (json.Properties().Any(j => j.Name == "name") && json.Properties().Any(j => j.Name == "version"))
            {
                packages.AddRange(GetDeveloperPackages(
                    json.Properties().First(j => j.Name == "name").Value.ToString(),
                    json.Properties().First(j => j.Name == "version").Value.ToString()));
            }
            JObject dependencies = (JObject)json["dependencies"];
            JObject dev_dependencies = (JObject)json["devDependencies"];

            if (dev_dependencies != null)
            {
                packages.AddRange(dev_dependencies.Properties().SelectMany(d => GetDeveloperPackages(d.Name, d.Value.ToString())));
            }

            if (dependencies != null)
            {
                packages.AddRange(dependencies.Properties().SelectMany(d => GetDeveloperPackages(d.Name, d.Value.ToString())));
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
        public string DefaultPackageSourceLockFile {get; } = "";

        public string PackageSourceLockFile {get; set;}
        #endregion

        #region Methods
        public string GetLocalVersion(string productName)
        {
            if(string.IsNullOrEmpty(_LocalBowerFolder))
            {
                var folder = new FileInfo(this.PackageManagerConfigurationFile);
                string settingsFile = Path.Combine(folder.Directory.FullName, ".bowerrc");                

                dynamic jsonSettings = JsonConvert.DeserializeObject(File.ReadAllText(settingsFile));
                _LocalBowerFolder = System.IO.Path.Combine(folder.Directory.FullName, jsonSettings.directory.ToString());
            }

            string tmpProductJson = System.IO.Path.Combine(_LocalBowerFolder, productName, ".bower.json");
            if (!File.Exists(tmpProductJson))
                return "unkown";

            dynamic productJson = JsonConvert.DeserializeObject(File.ReadAllText(tmpProductJson));

            return productJson.version;
        }

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

        public List<string> GetMinimumPackageVersions(string version)
        {
            if (version == "unkown")
                return new List<string>();

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
                        this.AuditEnvironment.Info("Using {0} package version {1} which satisfies range {2}.", 
                            this.PackageManagerLabel, gt.Version.ToNormalizedString(), version);

                    }
                }

            }
            return minVersions;
        }

        public List<Package> GetDeveloperPackages(string name, string version, string vendor = null, string group = null,string architecture = null)  
        {
            if(version.ToLower() == "latest")
            {
                version = GetLocalVersion(name);
            }

            return GetMinimumPackageVersions(version).Select(v => new Package(PackageManagerId, name, v, vendor, group, architecture)).ToList();
        }
        #endregion
    }
}

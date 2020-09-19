using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NuGet;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Packaging.Signing;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.DependencyResolver;
using NuGet.Versioning;

using NuGet.Repositories;
using Sprache;
using Versatile;
using System.Text.RegularExpressions;

namespace DevAudit.AuditLibrary
{
    public class NetCorePackageSource : PackageSource, IDeveloperPackageSource
    {
        #region Constructors
        public NetCorePackageSource(Dictionary<string, object> package_source_options, 
            EventHandler<EnvironmentEventArgs> message_handler = null) : base(package_source_options, message_handler)
        {}
        #endregion

        #region Overriden members
        public override string PackageManagerId { get { return "nuget"; } }

        public override string PackageManagerLabel { get { return ".NET Core"; } }

        public override string DefaultPackageManagerConfigurationFile { get { return string.Empty; } }
        public override IEnumerable<Package> GetPackages(params string[] o)
        {
            AuditFileInfo config_file = this.AuditEnvironment.ConstructFile(this.PackageManagerConfigurationFile);
            List<Package> packages = new List<Package>();


            var isSnlSolution = config_file.Name.EndsWith(".sln");
            var isCsProj = config_file.Name.EndsWith(".csproj");
            var isBuildTargets = config_file.Name.EndsWith(".Build.targets");
            var isDepsJson = config_file.Name.EndsWith(".deps.json");
            
            if(isSnlSolution)
            {
                var Content = File.ReadAllText(this.PackageManagerConfigurationFile);
                Regex projReg = new Regex("Project\\(\"\\{[\\w-]*\\}\"\\) = \"([\\w _]*.*)\", \"(.*\\.(cs|vcx|vb)proj)\"", RegexOptions.Compiled);
                var matches = projReg.Matches(Content).Cast<Match>();
                var Projects = matches.Select(x => x.Groups[2].Value).ToList();
                for (int i = 0; i < Projects.Count; ++i)
                {
                    if (!Path.IsPathRooted(Projects[i]))
                        Projects[i] = Path.Combine(Path.GetDirectoryName(this.PackageManagerConfigurationFile),
                            Projects[i]);
                    Projects[i] = Path.GetFullPath(Projects[i]);
                }

                foreach(var project in Projects)
                {
                    var tmpProject = GetPackagesFromProjectFile(project, true);

                    if(tmpProject.Count != 0)
                    {
                        this.AuditEnvironment.Info($"Found {tmpProject.Count} packages in {project}");

                        foreach(var tmpPacket in tmpProject)
                        {
                            if(!packages.Contains(tmpPacket))
                                packages.Add(tmpPacket);
                        }
                    }
                }
                return packages;
            }

			if (isCsProj || isBuildTargets)
			{
                return GetPackagesFromProjectFile(this.PackageManagerConfigurationFile, isCsProj);
            }
            if (isDepsJson)
            {
                try
                {
                    this.AuditEnvironment.Info("Reading packages from .NET Core dependencies manifest..");
                    JObject json = (JObject)JToken.Parse(config_file.ReadAsText());
                    JObject libraries = (JObject)json["libraries"];

                    if (libraries != null)
                    {
                        foreach (JProperty p in libraries.Properties())
                        {
    
                            string[] name = p.Name.Split('/');
							// Packages with version 0.0.0.0 can show up if the are part of .net framework.
							// Checking this version number is quite useless and might give a false positive.
                            if (name[1] != "0.0.0.0")
                            {
	                            packages.Add(new Package("nuget", name[0], name[1]));

                            }
                        }
                    }
                    return packages;
                }
                catch (Exception e)
                {
                    this.AuditEnvironment.Error(e, "Error reading .NET Core dependencies manifest {0}.", config_file.FullName);
                    return packages;
                }
            }

            this.AuditEnvironment.Error("Unknown .NET Core project file type: {0}.", config_file.FullName);
            return packages;

        }

        private List<Package> GetPackagesFromProjectFile(string filename, bool isCsProj)
        {
            List<Package> packages = new List<Package>();
            AuditFileInfo config_file = this.AuditEnvironment.ConstructFile(filename);

            var fileType = isCsProj ? ".csproj" : "build targets";
            this.AuditEnvironment.Info($"Reading packages from .NET Core C# {fileType} file.");
            string _byteOrderMarkUtf8 = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble());
            string xml = config_file.ReadAsText();
            if (xml.StartsWith(_byteOrderMarkUtf8, StringComparison.Ordinal))
            {
                var lastIndexOfUtf8 = _byteOrderMarkUtf8.Length;
                xml = xml.Remove(0, lastIndexOfUtf8);
            }
            XElement root = XElement.Parse(xml);

            var package = isCsProj ? "Include" : "Update";

            if (root.Name.LocalName == "Project")
            {
                packages =
                    root
                    .Descendants()
                    .Where(x => x.Name.LocalName == "PackageReference" && x.Attribute(package) != null && x.Attribute("Version") != null)
                    .SelectMany(r => GetDeveloperPackages(r.Attribute(package).Value, r.Attribute("Version").Value))
                    .ToList();

                IEnumerable<string> skipped_packages =
                    root
                    .Descendants()
                    .Where(x => x.Name.LocalName == "PackageReference" && x.Attribute(package) != null && x.Attribute("Version") == null)
                    .Select(r => r.Attribute(package).Value);

                if (skipped_packages.Count() > 0)
                {
                    this.AuditEnvironment.Warning("{0} package(s) do not have a version specified and will not be audited: {1}.", skipped_packages.Count(),
                    skipped_packages.Aggregate((s1, s2) => s1 + "," + s2));
                }
                var helper = new NuGetApiHelper(this.AuditEnvironment, config_file.DirectoryName);
                var nuGetFrameworks = helper.GetFrameworks(root);

                if (!nuGetFrameworks.Any())
                {
                    AuditEnvironment.Warning("Scanning from project file found 0 packages, checking for packages.config file. ");
                    nuGetFrameworks = helper.GetFrameworks();
                }

                if (!nuGetFrameworks.Any())
                {
                    
                    AuditEnvironment.Warning("Scanning NuGet transitive dependencies failed because no target framework is found in {0}...", config_file.Name);
                }

                foreach (var framework in nuGetFrameworks)
                {
                    AuditEnvironment.Info("Scanning NuGet transitive dependencies for {0}...", framework.GetFrameworkString());
                    var deps = helper.GetPackageDependencies(packages, framework);
                    Task.WaitAll(deps);
                    packages = helper.AddPackageDependencies(deps.Result, packages);
                }
                return packages;
            }
            else
            {
                this.AuditEnvironment.Error("{0} is not a .NET Core format .csproj file.", config_file.FullName);
                return packages;
            }
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
        public string DefaultPackageSourceLockFile {get; } = "";

        public string PackageSourceLockFile {get; set;}
        #endregion

        #region Methods
        public bool PackageVersionIsRange(string version)
        {
            var lcs = Versatile.SemanticVersion.Grammar.Range.Parse(version);
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
            if (version.StartsWith("["))
                version = version.Remove(0, 1);

            if (version.EndsWith("]"))
                version = version.Remove(version.Length -1, 1);

            var lcs = Versatile.SemanticVersion.Grammar.Range.Parse(version);
            List<string> minVersions = new List<string>();
            foreach(ComparatorSet<Versatile.SemanticVersion> cs in lcs)
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

        public List<Package> GetDeveloperPackages(string name, string version, string vendor = null, string group = null, 
            string architecture=null)  
        {
            return GetMinimumPackageVersions(version).Select(v => new Package(PackageManagerId, name, v, vendor, group,
                architecture)).ToList();
        }

        public async Task GetDeps(XElement project, List<Package> packages)
        {
	        IEnumerable<NuGetFramework> frameworks =
		        project.Descendants()
			        .Where(x => x.Name.LocalName == "TargetFramework" || x.Name.LocalName == "TargetFrameworks")
			        .SingleOrDefault()
			        .Value.Split(';')
			        .Select(f => NuGetFramework.ParseFolder(f));
	        AuditEnvironment.Info("{0}", frameworks.First().Framework);
	        var nugetPackages = packages.Select(p => new PackageIdentity(p.Name, NuGetVersion.Parse(p.Version)));
	        var settings = Settings.LoadDefaultSettings(root: null);
	        var sourceRepositoryProvider = new SourceRepositoryProvider(settings, Repository.Provider.GetCoreV3());
	        var logger = NullLogger.Instance;
	        using (var cacheContext = new SourceCacheContext())
	        {
		        foreach (var np in nugetPackages)
		        {
			        foreach (var sourceRepository in sourceRepositoryProvider.GetRepositories())
			        {
				        var dependencyInfoResource = await sourceRepository.GetResourceAsync<DependencyInfoResource>();
				        var dependencyInfo = await dependencyInfoResource.ResolvePackage(
					        np, frameworks.First(), cacheContext, logger, CancellationToken.None);

				        if (dependencyInfo != null)
				        {
					        AuditEnvironment.Info("Dependency info: {0}.", dependencyInfo);
				        }
			        }
		        }
	        }

        }

		#endregion

	}
}

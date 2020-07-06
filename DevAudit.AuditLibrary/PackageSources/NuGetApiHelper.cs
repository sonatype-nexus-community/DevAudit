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
using NuGet.ProjectManagement;
using NuGet.ProjectModel;
using NuGet.Packaging.Core;
using NuGet.Packaging.Signing;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.DependencyResolver;
using NuGet.Versioning;
using System.Security.Cryptography;

namespace DevAudit.AuditLibrary
{
    public class NuGetApiHelper
    {
        string _projRoot;

        public NuGetApiHelper(AuditEnvironment env, string projRoot)
        {
            Environment = env;
            _projRoot = projRoot;
        }
        public AuditEnvironment Environment { get; }

        public IEnumerable<NuGetFramework> GetFrameworks()
        {
            string xmlFile = System.IO.Path.Combine(_projRoot, "packages.config");
            if(!System.IO.File.Exists(xmlFile))
                return new List<NuGetFramework>();

            string _byteOrderMarkUtf8 = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble());
            string xml = System.IO.File.ReadAllText(xmlFile);
            if (xml.StartsWith(_byteOrderMarkUtf8, StringComparison.Ordinal))
            {
                var lastIndexOfUtf8 = _byteOrderMarkUtf8.Length;
                xml = xml.Remove(0, lastIndexOfUtf8);
            }
            XElement root = XElement.Parse(xml);
                        
            var xmlDescandants = root.Descendants();
            var packages = new List<NuGetFramework>(xmlDescandants.Count());

            foreach(var xmlDes in xmlDescandants)
            {
                var attributes = xmlDes.Attributes();
                string packageID = null;
                string version = null;
                string targetFrameworks;


                foreach (var att in attributes)
                {
                    switch(att.Name.LocalName)
                    {
                        case "id":
                            packageID = att.Value;
                            break;

                        case "version":
                            version = att.Value;
                            break;

                        case "TargetFrameworks":
                        case "targetFramework":
                            targetFrameworks = att.Value;
                            break;
                    }
                }

                var package = new NuGetFramework(packageID, Version.Parse(version));
                packages.Add(package);
            }

            return packages;
        }

        public IEnumerable<NuGetFramework> GetFrameworks(XElement projectXml) =>
	        projectXml
		        .Descendants()
		        .SingleOrDefault(x => x.Name.LocalName == "TargetFramework" || x.Name.LocalName == "TargetFrameworks")
		        ?.Value.Split(';')
		        .Select(NuGetFramework.ParseFolder) ?? Enumerable.Empty<NuGetFramework>();

        public async Task<IEnumerable<Tuple<Package, PackageDependency>>> GetPackageDependencies(IEnumerable<Package> packages, NuGetFramework framework)
        {
            var settings = Settings.LoadDefaultSettings(root: null);
            var sourceRepositoryProvider = new SourceRepositoryProvider(settings, Repository.Provider.GetCoreV3());
            var logger = NullLogger.Instance;
            var results = new List<Tuple<Package, PackageDependency>>();
            using (var cacheContext = new SourceCacheContext())
            {
                foreach (var p in packages)
                {
                    PackageIdentity np = new PackageIdentity(p.Name, NuGetVersion.Parse(p.Version));
                    foreach (var sourceRepository in sourceRepositoryProvider.GetRepositories())
                    {
                        var dependencyInfoResource = await sourceRepository.GetResourceAsync<DependencyInfoResource>();
                        var dependencyInfo = await dependencyInfoResource.ResolvePackage(
                            np, framework, cacheContext, logger, CancellationToken.None);
                        if (dependencyInfo != null && dependencyInfo.Dependencies.Count() > 0)
                        {
                            results.AddRange(dependencyInfo.Dependencies.Select(d => new Tuple<Package, PackageDependency>(p, d)));
                            
                                foreach (var d in dependencyInfo.Dependencies.Take(10))
                                {
                                    var _trdependencyInfo = await dependencyInfoResource.ResolvePackage(
                                        new PackageIdentity(d.Id, d.VersionRange.MinVersion), framework, cacheContext, logger, CancellationToken.None);
                                    if (_trdependencyInfo != null && _trdependencyInfo.Dependencies.Count() > 0)
                                    {
                                        results.AddRange(_trdependencyInfo.Dependencies.Select(_d => new Tuple<Package, PackageDependency>(p, _d)));
                                    }
                                }
                            
                        }
                    }
                }
            }
            return results;
        }

        public List<Package> AddPackageDependencies(IEnumerable<Tuple<Package, PackageDependency>> deps, List<Package> packages)
        {
            if (packages.Count == 0)
                return packages;

            string pm = packages.First().PackageManager;
            string vendor = packages.First().Vendor;
            string group = packages.First().Group;
            string arch = packages.First().Architecture;
            int added = 0;
           
            foreach (var d in deps)
            {
                var dp = new Package(pm, d.Item2.Id, d.Item2.VersionRange.MinVersion.ToNormalizedString(),
                        d.Item1.PackageManager, d.Item1.Group, d.Item1.Architecture);
                if (packages.Any(p => p.PackageManager == dp.PackageManager && p.Name == dp.Name && p.Version == dp.Version ))
                {
                    continue;
                }
                else
                {
                    packages.Add(dp);
                    added++; ;
                }
            }
            Environment.Info("Added {0} NuGet transitive dependencies.", added);
            return packages;
        }
    }
}

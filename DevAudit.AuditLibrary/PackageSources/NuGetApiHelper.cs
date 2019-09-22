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

namespace DevAudit.AuditLibrary
{
    public class NuGetApiHelper
    {
        public NuGetApiHelper(AuditEnvironment env, string projRoot)
        {
            Environment = env;
           
        }
        public AuditEnvironment Environment { get; }
        public IEnumerable<NuGetFramework> GetFrameworks(XElement projectXml) =>
             projectXml.Descendants()
                .Where(x => x.Name.LocalName == "TargetFramework" || x.Name.LocalName == "TargetFrameworks")
                .SingleOrDefault()
                .Value.Split(';')
                .Select(f => NuGetFramework.ParseFolder(f));

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

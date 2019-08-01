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

namespace DevAudit.AuditLibrary
{
    public class NuGetApiHelper
    {
        public NuGetApiHelper(AuditEnvironment env)
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

        public async Task<IEnumerable<Tuple<Package, SourcePackageDependencyInfo>>> GetPackageDependencies(IEnumerable<Package> packages, NuGetFramework framework)
        {
            var settings = Settings.LoadDefaultSettings(root: null);
            var sourceRepositoryProvider = new SourceRepositoryProvider(settings, Repository.Provider.GetCoreV3());
            var logger = NullLogger.Instance;
            var results = new List<Tuple<Package, SourcePackageDependencyInfo>>();
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
                        if (dependencyInfo != null)
                        {
                            results.Add(new Tuple<Package, SourcePackageDependencyInfo>(p, dependencyInfo));
                        }
                    }
                }
            }
            return results;
        }

        public List<Package> AddPackageDependencies(IEnumerable<Tuple<Package, SourcePackageDependencyInfo>> deps, List<Package> packages)
        {
            string pm = packages.First().PackageManager;
            string vendor = packages.First().Vendor;
            string group = packages.First().Group;
            string arch = packages.First().Architecture;
           
            foreach (var d in deps)
            {
                if (packages.Contains(d.Item1))
                {
                    continue;
                }
                else
                {
                    packages.Add(new Package(pm, d.Item2.Id, d.Item2.Version.ToNormalizedString(), 
                        d.Item1.PackageManager, d.Item1.Group, d.Item1.Architecture));
                }
            }
            return packages;
        }
    }
}

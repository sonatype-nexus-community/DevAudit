using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;
using DevAudit.AuditLibrary;

namespace DevAudit.Tests
{
    public class NuGetPackageSourceTests : PackageSourceTests
    {
        protected override PackageSource s {get; } = new NuGetPackageSource(new Dictionary<string, object>()
        { {"File", @".\Examples\packages.config.example" } });

        [Fact]
        public override void CanComparePackageVersions()
        {
            Assert.True(s.IsVulnerabilityVersionInPackageVersionRange("<3.3.5.3", "3.3.5.2"));
            Assert.True(s.IsVulnerabilityVersionInPackageVersionRange("<4", "[3,4)"));
            Assert.True(s.IsVulnerabilityVersionInPackageVersionRange(">4.6", "[3.5,5.3.1.1)"));
            Assert.False(s.IsVulnerabilityVersionInPackageVersionRange(">5.4", "[3.5,5.3.1.1)"));
            Assert.True(s.IsVulnerabilityVersionInPackageVersionRange("7.6.3.2", "(5,7.6.3.2]"));
            Assert.True(s.IsVulnerabilityVersionInPackageVersionRange("<7.6", "(5,7.6.3.2]"));
            Assert.False(s.IsVulnerabilityVersionInPackageVersionRange("<5", "(5,7.6.3.2]"));
        }

        public override async Task CanGetProjects()
        {
            OSSIndexHttpClient http_client = new OSSIndexHttpClient("1.1");
            OSSIndexProject p1 = await http_client.GetProjectForIdAsync("8396831047");
            Assert.NotNull(p1);
            Assert.Equal(p1.Id, 8396831047);
            Assert.Equal(p1.Name, "Modernizr");
            Assert.Equal(p1.HasVulnerability, false);
        }

        
        public override async Task CanGetVulnerabilities()
        {
            OSSIndexHttpClient http_client = new OSSIndexHttpClient("1.1");
            List<OSSIndexProjectVulnerability> v1 = (await http_client.GetVulnerabilitiesForIdAsync("8396559329")).ToList();
            Assert.NotNull(v1);
            Assert.True(v1.Any(v => v.Uri == "cve:/CVE-2011-4969"));
        }

        public override void CanCacheProjectVulnerabilities()
        {
            string cache_file_name = @".\DevAudit-package-source-tests.cache";
            if (File.Exists(cache_file_name))
            {
                File.Delete(cache_file_name);
            }
            PackageSource cs =  new NuGetPackageSource(new Dictionary<string, object>()
            {
                { "File", @".\Examples\packages.config.example" },
                { "Cache", true },
                { "CacheFile", cache_file_name }
            });
            Assert.True(cs.ProjectVulnerabilitiesCacheEnabled);
            cs.PackagesTask.Wait();
            Assert.NotEmpty(cs.Packages);
            Task.WaitAll(cs.ArtifactsTask.ToArray());
            Assert.NotEmpty(cs.Artifacts);
            try
            {
                Task.WaitAll(cs.VulnerabilitiesTask.ToArray());
            }

            catch (AggregateException)
            {

            }

            Assert.True(cs.ProjectVulnerabilitiesCacheItems.Count() > 0);
            Assert.True(cs.CachedArtifacts.Count() > 0);
            cs.Dispose();
            cs = null;
            cs = new NuGetPackageSource(new Dictionary<string, object>()
            {
                { "File", @".\Examples\packages.config.example" },
                { "Cache", true },
                { "CacheFile", cache_file_name }
            });            
            cs.PackagesTask.Wait();
            Assert.NotEmpty(cs.Packages);
            Task.WaitAll(cs.ArtifactsTask.ToArray());
            Assert.NotEmpty(cs.Artifacts);
            Assert.True(cs.ProjectVulnerabilitiesCacheItems.Count() > 0);
            Assert.True(cs.CachedArtifacts.Count() > 0);
            cs.ProjectVulnerabilitiesCacheTTL = TimeSpan.FromSeconds(0);
            Assert.False(cs.ProjectVulnerabilitiesCacheItems.Count() > 0);
            Assert.False(cs.CachedArtifacts.Count() > 0);
        }

    }
}

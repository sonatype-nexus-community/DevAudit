using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;
using WinAudit.AuditLibrary;

namespace WinAudit.Tests
{
    public class NuGetPackageSourceTests : PackageSourceTests
    {
        protected override PackageSource s {get; } = new NuGetPackageSource();
        
        [Fact]
        public override async Task CanGetProjects()
        {
            OSSIndexHttpClient http_client = new OSSIndexHttpClient("1.0");
            OSSIndexProject p1 = await http_client.GetProjectForIdAsync("284089289");
            Assert.NotNull(p1);
            Assert.Equal(p1.Id, 284089289);
            Assert.Equal(p1.Name, "ajaxcontroltoolkit");
            Assert.Equal(p1.HasVulnerability, true);
            Assert.Equal(p1.Vulnerabilities, "https://ossindex.net/v1.0/scm/284089289/vulnerabilities");
        }

        [Fact]
        public override async Task CanGetVulnerabilities()
        {
            OSSIndexHttpClient http_client = new OSSIndexHttpClient("1.0");
            List<OSSIndexProjectVulnerability> v1 = (await http_client.GetVulnerabilitiesForIdAsync("284089289")).ToList();
            Assert.NotNull(v1);
            Assert.Equal(v1[1].Title, "CVE-2015-4670] Improper Limitation of a Pathname to a Restricted Directory");
        }

        [Fact]
        public override void CanGetVulnerabilitiesTask()
        {
            s.GetPackagesTask.Wait();
            s.GetArtifactsTask.Wait();
            Assert.NotEmpty(s.Packages);
            Assert.NotEmpty(s.Artifacts);            
            try
            {
                Task.WaitAll(s.GetVulnerabilitiesTask);
            }
            catch (AggregateException ae)
            {
                Assert.True(ae.InnerExceptions.Count() >= 1);
            }
            Assert.True(s.Vulnerabilities.All(v => v.Value.Count() == 0));
        }
    }
}

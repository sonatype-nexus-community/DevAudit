using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;
using WinAudit.AuditLibrary;

namespace WinAudit.Tests
{
    public class NuGetPackagesAuditTests
    {
        protected PackageSource nuget = new NuGetPackageSource();
        
        [Fact]
        public void CanGetNuGetPackagesTask()
        {
            Task<IEnumerable<OSSIndexQueryObject>> packages_task = nuget.GetPackagesTask;
            Assert.NotEmpty(packages_task.Result);
            Assert.NotEmpty(packages_task.Result.Where(p => p.PackageManager == "nuget"));
        }

        [Fact]
        public async Task CanGetNugetProject()
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
        public async Task CanGetNugetVulnerabilities()
        {
            OSSIndexHttpClient http_client = new OSSIndexHttpClient("1.0");
            List<OSSIndexProjectVulnerability> v1 = (await http_client.GetVulnerabilitiesForIdAsync("284089289")).ToList();
            Assert.NotNull(v1);
            Assert.Equal(v1[1].Title, "CVE-2015-4670] Improper Limitation of a Pathname to a Restricted Directory");
        }

        [Fact]
        public void CanGetArtifactsTask()
        {
            nuget.GetPackagesTask.Wait();
            Task<IEnumerable<OSSIndexArtifact>> artifacts_task = nuget.GetArtifactsTask;
            Assert.NotEmpty(artifacts_task.Result);
        }

        [Fact]
        public void CanGetVulnerabilitiesTask()
        {
            Assert.NotEmpty(nuget.GetPackagesTask.Result);
            Assert.NotEmpty(nuget.GetArtifactsTask.Result);            
            var v = nuget.GetVulnerabilitiesTask;
            try
            {
                Task.WaitAll(v);
            }
            catch (AggregateException ae)
            {
                Assert.Equal(ae.InnerExceptions.Count(), 1);
            }
            Assert.NotEmpty(nuget.Vulnerabilities);
        }
    }
}

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
        protected IPackagesAudit nuget_audit = new NuGetPackagesAudit();
        
        [Fact]
        public void CanGetNuGetPackagesTask()
        {
            Task<IEnumerable<OSSIndexQueryObject>> packages_task = nuget_audit.GetPackagesTask;
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
        public async Task CanGetProjectsTask()
        {
            Task<IEnumerable<OSSIndexQueryObject>> packages_task = nuget_audit.GetPackagesTask;
            await packages_task;
            Task<IEnumerable<OSSIndexQueryResultObject>> projects_task = nuget_audit.GetProjectsTask;
            Assert.NotEmpty(projects_task.Result);
        }

        [Fact]
        public void CanGetVulnerabilitiesTask()
        {
            Assert.NotEmpty(nuget_audit.GetPackagesTask.Result);
            Assert.NotEmpty(nuget_audit.GetProjectsTask.Result);            
            var v = nuget_audit.GetVulnerabilitiesTask;
            Task.WaitAll(v);
            Assert.NotEmpty(nuget_audit.Vulnerabilities);
        }
    }
}

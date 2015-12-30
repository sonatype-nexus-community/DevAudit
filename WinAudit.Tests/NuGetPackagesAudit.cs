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
        protected PackageSource nuget_audit = new NuGetPackagesAudit();
        
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
        public async Task CanGetNugetVulnerabilities()
        {
            OSSIndexHttpClient http_client = new OSSIndexHttpClient("1.0");
            List<OSSIndexProjectVulnerability> v1 = (await http_client.GetVulnerabilitiesForIdAsync("284089289")).ToList();
            Assert.NotNull(v1);
            Assert.Equal(v1[1].Title, "CVE-2015-4670] Improper Limitation of a Pathname to a Restricted Directory");
        }

        [Fact]
        public async Task CanContinueWithNugetVulnerabilities()
        {
            OSSIndexHttpClient http_client = new OSSIndexHttpClient("1.0");
            OSSIndexProject p1;
            Task<OSSIndexProject> t1;
            Task<IEnumerable<OSSIndexProjectVulnerability>> vt1;
            IEnumerable<OSSIndexProjectVulnerability> v1;
            Func<Task<IEnumerable<OSSIndexProjectVulnerability>>> getFunc = async () =>
            {
                OSSIndexProject p = await http_client.GetProjectForIdAsync("284089289");
                return await http_client.GetVulnerabilitiesForIdAsync(p.Id.ToString());
            };
            try
            {
                t1 = Task<OSSIndexProject>.Run(async () => (p1 = await http_client.GetProjectForIdAsync("284089289")));
                //vt1 = Task<IEnumerable<OSSIndexProjectVulnerability>>.Run(getFunc);
                Task<IEnumerable<OSSIndexProjectVulnerability>> t2 = t1.ContinueWith(async (antecedent) => (v1 = await http_client.GetVulnerabilitiesForIdAsync(antecedent.Result.Id.ToString()))).Unwrap();
                //vt1.Wait();
                t2.Wait();
                
            }
            catch (AggregateException e)
            {

            }
            //List<OSSIndexProjectVulnerability> v1 = (await http_client.GetVulnerabilitiesForIdAsync("284089289")).ToList();
            //Assert.NotNull(v1);
            //Assert.Equal(v1[1].Title, "CVE-2015-4670] Improper Limitation of a Pathname to a Restricted Directory");
        }


        [Fact]
        public async Task CanGetProjectsTask()
        {
            Task<IEnumerable<OSSIndexQueryObject>> packages_task = nuget_audit.GetPackagesTask;
            await packages_task;
            Task<IEnumerable<OSSIndexArtifact>> projects_task = nuget_audit.GetArtifactsTask;
            Assert.NotEmpty(projects_task.Result);
        }

        [Fact]
        public void CanGetVulnerabilitiesTask()
        {
            Assert.NotEmpty(nuget_audit.GetPackagesTask.Result);
            Assert.NotEmpty(nuget_audit.GetArtifactsTask.Result);            
            var v = nuget_audit.GetVulnerabilitiesTask;
            try
            {
                Task.WaitAll(v);
            }
            catch (AggregateException ae)
            {
                Assert.Equal(ae.InnerExceptions.Count(), 1);
            }
            Assert.NotEmpty(nuget_audit.Vulnerabilities);
        }
    }
}

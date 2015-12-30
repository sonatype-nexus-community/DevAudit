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
        public void CanGetNuGetPackages()
        {
            Task<IEnumerable<OSSIndexQueryObject>> packages_task = nuget_audit.GetPackagesTask;
            Assert.NotEmpty(packages_task.Result);
            Assert.NotEmpty(packages_task.Result.Where(p => p.PackageManager == "nuget"));
        }

        [Fact]
        public async Task CanGetProjectsTask()
        {
            Task<IEnumerable<OSSIndexQueryObject>> packages_task = nuget_audit.GetPackagesTask;
            await packages_task;
            Task<IEnumerable<OSSIndexQueryResultObject>> projects_task = nuget_audit.GetProjectsTask;
            Assert.NotEmpty(projects_task.Result);
        }
    }
}

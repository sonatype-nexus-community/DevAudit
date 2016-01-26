using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;
using WinAudit.AuditLibrary;

namespace WinAudit.Tests
{
    public abstract class PackageSourceTests
    {
        protected abstract PackageSource s { get; }
        
        [Fact]
        public void CanGetPackagesTask()
        {
            Task<IEnumerable<OSSIndexQueryObject>> packages_task = s.PackagesTask;
            packages_task.Wait();
            Assert.NotEmpty(packages_task.Result);
            Assert.NotEmpty(s.Packages.Where(p => p.PackageManager == s.PackageManagerId));
        }

        [Fact]
        public void CanGetArtifactsTask()
        {
            s.PackagesTask.Wait();
            Task.WaitAll(s.ArtifactsTask.ToArray());
            Assert.NotEmpty(s.Artifacts);
            Assert.NotEmpty(s.Artifacts.Where(a => a.PackageManager == s.PackageManagerId));
        }

        [Fact]
        public void CanGetVulnerabilitiesTask()
        {
            s.PackagesTask.Wait();
            Assert.NotEmpty(s.Packages);
            Task.WaitAll(s.ArtifactsTask.ToArray());
            Assert.NotEmpty(s.Artifacts);
            try
            {
                Task.WaitAll(s.VulnerabilitiesTask.ToArray());
            }
            
            catch (AggregateException)
            {

            }
            Assert.NotEmpty(s.Vulnerabilities);
        }

        [Fact]
        public void CanCacheProjectVulnerabilities() { }


        [Fact]
        public abstract Task CanGetProjects();

        [Fact]
        public abstract Task CanGetVulnerabilities();

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using
    System.Threading;
using System.Threading.Tasks;

using Xunit;
using DevAudit.AuditLibrary;

namespace DevAudit.Tests
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
        public void CanGetPackageVulnerabilities()
        {
            s.PackagesTask.Wait();
            Assert.NotEmpty(s.Packages);
            Task.WaitAll(s.ArtifactsTask.ToArray());
            Assert.NotEmpty(s.Artifacts);
            var t = new List<Task> (s.ArtifactProjects.Count());
            /*
            s.ArtifactProjects.ForEach(p =>
            {
                OSSIndexArtifact artifact = p as OSSIndexArtifact;
                List<OSSIndexPackageVulnerability> package_vulnerabilities = s.HttpClient.GetPackageVulnerabilitiesAsync(artifact.PackageId).Result;
            });*/
            
            s.ArtifactProjects.ForEach(p => t.Add(Task<Task>
                .Factory.StartNew(async (o) =>
                {
                    OSSIndexArtifact artifact = o as OSSIndexArtifact;
                    List<OSSIndexPackageVulnerability> package_vulnerabilities = await s.HttpClient.GetPackageVulnerabilitiesAsync(artifact.PackageId);
                }, p, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default).Unwrap()));
                
            try
            {
                Task.WaitAll(t.ToArray());
            }
            catch (AggregateException)
            {
                
            }
            catch (Exception)
            { }
            
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
        public abstract void CanCacheProjectVulnerabilities();
               
        [Fact]
        public abstract Task CanGetProjects();

        [Fact]
        public abstract Task CanGetVulnerabilities();

        [Fact]
        public abstract void CanComparePackageVersions();


    }
}

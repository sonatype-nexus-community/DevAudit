using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Xunit;
using DevAudit.AuditLibrary;

namespace DevAudit.Tests
{
    public abstract class PackageSourceTests
    {
        protected abstract PackageSource s { get; }
        
        [Fact]
        public void CanGetPackages()
        {
            Assert.NotEmpty(s.GetPackages().Where(p => p.PackageManager == s.PackageManagerId));
        }

        [Fact]
        public void CanGetPackageVulnerabilities()
        {
                
            
        }


        [Fact]
        public void CanGetVulnerabilitiesTask()
        {
            
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

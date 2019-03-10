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
        protected static void EnvironmentMessageHandler(object sender, EnvironmentEventArgs e) { }

        protected abstract PackageSource s { get; }
        
        [Fact]
        public void CanGetPackages()
        {
            Assert.NotEmpty(s.GetPackages());
        }

        public abstract Task CanGetVulnerabilities();
        
        public abstract void CanComparePackageVersions();
    }
}

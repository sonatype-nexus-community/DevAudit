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

        protected CancellationTokenSource Cts = new CancellationTokenSource();

        protected abstract PackageSource Source { get; }

        [Fact]
        public virtual void CanConstructPackageSource()
        {
            Assert.NotNull(Source);
        }

        [Fact]
        public virtual void CanGetPackages()
        {
            Assert.NotEmpty(Source.GetPackages());
        }

        [Fact]
        public virtual void CanGetVulnerabilities()
        {
            var res = Source.Audit(Cts.Token);
            Assert.True(res == AuditTarget.AuditResult.SUCCESS);
        }

        
        public abstract void CanTestVulnerabilityVersionInPackageVersionRange();

        public abstract void CanGetMinimumPackageVersion();
    }
}

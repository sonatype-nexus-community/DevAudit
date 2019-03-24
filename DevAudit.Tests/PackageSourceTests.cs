using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using DevAudit.AuditLibrary;

namespace DevAudit.Tests
{
    public abstract class PackageSourceTests
    {
        #region Constructors
        public PackageSourceTests()
        {
            DPS = this.Source as IDeveloperPackageSource;
        }
        #endregion

        #region Abstract properties
        protected abstract PackageSource Source { get; }
        #endregion

        #region Abstract methods
        public abstract void CanTestVulnerabilityVersionInPackageVersionRange();
        #endregion

        #region Properties
        protected CancellationTokenSource Cts { get; } = new CancellationTokenSource();

        protected IDeveloperPackageSource DPS { get; }
        #endregion

        #region Methods
        protected static void EnvironmentMessageHandler(object sender, EnvironmentEventArgs e) { }

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

        [Fact]
        public abstract void CanGetMinimumPackageVersion();
        #endregion
    }
}

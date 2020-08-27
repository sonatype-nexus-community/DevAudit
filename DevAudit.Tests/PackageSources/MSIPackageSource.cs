using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Xunit;

using DevAudit.AuditLibrary;

namespace DevAudit.Tests
{
    public class MSI : PackageSourceTests
    {
        #region Overriden Members
        protected override List<PackageSource> Sources { get; } =
            new List<PackageSource>()
            {
                new MSIPackageSource(new Dictionary<string, object>(), EnvironmentMessageHandler),
            };
        #endregion

        #region Overriden Tests
        [Fact]
        public override void CanTestVulnerabilityVersionInPackageVersionRange()
        {
            // Assert.True(Sources[0].IsVulnerabilityVersionInPackageVersionRange(">1.2", "<1.5.2"));
            // Assert.True(Sources[0].IsVulnerabilityVersionInPackageVersionRange("<=4.3", "<4.2"));
            // Assert.True(Sources[0].IsVulnerabilityVersionInPackageVersionRange(">=1.2.2", ">1.2.0-alpha.0"));
            // Assert.True(Sources[0].IsVulnerabilityVersionInPackageVersionRange(">12.2.2", "<=20.0.0"));
            // Assert.True(Sources[0].IsVulnerabilityVersionInPackageVersionRange(">12.2.2", "<=20.0.0"));
            //Fixme: Assert.True(Sources[0].IsVulnerabilityVersionInPackageVersionRange("1.3.x - 1.5.x", ">1.4.1"));
        }

        [Fact]
        public override void IsDeveloperPackageSource()
        {
            Assert.All(DPS, s => Assert.Null(s));
        }

        [Fact]
        public override void CanGetDPSMinimumPackageVersion()
        {
        }
        #endregion

        #region Tests
        [Fact]
        public void CanTestDPSPackageVersionIsRange()
        {
            Assert.False(DPS[0].PackageVersionIsRange("6.4.2"));
            Assert.True(DPS[0].PackageVersionIsRange("~5.1"));
            Assert.False(DPS[0].PackageVersionIsRange("=7.3"));
        }
        #endregion
    }
}

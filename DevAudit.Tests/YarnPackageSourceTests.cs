using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

using Xunit;


using DevAudit.AuditLibrary;

namespace DevAudit.Tests
{
    public class Yarn : PackageSourceTests 
    {
        #region Overriden properties
        protected override PackageSource Source { get; } = 
            new YarnPackageSource(new Dictionary<string, object>()
            { 
                {"File", @".\Examples\package.json.2" },
                {"LockFile", @".\Examples\yarn.lock.2"}
            
            }, EnvironmentMessageHandler);
        #endregion

        #region Overriden tests
        [Fact]
        public override void CanTestVulnerabilityVersionInPackageVersionRange()
        {
            Assert.True(Source.IsVulnerabilityVersionInPackageVersionRange(">1.2", "<1.5.2"));
            Assert.True(Source.IsVulnerabilityVersionInPackageVersionRange("<=4.3", "<4.2"));
            Assert.True(Source.IsVulnerabilityVersionInPackageVersionRange(">=1.2.2", ">1.2.0-alpha.0"));
            Assert.True(Source.IsVulnerabilityVersionInPackageVersionRange(">12.2.2", "<=20.0.0"));
        }

        [Fact]
        public override void IsDeveloperPackageSource()
        {
            Assert.NotNull(DPS);
        }

        [Fact]
        public override void CanGetDPSMinimumPackageVersion()
        {
            var v = DPS.GetMinimumPackageVersions(">=2.7.1");
            Assert.Single(v);
            Assert.EndsWith("1", v.Single());

            v = DPS.GetMinimumPackageVersions(">2.7.1");
            Assert.Single(v);

            v = DPS.GetMinimumPackageVersions("<2.6.1");
            Assert.Single(v);
            Assert.StartsWith("0", v.Single());

            v = DPS.GetMinimumPackageVersions("<=2.7.1");
            Assert.Single(v);
            Assert.EndsWith("1", v.Single());

            
            v = DPS.GetMinimumPackageVersions("^2.7.1");
            Assert.Single(v);
            Assert.EndsWith("1", v.Single());

            v = DPS.GetMinimumPackageVersions("~2.7.1");
            Assert.Single(v);
            Assert.EndsWith("1", v.Single());
        }

        #endregion

        [Fact]
        public void CanTestDPSPackageVersionIsRange()
        {
            Assert.False(DPS.PackageVersionIsRange("6.4.2"));
            Assert.True(DPS.PackageVersionIsRange("~5.1"));
            Assert.False(DPS.PackageVersionIsRange("=7.3"));
        }
    }
}

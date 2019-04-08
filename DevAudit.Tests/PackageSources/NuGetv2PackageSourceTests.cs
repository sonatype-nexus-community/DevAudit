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
    public class NuGetv2 : PackageSourceTests
    {
        #region Overriden Members
        protected override List<PackageSource> Sources { get; } =
            new List<PackageSource>()
            {
                new NuGetv2PackageSource(new Dictionary<string, object>()
                {
                    {"File", @".\Examples\packages.config.example" }

                }, EnvironmentMessageHandler),
                new NuGetv2PackageSource(new Dictionary<string, object>()
                {
                    {"File", @".\Examples\packages.config.example.2" }

                }, EnvironmentMessageHandler),
                new NuGetv2PackageSource(new Dictionary<string, object>()
                {
                    {"File", @".\Examples\packages.config.example.3" }

                }, EnvironmentMessageHandler)
            };
        #endregion

        #region Overriden Tests
        [Fact]
        public override void CanTestVulnerabilityVersionInPackageVersionRange()
        {
            Assert.True(Sources[0].IsVulnerabilityVersionInPackageVersionRange(">1.2", "<1.5.2"));
            Assert.True(Sources[0].IsVulnerabilityVersionInPackageVersionRange("<=4.3", "<4.2"));
            Assert.True(Sources[0].IsVulnerabilityVersionInPackageVersionRange(">=1.2.2", ">1.2.0-alpha.0"));
            Assert.True(Sources[0].IsVulnerabilityVersionInPackageVersionRange(">12.2.2", "<=20.0.0"));

            Assert.True(Sources[0].IsVulnerabilityVersionInPackageVersionRange("4.5.7", "(2.4, 6.1.3-alpha5]"));
            Assert.True(Sources[0].IsVulnerabilityVersionInPackageVersionRange("(11, 13.3.0-beta7]", "12"));
            Assert.True(Sources[0].IsVulnerabilityVersionInPackageVersionRange("5.1", "4.4, 6.3,5.1"));
        }

        [Fact]
        public override void CanGetVulnerabilities()
        {
            base.CanGetVulnerabilities();
            Assert.NotEmpty(Sources[0].Vulnerabilities.SelectMany(v => v.Value));
            Assert.NotEmpty(Sources[1].Vulnerabilities.SelectMany(v => v.Value));
        }

        [Fact]
        public override void IsDeveloperPackageSource()
        {
            Assert.All(DPS, s => Assert.NotNull(s));
        }

        [Fact]
        public override void CanGetDPSMinimumPackageVersion()
        {
            var v = DPS[0].GetMinimumPackageVersions(">=2.7.1");
            Assert.Single(v);
            Assert.EndsWith("1", v.Single());

            v = DPS[0].GetMinimumPackageVersions(">2.7.1");
            Assert.Single(v);
            Assert.EndsWith("2", v.Single());

            v = DPS[0].GetMinimumPackageVersions("<2.6.1");
            Assert.Single(v);
            Assert.StartsWith("0", v.Single());

            v = DPS[0].GetMinimumPackageVersions("<=2.7.1");
            Assert.Single(v);
            Assert.EndsWith("1", v.Single());

            v = DPS[0].GetMinimumPackageVersions("2.6.1");
            Assert.Single(v);
            Assert.Equal("2.6.1", v.Single());

            v = DPS[0].GetMinimumPackageVersions("<=2.7.1");
            Assert.Single(v);
            Assert.EndsWith("1", v.Single());

            v = DPS[0].GetMinimumPackageVersions("[2.7.1,3)");
            Assert.Single(v);
            Assert.EndsWith("1", v.Single());

        }
        #endregion

        #region Tests
        [Fact]
        public void CanTestDPSPackageVersionIsRange()
        {
            Assert.True(DPS[0].PackageVersionIsRange("[6.4.2, 7.0.0]"));
            Assert.True(DPS[0].PackageVersionIsRange("5.1, 5.3, 6.0"));
            Assert.True(DPS[0].PackageVersionIsRange(">7.3"));
            Assert.False(DPS[0].PackageVersionIsRange("7.3"));
        }
        #endregion
    }
}

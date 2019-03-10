using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;

using DevAudit.AuditLibrary;

namespace DevAudit.Tests
{
    public class Yarn : PackageSourceTests 
    {
        protected override PackageSource Source { get; } = 
            new YarnPackageSource(new Dictionary<string, object>(){ {"File", @".\Examples\package.json.2" } }, EnvironmentMessageHandler);
			
        [Fact]
        public override void CanTestVulnerabilityVersionInPackageVersionRange()
        {
            Assert.True(Source.IsVulnerabilityVersionInPackageVersionRange(">1.2", "<1.5.2"));
            Assert.True(Source.IsVulnerabilityVersionInPackageVersionRange("<=4.3", "<4.2"));
            Assert.True(Source.IsVulnerabilityVersionInPackageVersionRange(">=1.2.2", ">1.2.0-alpha.0"));
            Assert.True(Source.IsVulnerabilityVersionInPackageVersionRange(">12.2.2", "<=20.0.0"));
        }

        [Fact]
        public override void CanGetMinimumPackageVersion()
        {
            string version = "^6.26.0";
            Assert.Equal("6.26.0", YarnPackageSource.GetMinimumPackageVersion(version));
        }
    }
}

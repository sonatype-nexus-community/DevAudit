using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Xunit;

using DevAudit.AuditLibrary;

namespace DevAudit.Tests
{
    public class BowerPackageSourceTests : PackageSourceTests
    {
        protected override PackageSource s { get; } = new BowerPackageSource(new Dictionary<string, object>()
        { {"File", @".\Examples\bower.json.example" } });


        public override void CanComparePackageVersions()
        {
            s.IsVulnerabilityVersionInPackageVersionRange("2.1", "~2.1.6");
        }

        [Fact]
        public override async Task CanGetProjects()
        {
            OSSIndexHttpClient http_client = new OSSIndexHttpClient("1.1");
            OSSIndexProject p1 = await http_client.GetProjectForIdAsync("8396559329");
            Assert.NotNull(p1);
            Assert.Equal(p1.Id, 8396559329);
            Assert.Equal(p1.Name, "JQuery");
            Assert.Equal(p1.HasVulnerability, true);
            Assert.Equal(p1.Vulnerabilities, "http://ossindex.net:8080/v1.1/project/8396559329/vulnerabilities");
        }

        [Fact]
        public override async Task CanGetVulnerabilities()
        {
            OSSIndexHttpClient http_client = new OSSIndexHttpClient("1.0");
            List<OSSIndexProjectVulnerability> v1 = (await http_client.GetVulnerabilitiesForIdAsync("284089289")).ToList();
            Assert.NotNull(v1);
            Assert.Equal(v1[1].Title, "CVE-2015-4670] Improper Limitation of a Pathname to a Restricted Directory");
        }

        [Fact]
        public void CanTestVulnerabilityVersionInPackageVersionRange()
        {
            Assert.True(s.IsVulnerabilityVersionInPackageVersionRange(">1.2", "<1.5.2"));
            Assert.True(s.IsVulnerabilityVersionInPackageVersionRange("<=4.3", "<4.2"));
            Assert.True(s.IsVulnerabilityVersionInPackageVersionRange(">=1.2.2", ">1.2.0-alpha.0"));
            Assert.True(s.IsVulnerabilityVersionInPackageVersionRange(">12.2.2", "<=20.0.0"));

        }

        [Fact]
        public override void CanCacheProjectVulnerabilities()
        {
            //throw new NotImplementedException();
        }
           
    }
}

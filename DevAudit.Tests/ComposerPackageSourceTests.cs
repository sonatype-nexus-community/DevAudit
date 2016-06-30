using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;

using DevAudit.AuditLibrary;

namespace DevAudit.Tests
{
    public class ComposerPackageSourceTests : PackageSourceTests
    {
        protected override PackageSource s { get; } = new ComposerPackageSource(new Dictionary<string, object>()
        { {"File", @".\Examples\composer.json.example.1" } });

        public override void CanComparePackageVersions()
        {
            Assert.True(s.IsVulnerabilityVersionInPackageVersionRange(">=3.4", "3.6"));
            Assert.True(s.IsVulnerabilityVersionInPackageVersionRange(">1.2", "<1.5.2"));
            Assert.True(s.IsVulnerabilityVersionInPackageVersionRange("<=4.3", "<4.2"));
            Assert.True(s.IsVulnerabilityVersionInPackageVersionRange(">=1.2.2", ">1.2.0-alpha.0"));
            Assert.True(s.IsVulnerabilityVersionInPackageVersionRange(">12.2.2", "<=20.0.0"));
            Assert.True(s.IsVulnerabilityVersionInPackageVersionRange("~6.1", "6.1.1.0")); 
        }

        [Fact]
        public override async Task CanGetProjects()
        {
            OSSIndexHttpClient http_client = new OSSIndexHttpClient("1.1");
            OSSIndexProject p1 = await http_client.GetProjectForIdAsync("8396615975");
            Assert.NotNull(p1);
            Assert.Equal(p1.Id, 8396615975);
            Assert.Equal(p1.Name, "Cakephp");
            Assert.Equal(p1.HasVulnerability, true);
            Assert.Equal(p1.Vulnerabilities, "https://ossindex.net/v1.1/project/8396615975/vulnerabilities");
        }

        [Fact]
        public override async Task CanGetVulnerabilities()
        {
            OSSIndexHttpClient http_client = new OSSIndexHttpClient("1.1");
            List<OSSIndexProjectVulnerability> v1 = (await http_client.GetVulnerabilitiesForIdAsync("8396615975")).ToList();
            Assert.NotNull(v1);
            Assert.Equal(v1[0].Title, "[CVE-2006-4067] Cross-site scripting (XSS) vulnerability in cake/libs/error.php in CakePHP befor...");
        }

        [Fact]
        public override void CanCacheProjectVulnerabilities()
        {
            //throw new NotImplementedException();
        }
    }
}

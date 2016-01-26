using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;
using WinAudit.AuditLibrary;

namespace WinAudit.Tests
{
    public class NuGetPackageSourceTests : PackageSourceTests
    {
        protected override PackageSource s {get; } = new NuGetPackageSource(new Dictionary<string, object>()
        { {"File", @".\packages.config.example" } });

        [Fact]
        public override async Task CanGetProjects()
        {
            OSSIndexHttpClient http_client = new OSSIndexHttpClient("1.1");
            OSSIndexProject p1 = await http_client.GetProjectForIdAsync("8396831047");
            Assert.NotNull(p1);
            Assert.Equal(p1.Id, 8396831047);
            Assert.Equal(p1.Name, "Modernizr");
            Assert.Equal(p1.HasVulnerability, false);
        }

        [Fact]
        public override async Task CanGetVulnerabilities()
        {
            OSSIndexHttpClient http_client = new OSSIndexHttpClient("1.1");
            List<OSSIndexProjectVulnerability> v1 = (await http_client.GetVulnerabilitiesForIdAsync("8396559329")).ToList();
            Assert.NotNull(v1);
            Assert.True(v1.Any(v => v.Uri == "cve:/CVE-2011-4969"));
        }

    }
}

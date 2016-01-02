using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Xunit;
using WinAudit.AuditLibrary;

namespace WinAudit.Tests
{
    public class BowerPackageSourceTests : PackageSourceTests
    {
        protected override PackageSource s { get; } = new BowerPackageSource();
        
        public BowerPackageSourceTests()
        {
            s.PackageSourceOptions.Add("File", @".\bower.json.example");
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
            Assert.Equal(p1.Vulnerabilities, "https://ossindex.net/v1.1/project/8396559329/vulnerabilities");
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
        public void CanTestPackageVersionInRange()
        {
            
            Regex parse = new Regex(@"^(~+|<+=?|>+=?)(.*)", RegexOptions.Compiled);
            Regex parse_ex = new Regex(@"^(?<range>~+|<+=?|>+=?)" +
                @"(?<ver>(\d+)" +
                @"(\.(\d+))?" +
                @"(\.(\d+))?" +
                @"(\-([0-9A-Za-z\-\.]+))?" +
                @"(\+([0-9A-Za-z\-\.]+))?)$",
                RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.ExplicitCapture);
            Match m = parse_ex.Match("~1.2.3");
            Assert.True(m.Success);
            Assert.Equal(m.Groups[1].Value, "~");
            m = parse.Match("<=1.2.3");
            Assert.True(m.Success);
            Assert.Equal(m.Groups[1].Value, "<=");
            m = parse.Match("<1.2.3");
            Assert.True(m.Success);
            Assert.Equal(m.Groups[1].Value, "<");
            Assert.True(s.PackageVersionInRange(">1.2", "1.2.2"));
            Assert.True(s.PackageVersionInRange("<=4.3", "4.2"));
            Assert.True(s.PackageVersionInRange("<1.2.2", "1.2.1"));
            Assert.True(s.PackageVersionInRange(">12.2.2", "20.0.0"));
        }
    }
}

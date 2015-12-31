using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;
using WinAudit.AuditLibrary;
namespace WinAudit.Tests
{
    public class HttpClientv11Tests
    {
        protected OSSIndexHttpClient http_client = new OSSIndexHttpClient("1.1");
    
        [Fact]
        public async Task CanSearch()
        {
            OSSIndexQueryObject q1 = new OSSIndexQueryObject("msi", "Adobe Reader", "11.0.10", "");
            OSSIndexQueryObject q2 = new OSSIndexQueryObject("msi", "Adobe Reader", "10.1.1", "");

            IEnumerable<OSSIndexArtifact> r1 = await http_client.Search("msi", q1);
            Assert.NotEmpty(r1);
            IEnumerable<OSSIndexArtifact> r2 = await http_client.SearchAsync("msi", new List<OSSIndexQueryObject>() { q1, q2 });
            Assert.NotEmpty(r2);
            
        }

        [Fact]
        public async Task CanGetProject()
        {
            OSSIndexQueryObject q1 = new OSSIndexQueryObject("msi", "Adobe Reader", "11.0.10", "");
            IEnumerable<OSSIndexArtifact> r1 = await http_client.Search("msi", q1);
            Assert.True(r1.Count() == 1);
            OSSIndexProject p1 = await http_client.GetProjectForIdAsync(r1.First().ProjectId);
            Assert.NotNull(p1);
            Assert.Equal(p1.Name, "Adobe Reader");
            Assert.Equal(p1.HasVulnerability, true);
            Assert.Equal(p1.Vulnerabilities, "https://ossindex.net/v1.1/project/8396797903/vulnerabilities");
        }

        [Fact]
        public async Task CanGetVulnerabilityForId()
        {
            IEnumerable<OSSIndexProjectVulnerability> v = await http_client.GetVulnerabilitiesForIdAsync("8396797903");
            Assert.NotEmpty(v);
            Assert.Equal(v.First().ProjectId, "8396797903");
        }

    }
}

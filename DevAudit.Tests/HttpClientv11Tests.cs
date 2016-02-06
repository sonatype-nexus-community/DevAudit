using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;
using DevAudit.AuditLibrary;
namespace DevAudit.Tests
{
    public class HttpClientv11Tests : HttpClientTests
    {
        protected override OSSIndexHttpClient http_client { get; } = new OSSIndexHttpClient("1.1");
        protected Func<List<OSSIndexArtifact>, List<OSSIndexArtifact>> transform = (artifacts) =>
        {
            List<OSSIndexArtifact> o = artifacts.ToList();
            foreach (OSSIndexArtifact a in o)
            {
                if (a.Search == null || a.Search.Count() != 4)
                {
                    throw new Exception("Did not receive expected Search field properties for artifact name: " + a.PackageName + " id: " +
                        a.PackageId + " project id: " + a.ProjectId + ".");
                }
                else
                {
                    OSSIndexQueryObject package = new OSSIndexQueryObject(a.Search[0], a.Search[1], a.Search[3], "");
                    a.Package = package;
                }
            }
            return o;
        };

        [Fact]
        public override async Task CanSearch()
        {
            OSSIndexQueryObject q1 = new OSSIndexQueryObject("msi", "Adobe Reader", "11.0.10", "");
            OSSIndexQueryObject q2 = new OSSIndexQueryObject("msi", "Adobe Reader", "10.1.1", "");
            
            IEnumerable<OSSIndexArtifact> r1 = await http_client.SearchAsync("msi", q1, transform);
            Assert.NotEmpty(r1);
            Assert.True(r1.All(r => r.Package != null && !string.IsNullOrEmpty(r.Package.Name) && !string.IsNullOrEmpty(r.Package.Version)));
            IEnumerable<OSSIndexArtifact> r2 = await http_client.SearchAsync("msi", new List<OSSIndexQueryObject>() { q1, q2 }, transform);
            Assert.NotEmpty(r2);
            Assert.True(r2.All(r => r.Package != null && !string.IsNullOrEmpty(r.Package.Name) && !string.IsNullOrEmpty(r.Package.Version)));
        }

        
        public override async Task CanGetProject()
        {
            OSSIndexQueryObject q1 = new OSSIndexQueryObject("bower", "jquery", "1.6.1", "");
            IEnumerable<OSSIndexArtifact> r1 = await http_client.SearchAsync("bower", q1, transform);
            Assert.True(r1.Count() == 1);
            OSSIndexProject p1 = await http_client.GetProjectForIdAsync(r1.First().ProjectId);
            Assert.NotNull(p1);
            Assert.Equal(p1.Name, "JQuery");
            Assert.Equal(p1.HasVulnerability, true);
            Assert.Equal(p1.Vulnerabilities, "https://ossindex.net/v1.1/project/8396559329/vulnerabilities");
        }

        [Fact]
        public override async Task CanGetVulnerabilityForId()
        {
            IEnumerable<OSSIndexProjectVulnerability> v = await http_client.GetVulnerabilitiesForIdAsync("8396797903");
            Assert.NotEmpty(v);
            Assert.Equal(v.First().ProjectId, "8396797903");
        }

    }
}

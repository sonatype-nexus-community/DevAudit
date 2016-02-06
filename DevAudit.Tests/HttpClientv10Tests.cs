using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;
using DevAudit.AuditLibrary;

namespace DevAudit.Tests
{
    public class HttpClientv10Tests : HttpClientTests
    {
        protected override OSSIndexHttpClient http_client { get; } = new OSSIndexHttpClient("1.0");

        [Fact]
        public override async Task CanSearch()
        {
            OSSIndexQueryObject q1 = new OSSIndexQueryObject("nuget", "AjaxControlToolkit", "7.1213.0", "");
            Func<List<OSSIndexArtifact>, List<OSSIndexArtifact>> transform = (artifacts) =>
            {
                artifacts.Where(a => !string.IsNullOrEmpty(a.SCMId)).ToList().ForEach(a =>
                {
                    if (a.Search == null || a.Search.Count() != 4)
                    {
                        //throw new Exception("Did not receive expected Search field properties for artifact name: " + a.PackageName + " id: " +
                        //    a.PackageId + " project id: " + a.ProjectId + ".");
                        a.Package = new OSSIndexQueryObject(a.PackageManager, a.PackageName, "", "");
                    }
                    else a.Package = new OSSIndexQueryObject(a.Search[0], a.Search[1], a.Search[3], "");
                });
                return artifacts;
            };

            IEnumerable<OSSIndexArtifact> r1 = await http_client.SearchAsync("nuget", q1, transform);
            Assert.NotEmpty(r1);
            Assert.NotNull(r1.First().Package);
        }

        [Fact]
        public override async Task CanGetProject()
        {
            List<OSSIndexHttpException> http_errors = new List<OSSIndexHttpException>();
            Task<OSSIndexProject>[] t =
            {http_client.GetProjectForIdAsync("284089289"), http_client.GetProjectForIdAsync("8322029565") };
            try
            {
                await Task.WhenAll(t);
            }
            catch (AggregateException ae)
            {
                http_errors.AddRange(ae.InnerExceptions
                    .Where(i => i.GetType() == typeof(OSSIndexHttpException)).Cast<OSSIndexHttpException>());
            }
            
            List<OSSIndexProject> v = t.Where(s => s.Status == TaskStatus.RanToCompletion)
                .Select(ts => ts.Result).ToList();
            Assert.True(v.Count == 2);
            Assert.True(http_errors.Count == 0);
        }

        [Fact]
        public async override Task CanGetVulnerabilityForId()
        {
            IEnumerable<OSSIndexProjectVulnerability> v = await http_client.GetVulnerabilitiesForIdAsync("284089289");
            Assert.NotEmpty(v);
            Assert.Equal(v.First().ProjectId, "284089289");
        }
   
    }
}

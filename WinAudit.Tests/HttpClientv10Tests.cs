using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;
using WinAudit.AuditLibrary;
namespace WinAudit.Tests
{
    public class HttpClientv10Tests
    {
        protected OSSIndexHttpClient http_client = new OSSIndexHttpClient("1.0");

        [Fact]
        public async Task CanSearch()
        {
            OSSIndexQueryObject q1 = new OSSIndexQueryObject("nuget", "AjaxControlToolkit", "7.1213.0", "");
            IEnumerable<OSSIndexArtifact> r1 = await http_client.SearchAsync("nuget", q1);
            Assert.NotEmpty(r1);
        }

        [Fact]
        public async Task CanParallelGetProjects()
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
        public async Task CanGetVulnerabilityForId()
        {
            IEnumerable<OSSIndexProjectVulnerability> v = await http_client.GetVulnerabilitiesForIdAsync("284089289");
            Assert.NotEmpty(v);
            Assert.Equal(v.First().ProjectId, "284089289");
        }
   
    }
}

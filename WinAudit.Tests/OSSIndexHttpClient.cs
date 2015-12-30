using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;
using WinAudit.AuditLibrary;
namespace WinAudit.Tests
{
    public class OSSIndexHttpClientTests
    {
        protected OSSIndexHttpClient http_client = new OSSIndexHttpClient("1.1");
        
        public OSSIndexHttpClientTests()
        {

        }
    
        [Fact]
        public async Task CanSearchOSSIndex()
        {
            OSSIndexQueryObject q1 = new OSSIndexQueryObject("msi", "Adobe Reader", "11.0.10", "");
            OSSIndexQueryObject q2 = new OSSIndexQueryObject("msi", "Adobe Reader", "10.1.1", "");

            IEnumerable<OSSIndexQueryResultObject> r1 = await http_client.Search("msi", q1);
            Assert.NotEmpty(r1);
            IEnumerable<OSSIndexQueryResultObject> r2 = await http_client.SearchAsync("msi", new List<OSSIndexQueryObject>() { q1, q2 });
            Assert.NotEmpty(r2);
        }

        [Fact]
        public async Task CanGetOSSIndexVulnerabilityForId()
        {
            IEnumerable<OSSIndexProjectVulnerability> v = await http_client.GetVulnerabilitiesForIdAsync("284089289");
            Assert.NotEmpty(v);
        }

        [Fact]
        public void CanParallelGetPackages()
        {
            Task<IEnumerable<OSSIndexQueryObject>>[] t = 
            {
              //Task<IEnumerable<OSSIndexQueryObject>>.Factory.StartNew(() => http_client.GetMSIPackages()),
              //Task<IEnumerable<OSSIndexQueryObject>>.Factory.StartNew(() => http_client.GetOneGetPackages()),
              //Task<IEnumerable<OSSIndexQueryObject>>.Factory.StartNew(() => http_client.GetChocolateyPackages())
            };
            Task.WaitAll(t);
            Assert.NotEmpty(t[0].Result);
        }

        [Fact]
        public void CanParallelSearchPackages()
        {
            List<OSSIndexHttpException> http_errors = new List<OSSIndexHttpException>();
            Task<IEnumerable<OSSIndexProjectVulnerability>>[] t =
            {http_client.GetVulnerabilitiesForIdAsync("284089289"), http_client.GetVulnerabilitiesForIdAsync("2840892") };
            try
            {
                Task.WaitAll(t);
            }
            catch(AggregateException ae)
            {
                http_errors.AddRange(ae.InnerExceptions
                    .Where(i => i.GetType() == typeof(OSSIndexHttpException)).Cast<OSSIndexHttpException>());
            }
            List<IEnumerable<OSSIndexProjectVulnerability>> v = t.Where(s => s.Status == TaskStatus.RanToCompletion)
                .Select(ts => ts.Result).ToList();
            Assert.True(v.Count == 1);
            Assert.True(http_errors.Count == 1);
        }
    }
}

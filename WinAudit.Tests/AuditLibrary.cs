using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;
using WinAudit.AuditLibrary;
namespace WinAudit.Tests
{
    public class AuditLibraryTests
    {
        protected Audit audit = new Audit("1.1");

        public AuditLibraryTests()
        {
            
        }
        [Fact]
        public void CanGetMSIPackages()
        {
            IEnumerable<OSSIndexQueryObject> packages = audit.GetMSIPackages();
            Assert.NotEmpty(packages);
            Assert.NotEmpty(packages.Where(p => !string.IsNullOrEmpty(p.Vendor) && p.Vendor.StartsWith("Microsoft")));
            Assert.NotEmpty(packages.Where(p => !string.IsNullOrEmpty(p.Vendor) && p.Name.StartsWith("Windows")));
        }

        [Fact]
        public void CanGetChocolateyPackages()
        {
            IEnumerable<OSSIndexQueryObject> packages = audit.GetChocolateyPackages();
            Assert.NotEmpty(packages);
        }

        [Fact]
        public async Task CanSearchOSSIndex()
        {
            OSSIndexQueryObject q1 = new OSSIndexQueryObject("msi", "Adobe Reader", "11.0.10", "");
            OSSIndexQueryObject q2 = new OSSIndexQueryObject("msi", "Adobe Reader", "10.1.1", "");

            IEnumerable<OSSIndexQueryResultObject> r1 = await audit.SearchOSSIndex("msi", q1);
            Assert.NotEmpty(r1);
            IEnumerable<OSSIndexQueryResultObject> r2 = await audit.SearchOSSIndex("msi", new List<OSSIndexQueryObject>() { q1, q2 });
            Assert.NotEmpty(r2);
        }

    }
}

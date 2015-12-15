using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;
using WinAudit.AuditLibrary;
namespace WinAudit.Tests
{
    public class AuditLibrary
    {
        [Fact]
        public void CanGetMSIPackages()
        {
            Audit a = new Audit();
            IEnumerable<OSSIndexQueryObject> packages = a.GetMSIPackages();
            Assert.NotEmpty(packages);
        }
    }
}

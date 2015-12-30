using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;
using WinAudit.AuditLibrary;

namespace WinAudit.Tests
{
    public class BowerPackagesAuditTests
    {
        protected IPackageSource bower_audit = new BowerPackagesAudit();

        [Fact]
        public void CanGetBowerPackages()
        {
            Task<IEnumerable<OSSIndexQueryObject>> packages_task = bower_audit.GetPackagesTask;
            Assert.NotEmpty(packages_task.Result);
            Assert.NotEmpty(packages_task.Result.Where(p => p.PackageManager == "bower"));
        }
    }
}

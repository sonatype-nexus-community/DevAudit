using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;
using WinAudit.AuditLibrary;

namespace WinAudit.Tests
{
    public class OneGetPackagesAuditTests
    {
        protected IPackagesAudit oneget_audit = new OneGetPackagesAudit();

        [Fact]
        public void CanGetOneGetPackages()
        {
            Task<IEnumerable<OSSIndexQueryObject>> packages_task = oneget_audit.GetPackagesTask;
            Assert.NotEmpty(packages_task.Result);
            Assert.NotEmpty(packages_task.Result.Where(p => p.PackageManager == "oneget"));
        }
    }
}

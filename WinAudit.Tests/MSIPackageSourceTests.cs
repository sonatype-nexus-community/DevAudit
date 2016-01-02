using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;
using WinAudit.AuditLibrary;

namespace WinAudit.Tests
{
    public class MSIPackageSourceTests
    {
        protected PackageSource msi = new MSIPackageSource();

        [Fact]
        public void CanGetMSIPackages()
        {
            Task<IEnumerable<OSSIndexQueryObject>> packages_task = msi.PackagesTask;
            Assert.NotEmpty(packages_task.Result);
            Assert.NotEmpty(packages_task.Result.Where(p => p.PackageManager == "msi"));
            Assert.NotEmpty(packages_task.Result.Where(p => !string.IsNullOrEmpty(p.Vendor) && p.Vendor.StartsWith("Microsoft")));
            Assert.NotEmpty(packages_task.Result.Where(p => !string.IsNullOrEmpty(p.Vendor) && p.Name.StartsWith("Windows")));
        }
    }
}

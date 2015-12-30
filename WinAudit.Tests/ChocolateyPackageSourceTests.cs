using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;
using WinAudit.AuditLibrary;

namespace WinAudit.Tests
{
    public class ChocolateyPackageSourceTests
    {
        protected PackageSource chocolatey = new ChocolateyPackageSource();

        [Fact]
        public void CanGetOneGetPackages()
        {
            Task<IEnumerable<OSSIndexQueryObject>> packages_task = chocolatey.GetPackagesTask;
            Assert.NotEmpty(packages_task.Result);
            Assert.NotEmpty(packages_task.Result.Where(p => p.PackageManager == "chocolatey"));
        }
    }
}

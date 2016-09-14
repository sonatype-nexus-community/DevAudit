using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;
using DevAudit.AuditLibrary;

namespace DevAudit.Tests
{
    public class ChocolateyPackageSourceTests
    {
        protected PackageSource chocolatey = new ChocolateyPackageSource(new Dictionary<string, object>());

        [Fact]
        public void CanGetOneGetPackages()
        {
            Task<IEnumerable<OSSIndexQueryObject>> packages_task = chocolatey.PackagesTask;
            Assert.NotEmpty(packages_task.Result);
            Assert.NotEmpty(packages_task.Result.Where(p => p.PackageManager == "chocolatey"));
        }
    }
}

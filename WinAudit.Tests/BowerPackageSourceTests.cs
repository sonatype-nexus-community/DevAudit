using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;
using WinAudit.AuditLibrary;

namespace WinAudit.Tests
{
    public class BowerPackageSourceTests
    {
        protected PackageSource bower = new BowerPackageSource();
        
        public BowerPackageSourceTests()
        {
            bower.PackageSourceOptions.Add("File", @".\bower.json.example");
        }
        [Fact]
        public void CanGetBowerPackages()
        {
            Task<IEnumerable<OSSIndexQueryObject>> packages_task = bower.GetPackagesTask;
            Assert.NotEmpty(packages_task.Result);
            Assert.NotEmpty(packages_task.Result.Where(p => p.PackageManager == "bower"));
        }
    }
}

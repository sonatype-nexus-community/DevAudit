using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
            Task<IEnumerable<OSSIndexQueryObject>> packages_task = bower.PackagesTask;
            Assert.NotEmpty(packages_task.Result);
            Assert.NotEmpty(packages_task.Result.Where(p => p.PackageManager == "bower"));
        }

        [Fact]
        public void CanTestPackageVersionInRange()
        {
            
            Regex parse = new Regex(@"^(~+|<+=?|>+=?)(.*)", RegexOptions.Compiled);
            Regex parse_ex = new Regex(@"^(?<range>~+|<+=?|>+=?)" +
                @"(?<ver>(\d+)" +
                @"(\.(\d+))?" +
                @"(\.(\d+))?" +
                @"(\-([0-9A-Za-z\-\.]+))?" +
                @"(\+([0-9A-Za-z\-\.]+))?)$",
                RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.ExplicitCapture);
            Match m = parse_ex.Match("~1.2.3");
            Assert.True(m.Success);
            Assert.Equal(m.Groups[1].Value, "~");
            m = parse.Match("<=1.2.3");
            Assert.True(m.Success);
            Assert.Equal(m.Groups[1].Value, "<=");
            m = parse.Match("<1.2.3");
            Assert.True(m.Success);
            Assert.Equal(m.Groups[1].Value, "<");
            Assert.True(bower.PackageVersionInRange("1.2.2", "1.2"));
            Assert.True(bower.PackageVersionInRange("<=4.3", "4.2"));
            Assert.True(bower.PackageVersionInRange("<1.2.2", "1.2.1"));
            Assert.True(bower.PackageVersionInRange(">12.2.2", "20.0.0"));
        }
    }
}

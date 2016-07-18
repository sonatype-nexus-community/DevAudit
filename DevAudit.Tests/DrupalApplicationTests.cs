using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;
using DevAudit.AuditLibrary;

namespace DevAudit.Tests
{
    public class DrupalApplicationTests : PackageSourceTests
    {
        protected DrupalApplication d { get; } = new DrupalApplication(new Dictionary<string, object>()
        { {"RootDirectory", @"C:\Bitnami\wampstack-5.6.18-0\apps\drupal\htdocs" /*Application.CombinePaths("Examples", "Drupal")*/ }          
        });

        protected override PackageSource s { get; } = new DrupalApplication(new Dictionary<string, object>()
        { {"RootDirectory", @"C:\Bitnami\wampstack-5.6.18-0\apps\drupal\htdocs" /*Application.CombinePaths("Examples", "Drupal")*/ }
        });


        [Fact]
        public void CanConstruct ()
        {
            Assert.True(d.ApplicationFileSystemMap.ContainsKey("RootDirectory"));
            Assert.NotNull(d.CorePackagesFile);
            Assert.NotNull(d.CoreModulesDirectory);
            Dictionary<string, IEnumerable<OSSIndexQueryObject>> modules = d.GetModules();
            Assert.NotEmpty(modules["core"]);
            Assert.NotEmpty(modules["contrib"]);
            Assert.NotEmpty(modules["sites_all_contrib"]);
            Assert.NotEmpty(modules["all"]);
            Assert.True(modules["all"].Count() == modules["core"].Count() + modules["contrib"].Count() + modules["sites_all_contrib"].Count());
        }

        public override Task CanGetVulnerabilities()
        {
            throw new NotImplementedException();
        }

        public override Task CanGetProjects()
        {
            throw new NotImplementedException();
        }

        public override void CanCacheProjectVulnerabilities()
        {
            throw new NotImplementedException();
        }

        public override void CanComparePackageVersions()
        {
            Assert.True(s.IsVulnerabilityVersionInPackageVersionRange("6.x-1.0", "<6.x-2.0"));
            Assert.True(s.IsVulnerabilityVersionInPackageVersionRange("<=5.x-2.8", "<6.x-1.0"));
            Assert.True(s.IsVulnerabilityVersionInPackageVersionRange(">=5.x-2.0", "6.x-1.0-alpha1"));
            //Assert.True(s.IsVulnerabilityVersionInPackageVersionRange(">12.2.2", "<=20.0.0"));
        }
    }
}

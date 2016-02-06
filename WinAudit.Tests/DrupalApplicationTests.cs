using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;
using WinAudit.AuditLibrary;

namespace WinAudit.Tests
{
    public class DrupalApplicationTests
    {
        protected DrupalApplication d { get; } = new DrupalApplication(new Dictionary<string, object>()
        { {"RootDirectory", Application.CombinePaths("Examples", "Drupal") }          
        });

        [Fact]
        public void CanConstruct()
        {
            Assert.True(d.ApplicationFileSystemMap.ContainsKey("RootDirectory"));
            Assert.NotNull(d.CorePackagesFile);
        }
    }
}

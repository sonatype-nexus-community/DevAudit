using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

using Xunit;
using DevAudit.AuditLibrary;

namespace DevAudit.Tests
{
    public class PowerShellTests
    {
        [Fact]
        public void CanGetPackages()
        {            
            InitialSessionState s = InitialSessionState.CreateDefault();
            s.ImportPSModule(new[] { @".\DevAudit.PowerShell.dll" });
            Runspace r = RunspaceFactory.CreateRunspace(s);
            r.Open();            
            Pipeline pipeline = r.CreatePipeline();
            Command c = new Command("Get-AuditPackages");
            c.Parameters.Add("Source", "msi");
            pipeline.Commands.Add(c);                        
            Collection<PSObject> results = pipeline.Invoke();                        
            r.Close();
            IEnumerable<Package> packages = results.Select(result => (Package) result.BaseObject);
            Assert.NotEmpty(packages);
            Assert.NotEmpty(packages.Where(p => p.PackageManager == "msi"));
            Assert.NotEmpty(packages.Where(p => p.Name.Contains("Microsoft")));
        }
    }
}

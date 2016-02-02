using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

using Xunit;
using WinAudit.AuditLibrary;

namespace WinAudit.Tests
{
    public class PowerShellTests
    {
        [Fact]
        public void CanGetPackaages()
        {            
            InitialSessionState s = InitialSessionState.CreateDefault();
            s.ImportPSModule(new[] { @".\WinAudit.PowerShell.dll" });
            Runspace r = RunspaceFactory.CreateRunspace(s);
            r.Open();            
            Pipeline pipeline = r.CreatePipeline();
            Command c = new Command("Audit-Packages");
            c.Parameters.Add("Source", "msi");
            pipeline.Commands.Add(c);                        
            Collection<PSObject> results = pipeline.Invoke();
            foreach(PSObject result in results)
            {
                
            }
            
            r.Close();
        }
    }
}

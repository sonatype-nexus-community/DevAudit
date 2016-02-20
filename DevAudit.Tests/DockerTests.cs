using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;

using DevAudit.AuditLibrary;

namespace DevAudit.Tests
{
    public class DockerTests
    {
        [Fact]
        public void CanInspectContainer()
        {
            Docker.ProcessStatus process_status;
            string process_output, process_error;
            Docker.GetContainer("48743124ffee", out process_status, out process_output, out process_error);
        }

        [Fact]
        public void CanExecCommand()
        {
            Docker.ProcessStatus process_status;
            string process_output, process_error;
            Docker.ExecuteInContainer("5a9d667f7a13", @"dpkg-query -W -f ${Package}'${Version}\n", out process_status, out process_output, out process_error);
        }

        [Fact]
        public void CanDpkg()
        {
            DpkgPackageSource dpkg = new DpkgPackageSource(new Dictionary<string, object>() { { "DockerContainerId", "5a9d667f7a13" } });
            dpkg.PackagesTask.Wait();
            Assert.NotEmpty(dpkg.PackagesTask.Result);

        }
    }
}

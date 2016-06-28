using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Medallion.Shell;
using Xunit;

using DevAudit.AuditLibrary;

namespace DevAudit.Tests
{
    public class InteractiveConsoleProcessTests
    {
        [Fact]
        public void CanRunSsh()
        {
            var shell = Shell.Default;

            Command c = Shell.Default.Run(@"..\..\..\cygwin\bin\bash.exe", "--login");
            c.StandardInput.NewLine = "\n";
            Task i = c.StandardInput.WriteLineAsync("ls.exe");
            i.Wait(1000);
            MemoryStream output = new MemoryStream();
            MemoryStream error = new MemoryStream();
            Task e = c.StandardError.ReadLineAsync();
            Task o = c.StandardOutput.ReadLineAsync();
            Task.WaitAny(new Task[] { e, o }, 2000);


            //Task<string> o = c.StandardOutput.ReadLineAsync();


            //Assert.NotNull(o.Result);
            //tion<string> o = new Action<string>((string s) => { });
            //r0ctiveConsoleProcess p = new InteractiveConsoleProcess(@"C:\cygwin\bin\ssh.exe", "-v", o);
        }

        [Fact]
        public void CanRunConsoleProcess()
        {

        }
    }
}

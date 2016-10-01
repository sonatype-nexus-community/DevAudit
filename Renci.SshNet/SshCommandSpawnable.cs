using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using ExpectNet;
namespace Renci.SshNet
{
    /// <summary>
    /// Represents Ssh commmand.
    /// </summary>
    public class SshCommandSpawanble : ISpawnable
    {
        private SshCommand command;
        private CommandAsyncResult command_result;
        /// <summary>
        /// Initializes new Ssh Command spawnable.
        /// </summary>
        /// <param name="command">Ssh command to be run.</param>
        public SshCommandSpawanble(SshCommand command)
        {
            this.command = command;
        }

        /// <summary>
        /// Initialise command execution
        /// </summary>
        public void Init()
        {
            this.command_result = this.command.BeginExecute() as CommandAsyncResult;
        }

        /// <summary>
        /// Writes 
        /// </summary>
        /// <param name="command">specify what should be written to process</param>
        public void Write(string command)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Reads in asynchronous way from both standard input and standard error streams. 
        /// </summary>
        /// <returns>text read from streams</returns>
        public async Task<string> ReadAsync()
        {

            return await CreateStringAsync(new char[256], Task<int>.Run(() => 1));
        }

        private async Task<string> CreateStringAsync(char[] c, Task<int> n)
        {
            return new string(c, 256, await n.ConfigureAwait(false));
        }

        /// <summary>
        /// Wait till command has completed executing then return the output 
        /// </summary>
        /// <returns>text read from streams</returns>
        public string Read(CancellationTokenSource cts)
        {
            this.command_result.AsyncWaitHandle.WaitOne();
            cts.Cancel(false);
            return this.command.EndExecute(command_result);
        }

        private void ExecuteCallback(IAsyncResult result)
        {
            
        }

    
    }


}

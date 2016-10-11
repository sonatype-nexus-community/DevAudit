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
    public class SshCommandSpawanble : ISpawnable, IDisposable
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
            return await Task.Factory.FromAsync(command_result, command.EndExecute);
        }

        
        /// <summary>
        /// Wait till command has completed executing then return the output 
        /// </summary>
        /// <returns>text read from streams</returns>
        public string Read(out bool completed)
        {
            string r = this.command.EndExecute(command_result);
            completed = this.command_result.IsCompleted;
            return r;
        }

        #region Disposer
        private bool IsDisposed { get; set; }
        /// <summary> 
        /// /// Implementation of Dispose according to .NET Framework Design Guidelines. 
        /// /// </summary> 
        /// /// <remarks>Do not make this method virtual. 
        /// /// A derived class should not be able to override this method. 
        /// /// </remarks>         
        public void Dispose()
        {
            Dispose(true); // This object will be cleaned up by the Dispose method. // Therefore, you should call GC.SupressFinalize to // take this object off the finalization queue // and prevent finalization code for this object // from executing a second time. // Always use SuppressFinalize() in case a subclass // of this type implements a finalizer. GC.SuppressFinalize(this); }
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Called when a SshCommandSpawnable is disposed
        /// </summary>
        /// <param name="isDisposing">Flag indicating the operation being performed.</param>
        protected virtual void Dispose(bool isDisposing)
        {
            // TODO If you need thread safety, use a lock around these 
            // operations, as well as in your methods that use the resource. 
            try
            {
                if (!this.IsDisposed)
                {
                    // Explicitly set root references to null to expressly tell the GarbageCollector 
                    // that the resources have been disposed of and its ok to release the memory 
                    // allocated for them. 
                    if (isDisposing)
                    {
                        // Release all managed resources here 
                        // Need to unregister/detach yourself from the events. Always make sure 
                        // the object is not null first before trying to unregister/detach them! 
                        // Failure to unregister can be a BIG source of memory leaks 
                        //if (someDisposableObjectWithAnEventHandler != null)
                        //{ someDisposableObjectWithAnEventHandler.SomeEvent -= someDelegate; 
                        //someDisposableObjectWithAnEventHandler.Dispose(); 
                        //someDisposableObjectWithAnEventHandler = null; } 
                        // If this is a WinForm/UI control, uncomment this code 
                        //if (components != null) //{ // components.Dispose(); //} } 
                        // Release all unmanaged resources here 
                        // (example) if (someComObject != null && Marshal.IsComObject(someComObject)) { Marshal.FinalReleaseComObject(someComObject); someComObject = null; 
                        if (this.command != null)
                        {
                            this.command.Dispose();
                            this.command = null;
                        }
                    }
                }
            }
            finally
            {
                this.IsDisposed = true;
            }
        }
        #endregion

    }


}

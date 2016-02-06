using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management.Automation;

using WinAudit.AuditLibrary;

namespace WinAudit.PowerShell
{
    public class WinAuditCmdlet : Cmdlet, IDisposable
    {
        #region Public properties
        [Parameter(
            Mandatory = true,
            ValueFromPipelineByPropertyName = true,
            ValueFromPipeline = true,
            Position = 0,
            HelpMessage = "Package source to audit."
        )]
        [ValidatePattern("^nuget|msi|bower|oneget|chocolatey|composer|drupal$")]
        public string Source { get; set; }

        [Parameter(
            Mandatory = false,
            ValueFromPipelineByPropertyName = true,
            ValueFromPipeline = true,
            Position = 1,
            HelpMessage = "Package manager configuration file."
        )]
        public string File { get; set; }

        [Parameter(
            Mandatory = false,
            ValueFromPipelineByPropertyName = true,
            ValueFromPipeline = true,
            Position = 2,
            HelpMessage = "Root directory of application instance."
        )]
        public string RootDirectory { get; set; }
        #endregion

        #region Overriden methods
        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            switch (Source)
            {
                case "nuget":
                    if (string.IsNullOrEmpty(this.File))
                    {
                        this.File = "packages.config";
                        WriteVerbose("File parameter not specified, using packages.config by default.");
                    }
                    if (!System.IO.File.Exists(this.File))
                    {
                        this.ThrowTerminatingError(new ErrorRecord(new ArgumentException("File not found: " + File + "."), "FileParameter", ErrorCategory.InvalidArgument, null));
                    }
                    else
                    {
                        this.PackageSourceOptions.Add("File", File);
                    }
                    this.PackageSource = new NuGetPackageSource(this.PackageSourceOptions);
                    WriteVerbose(string.Format("Using package source {0} and package manager configuration file {1}.", this.PackageSource.PackageManagerLabel, this.PackageSource.PackageManagerConfigurationFile));
                    break;

                case "msi":
                    this.PackageSource = new MSIPackageSource(this.PackageSourceOptions);
                    WriteVerbose(string.Format("Using package source {0}.", this.PackageSource.PackageManagerLabel));
                    break;

                case "bower":
                    if (string.IsNullOrEmpty(this.File))
                    {
                        this.File = "bower.json";
                        WriteVerbose("File parameter not specified, using bower.json.example by default.");
                    }
                    if (!System.IO.File.Exists(this.File))
                    {
                        this.ThrowTerminatingError(new ErrorRecord(new ArgumentException("File not found: " + File + "."), "FileParameter", ErrorCategory.InvalidArgument, null));
                    }
                    else
                    {
                        this.PackageSourceOptions.Add("File", File);
                    }
                    this.PackageSource = new BowerPackageSource(this.PackageSourceOptions);
                    WriteVerbose(string.Format("Using package source {0} and package manager configuration file {1}.", this.PackageSource.PackageManagerLabel, this.PackageSource.PackageManagerConfigurationFile));
                    break;

                case "composer":
                    if (string.IsNullOrEmpty(this.File))
                    {                        
                        this.File = "composer.json";
                        WriteVerbose("File parameter not specified, using composer.json.example by default.");
                    }
                    if (!System.IO.File.Exists(this.File))
                    {
                        this.ThrowTerminatingError(new ErrorRecord(new ArgumentException("File not found: " + File + "."), "FileParameter", ErrorCategory.InvalidArgument, null));
                    }
                    else
                    {
                        this.PackageSourceOptions.Add("File", File);
                    }
                    this.PackageSource = new ComposerPackageSource(this.PackageSourceOptions);
                    WriteVerbose(string.Format("Using package source {0} and package manager configuration file {1}.", this.PackageSource.PackageManagerLabel, this.PackageSource.PackageManagerConfigurationFile));
                    break;

                case "oneget":
                    this.PackageSource = new OneGetPackageSource(this.PackageSourceOptions);
                    WriteVerbose(string.Format("Using package source {0}.", this.PackageSource.PackageManagerLabel));
                    break;

                case "choco":
                    this.PackageSource = new ChocolateyPackageSource(this.PackageSourceOptions);
                    WriteVerbose(string.Format("Using package source {0}.", this.PackageSource.PackageManagerLabel));
                    break;

                case "drupal":
                    if (string.IsNullOrEmpty(this.RootDirectory))
                    {                        
                        WriteVerbose("Root directory parameter not specified, using current directory by default.");
                    }
                    else if (!System.IO.Directory.Exists(this.RootDirectory))
                    {
                        this.ThrowTerminatingError(new ErrorRecord(new ArgumentException("Directory not found: " + RootDirectory + "."), "RootDirectoryParameter", ErrorCategory.InvalidArgument, null));
                    }
                    else
                    {
                        this.PackageSourceOptions.Add("RootDirectory", RootDirectory);
                    }
                    this.PackageSource = new DrupalApplication(this.PackageSourceOptions);
                    WriteVerbose(string.Format("Using application {0}.", this.PackageSource.PackageManagerLabel));
                    break;

                default:
                    this.ThrowTerminatingError(new ErrorRecord(new ArgumentException("PackageManager"), "PackageManagerParameter", ErrorCategory.InvalidArgument, null));
                    break;

            }
        }
        
        #endregion

        #region Protected methods
        protected void HandleOSSIndexHttpException(Exception e)
        {
            if (e.GetType() == typeof(OSSIndexHttpException))
            {
                OSSIndexHttpException oe = (OSSIndexHttpException)e;
                WriteError(new ErrorRecord(new Exception(string.Format("HTTP status: {0} {1} \nReason: {2}\nRequest:\n{3}",
                    (int)oe.StatusCode, oe.StatusCode, oe.ReasonPhrase, oe.Request)), "OSSIndexHttpException", ErrorCategory.InvalidOperation, null));
            }

        }
        #endregion

        #region Protected properties
        protected PackageSource PackageSource { get; set; }

        protected Dictionary<string, object> PackageSourceOptions { get; set; } = new Dictionary<string, object>();
        #endregion

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
                        this.PackageSource.Dispose();
                        this.PackageSource = null;
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

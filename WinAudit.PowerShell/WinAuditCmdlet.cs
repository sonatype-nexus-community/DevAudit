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
    public class WinAuditCmdlet : Cmdlet
    {
        #region Public properties
        [Parameter(
            Mandatory = true,
            ValueFromPipelineByPropertyName = true,
            ValueFromPipeline = true,
            Position = 0,
            HelpMessage = "Package source to audit."
        )]
        [ValidatePattern("^nuget|msi|bower|oneget|chocolatey|composer$")]
        public string Source { get; set; }

        [Parameter(
            Mandatory = false,
            ValueFromPipelineByPropertyName = true,
            ValueFromPipeline = true,
            Position = 1,
            HelpMessage = "Package manager configuration file."
        )]
        public string File { get; set; }
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
                        this.File = "bower.json.example";
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
                        this.File = "composer.json.example";
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

                default:
                    this.ThrowTerminatingError(new ErrorRecord(new ArgumentException("PackageManager"), "PackageManagerParameter", ErrorCategory.InvalidArgument, null));
                    break;

            }
        }

        protected override void EndProcessing()
        {
            base.EndProcessing();
            this.PackageSource.Dispose();
        }

        protected override void StopProcessing()
        {
            base.StopProcessing();
            this.PackageSource.Dispose();
        }

        #endregion

        #region Protected methods
        protected void HandleOSSIndexHttpException(Exception e)
        {
            if (e.GetType() == typeof(OSSIndexHttpException))
            {
                OSSIndexHttpException oe = (OSSIndexHttpException)e;
                WriteError(new ErrorRecord(new Exception(string.Format("HTTP status: {0} {1} \nReason: {2}\nRequest:\n{3}",
                    (int)oe.StatusCode, oe.StatusCode, oe.ReasonPhrase, oe.Request)), "OSSIndexHttpException", ErrorCategory.InvalidData, null));
            }

        }
        #endregion

        #region Protected properties
        protected PackageSource PackageSource { get; set; }

        protected Dictionary<string, object> PackageSourceOptions { get; set; } = new Dictionary<string, object>();
        #endregion
    }
}

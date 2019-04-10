using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

using Xunit;

using DevAudit.AuditLibrary;

namespace DevAudit.Tests.VMRequired
{
    public class Ssh : EnvironmentTests 
    {
        #region Constructor
        public Ssh() : base()
        {
            Sources.Add(
                new YarnPackageSource(new Dictionary<string, object>
                {
                    {"AuditEnvironment", SshEnv},
                    {"HostEnvironment", HostEnvironment},
                }, EnvironmentMessageHandler));

        }
        #endregion

        #region Overriden Members
        protected override AuditEnvironment Env => SshEnv;

        protected override List<string> FilesToConstruct { get; } = new List<string>
        {
            "README.md",
            "LICENSE",
            "packages/empty.ts"
        };

        protected override List<string> FilesToTestExistence => FilesToConstruct;

        protected override Dictionary<string, string> FilesToRead { get; } = new Dictionary<string, string>
        {
            {"LICENSE", "Permission is hereby granted, free of charge,"}
        };
        #endregion

        #region Properties
        protected SshAuditEnvironment SshEnv {get; } = new SshAuditEnvironment(EnvironmentMessageHandler, "ssh", 
            Environment.GetEnvironmentVariable("SSH_REMOTE_HOST"), 
            Int32.Parse(Environment.GetEnvironmentVariable("SSH_REMOTE_SSH_PORT")),
            Environment.GetEnvironmentVariable("SSH_REMOTE_USER"), 
            Environment.GetEnvironmentVariable("SSH_REMOTE_SSH_PASS"), null, new OperatingSystem(PlatformID.Unix, new Version(0, 0)), 
            HostEnvironment);
        #endregion
    }
}

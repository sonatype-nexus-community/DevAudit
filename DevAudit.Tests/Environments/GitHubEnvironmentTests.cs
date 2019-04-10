using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

using Xunit;

using DevAudit.AuditLibrary;

namespace DevAudit.Tests
{
    public class GitHub : EnvironmentTests 
    {
        #region Constructor
        public GitHub() : base()
        {
            Sources.Add(
                new YarnPackageSource(new Dictionary<string, object>
                {
                    {"AuditEnvironment", GitHubEnv},
                    {"HostEnvironment", HostEnvironment},
                }, EnvironmentMessageHandler));

        }
        #endregion

        #region Overriden Properties
        protected override AuditEnvironment Env => GitHubEnv;

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
        protected GitHubAuditEnvironment GitHubEnv {get; } = new GitHubAuditEnvironment(EnvironmentMessageHandler, 
        Environment.GetEnvironmentVariable("GITHUB_USER_API_TOKEN"), "angular", "angular", "master", HostEnvironment);
        #endregion
    }
}

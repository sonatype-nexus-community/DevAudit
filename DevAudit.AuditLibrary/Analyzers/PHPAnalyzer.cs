using System;
using System.Collections.Generic;
using System.IO;

namespace DevAudit.AuditLibrary.Analyzers
{
    public abstract class PHPAnalyzer : Analyzer
    {
        #region Constructors
        public PHPAnalyzer(ScriptEnvironment script_env, string name, object workspace, object project, object compilation) : base(script_env, name, workspace, project, compilation)
        {
            this.Tags.Add("php");
            this.ProjectFiles = workspace as Dictionary<string, List<FileInfo>>;
            this.PHPSourceUnits = project as Dictionary<FileInfo, PHPAuditSourceUnit>;
            this.PhpFiles = this.ProjectFiles["PHP"];
            this.YamlFiles = this.ProjectFiles["YAML"];
            this.JsonFiles = this.ProjectFiles["JSON"];
        }
        #endregion

        #region Public properties
        public Dictionary<string, List<FileInfo>> ProjectFiles { get; protected set; }
        public Dictionary<FileInfo, PHPAuditSourceUnit> PHPSourceUnits { get; protected set; }
        public List<FileInfo> PhpFiles { get; protected set; }
        public List<FileInfo> YamlFiles { get; protected set; }
        public List<FileInfo> JsonFiles { get; protected set; }
        #endregion
    }
}

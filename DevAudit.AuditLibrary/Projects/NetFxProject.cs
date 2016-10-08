using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevAudit.AuditLibrary
{
    
    public class NetFxProject : CodeProject
    {
        #region Overriden properties
        public override string CodeProjectId { get { return "netfx"; } }

        public override string CodeProjectLabel { get { return ".NET Framework"; } }

        public override string PackageManagerId { get { return ""; } }

        public override string PackageManagerLabel { get { return "Drupal"; } }

        public override OSSIndexHttpClient HttpClient { get; } = new OSSIndexHttpClient("1.1");
        #endregion

        #region Constructors
        public NetFxProject(Dictionary<string, object> project_options, EventHandler<EnvironmentEventArgs> message_handler) : base(project_options, message_handler) { }
        #endregion
    }
    
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;

namespace DevAudit.AuditLibrary
{
    public class MVC5CodeProject : NetFxCodeProject
    {
        public MVC5CodeProject(Dictionary<string, object> project_options, EventHandler<EnvironmentEventArgs> message_handler) : 
            base(project_options, new Dictionary<string, string[]> { { "AppConfig", new string[] { "Web.config" } } }, message_handler)
        {
           
        }

        #region Methods
      
        #endregion
    }
}

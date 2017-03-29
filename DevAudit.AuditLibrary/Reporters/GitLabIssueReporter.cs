using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using NGitLab;
using NGitLab.Models;

namespace DevAudit.AuditLibrary
{
    public class GitLabIssueReporter : AuditReporter
    {
        #region Constructors
        public GitLabIssueReporter(PackageSource source) : base(source) {} 
        #endregion

        #region Overriden methods
        public override async Task<bool> ReportPackageSourceAudit()
        {
            if (!this.AuditOptions.ContainsKey("GitLabReportUrl") || !this.AuditOptions.ContainsKey("GitLabReportName") || !this.AuditOptions.ContainsKey("GitLabToken"))
            {
                throw new ArgumentException("A required audit option for the GitLab environment is missing.");
            }
            HostUrl = (string) this.AuditOptions["GitLabReportUrl"];
            Token = (string)this.AuditOptions["GitLabToken"];
            ProjectName = (string)this.AuditOptions["GitLabReportName"];
            GitLabClient client = null;
            try
            {
                this.AuditEnvironment.Info("Connecting to project {0} at {1}", ProjectName, HostUrl);
                client = new GitLabClient(HostUrl, Token);                 
                IEnumerable<Project> projects = await client.Projects.Owned();
                Project = projects.Where(p => p.Name == ProjectName).FirstOrDefault();
                this.AuditEnvironment.Info("Connected to project {0}.", Project.PathWithNamespace);
            }
            catch (AggregateException ae)
            {
                AuditEnvironment.Error(ae, "Could not get project {0} at url {1}.", ProjectName, HostUrl);
                return false;
            }
            catch (Exception e)
            {
                AuditEnvironment.Error(e, "Could not get project {0} at url {1}.", ProjectName, HostUrl);
                return false;
            }
            if (Project == null)
            {
                AuditEnvironment.Error("Could not find the project {0}.", Project.Name);
                return false;
            }
            if (!Project.IssuesEnabled)
            {
                AuditEnvironment.Error("Issues are not enabled for the project {0}/{1}.", Project.Owner, Project.Name);
                return false;
            }
            if (AuditOptions.ContainsKey("GitLabReportTitle"))
            {
                IssueTitle = (string)AuditOptions["GitLabReportTitle"];
            }
            else
            {
                IssueTitle = string.Format("[DevAudit] {2} audit on {0} {1}", DateTime.UtcNow.ToShortDateString(), DateTime.UtcNow.ToShortTimeString(), Source.PackageManagerLabel);
            }
            BuildPackageSourceAuditReport();           
            try
            {
                IssueCreate ic = new IssueCreate
                {
                    ProjectId = Project.Id,
                    Title = IssueTitle,
                    Description = IssueText.ToString()
                };
                Issue issue = await client.Issues.CreateAsync(ic);
                if (issue != null)
                {
                    AuditEnvironment.Success("Created issue #{0} '{1}' in GitLab project {2}/{3} at host url {4}.", issue.IssueId, issue.Title, Project.Owner, ProjectName, HostUrl);
                    return true;
                }
                else
                {
                    AuditEnvironment.Error("Error creating new issue for project {0} at host url {1}. The issue object is null.", ProjectName, HostUrl);
                    return false;
                }
            }
            catch (AggregateException ae)
            {
                AuditEnvironment.Error(ae, "Error creating new issue for project {0} at host url {1}.", ProjectName, HostUrl);
                return false;
            }
            catch (Exception e)
            {
                AuditEnvironment.Error(e, "Error creating new issue for project {0} at host url {1}.", ProjectName, HostUrl);
                return false;
            }
        }

        protected override void PrintMessage(ConsoleColor color, string format, params object[] args)
        {
            IssueText.AppendFormat(format, args);
        }

        protected override void PrintMessage(string format, params object[] args)
        {
            IssueText.AppendFormat(format, args);
        }

        protected override void PrintMessageLine(ConsoleColor color, string format, params object[] args)
        {
            IssueText.AppendFormat(format, args);
            IssueText.AppendLine();
        }

        protected override void PrintMessageLine(string format, params object[] args)
        {
            IssueText.AppendFormat(format, args);
            IssueText.AppendLine();
        }

        protected override void PrintMessageLine(string format)
        {
            IssueText.AppendFormat(format);
            IssueText.AppendLine();
        }
        #endregion

        #region Properties
        protected string HostUrl { get; set; }
        protected string Token { get; set; }
        protected string ProjectName { get; set; }
        protected Project Project { get; set; }
        protected string IssueTitle { get; set; }
        protected StringBuilder IssueText { get; set; } = new StringBuilder();
        #endregion


    }
}

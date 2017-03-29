using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Octokit;

namespace DevAudit.AuditLibrary
{
    public class GitHubIssueReporter : AuditReporter
    {
        #region Constructors
        public GitHubIssueReporter(PackageSource source) : base(source) {} 
        #endregion

        #region Overriden methods
        public override async Task<bool> ReportPackageSourceAudit()
        {
            if (!AuditOptions.ContainsKey("GitHubReportOwner") || !AuditOptions.ContainsKey("GitHubReportName") || !AuditOptions.ContainsKey("GitHubToken"))
            {
                throw new ArgumentException("The GitHubReportOwner, GitHubReportName, and GitHubReportOwner audit options must be present.");
            }
            if (AuditOptions.ContainsKey("GitHubReportTitle"))
            {
                IssueTitle = (string)AuditOptions["GitHubReportTitle"];
            }
            else
            {
                IssueTitle = string.Format("[DevAudit] {2} audit on {0} {1}", DateTime.UtcNow.ToShortDateString(), DateTime.UtcNow.ToShortTimeString(), Source.PackageManagerLabel);
            }
            GitHubClient client;
            client = new GitHubClient(new ProductHeaderValue("DevAudit"));
            client.Credentials = new Credentials((string) AuditOptions["GitHubToken"]);
            Repository repository;
            try
            {
                repository = await client.Repository.Get((string) AuditOptions["GitHubReportOwner"], (string) AuditOptions["GitHubReportName"]);
            }
            catch (Exception)
            {
                AuditEnvironment.Warning("Could not get repository {0}/{1}.", (string) AuditOptions["GitHubReportOwner"], (string) AuditOptions["GitHubReportName"]);
            }
            NewIssue issue = new NewIssue(IssueTitle);
            BuildPackageSourceAuditReport();
            issue.Body = IssueText.ToString();
            try
            {
                Issue i = await client.Issue.Create((string)AuditOptions["GitHubReportOwner"], (string)AuditOptions["GitHubReportName"], issue);
                
                AuditEnvironment.Info("Created issue #{0} {1} in GitHub repository {2}/{3}.", i.Number, IssueTitle, (string)AuditOptions["GitHubReportOwner"], (string)AuditOptions["GitHubReportName"]);
            }
            catch (AggregateException ae)
            {
                AuditEnvironment.Error(ae, "Error creating new issue for repository {0}/{1}.", (string)AuditOptions["GitHubReportOwner"], (string)AuditOptions["GitHubReportName"]);
                return false;
            }
            catch (Exception e)
            {
                AuditEnvironment.Error(e, "Error creating new issue for repository {0}/{1}.", (string)AuditOptions["GitHubReportOwner"], (string)AuditOptions["GitHubReportName"]);
                return false;
            }
            return true;
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
        protected string IssueTitle { get; set; }
        protected StringBuilder IssueText { get; set; } = new StringBuilder();
        #endregion


    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SharpBucket.V2;
using SharpBucket.V2.EndPoints;
using SharpBucket.V2.Pocos;
namespace DevAudit.AuditLibrary
{
    public class BitBucketIssueReporter : AuditReporter 
    {
        #region Constructors
        public BitBucketIssueReporter(PackageSource source) : base(source) { }
        #endregion

        
        #region Overriden methods
        public override Task<bool> ReportPackageSourceAudit()
        {
            if (!AuditOptions.ContainsKey("BitBucketReportAccount") || !AuditOptions.ContainsKey("BitBucketReportName") || !AuditOptions.ContainsKey("BitBucketKey"))
            {
                throw new ArgumentException("The BitBucketReportAccount, BitBucketReportName, and BitBucketReportKey audit options must be present.");
            }
            string key = (string) AuditOptions["BitBucketKey"];
            string[] k = key.Split('|');
            if (k.Count() != 2)
            {
                throw new ArgumentException("The BitBucketReportKey audit option must have the format consumer_key|secret.");
            }
            string consumer = k[0], secret = k[1];
            string account = (string) AuditOptions["BitBucketReportAccount"];
            string repository = (string) AuditOptions["BitBucketReportName"];

            if (AuditOptions.ContainsKey("BitBucketReportTitle"))
            {
                IssueTitle = (string)AuditOptions["BitBucketReportTitle"];
            }
            else
            {
                IssueTitle = string.Format("[DevAudit] {0} audit on {1} {2}", Source.PackageManagerLabel, DateTime.UtcNow.ToShortDateString(), DateTime.UtcNow.ToShortTimeString());
            }
            SharpBucketV2 sharp_bucket = new SharpBucketV2();
            sharp_bucket.OAuth2LeggedAuthentication(consumer, secret);
            RepositoriesEndPoint repository_endpoint = sharp_bucket.RepositoriesEndPoint(account, repository);
            IssuesResource r;
            try
            {
                r = repository_endpoint.IssuesResource();
            }
            catch (Exception e)
            {
                AuditEnvironment.Error(e, "Could not get issues resource for repository {0}/{1}.", account, repository);
                return Task.FromResult(false);
            }
            BuildPackageSourceAuditReport();
            Issue issue = new Issue()
            {
                title = IssueTitle,
                content = IssueText.ToString(),
                status = "new",
                priority = "major",
                kind = "bug"
            };
            try
            {
                Issue i = r.PostIssue(issue);
                if (i == null)
                {
                    AuditEnvironment.Error("Could not post issue to repository {0}/{1}.", account, repository);
                    return Task.FromResult(false);
                }
                else
                {
                    AuditEnvironment.Success("Created issue {0} at {1}.", i.title, i.resource_uri);
                }
            }
            catch (Exception e)
            {
                AuditEnvironment.Error(e, "Could not post issue to repository {0}/{1}.", account, repository);
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
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

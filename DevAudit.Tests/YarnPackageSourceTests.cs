using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

using Xunit;
using Sprache;
using Versatile;

using DevAudit.AuditLibrary;

namespace DevAudit.Tests
{
    public class Yarn : PackageSourceTests 
    {
        protected override PackageSource Source { get; } = 
            new YarnPackageSource(new Dictionary<string, object>()
            { 
                {"File", @".\Examples\package.json.2" },
                {"LockFile", @".\Examples\yarn.lock.2"}
            
            }, EnvironmentMessageHandler);
        
        [Fact]
        public override void CanTestVulnerabilityVersionInPackageVersionRange()
        {
            Assert.True(Source.IsVulnerabilityVersionInPackageVersionRange(">1.2", "<1.5.2"));
            Assert.True(Source.IsVulnerabilityVersionInPackageVersionRange("<=4.3", "<4.2"));
            Assert.True(Source.IsVulnerabilityVersionInPackageVersionRange(">=1.2.2", ">1.2.0-alpha.0"));
            Assert.True(Source.IsVulnerabilityVersionInPackageVersionRange(">12.2.2", "<=20.0.0"));
        }

        [Fact]
        public void YarnIsDeveloperPackageSource()
        {
            Assert.NotNull(DPS);
        }

        [Fact]
        public void CanTestPackageVersionIsRange()
        {
            Assert.False(DPS.PackageVersionIsRange("6.4.2"));
            Assert.True(DPS.PackageVersionIsRange("~5.1"));
            Assert.False(DPS.PackageVersionIsRange("=7.3"));
        }
        
        [Fact]
        public override void CanGetMinimumPackageVersion()
        {
            var comparatorSets = SemanticVersion.Grammar.Range.Parse("6.1.0");
            Assert.NotEmpty(comparatorSets);
            Assert.Single(comparatorSets);
            var comparatorSet = comparatorSets.Single();
            var a = comparatorSet[0];
            Assert.Equal(ExpressionType.Equal, a.Operator);

            comparatorSet = SemanticVersion.Grammar.Range.Parse("<2.0.0").Single();
            Assert.Equal(2, comparatorSet.Count);
            Assert.Contains(comparatorSet, c => c.Operator == ExpressionType.LessThan);
            
            var max = comparatorSet.Single(c => c.Operator == ExpressionType.LessThan);
            Assert.Equal(2, max.Version.Major);
            var min = comparatorSet.Single(c => c.Operator == ExpressionType.GreaterThan);
            Assert.Equal(0, min.Version.Major);
             
            comparatorSet = SemanticVersion.Grammar.Range.Parse("<=3.1.4").Single();
            Assert.Equal(2, comparatorSet.Count);
            Assert.Contains(comparatorSet, c => c.Operator == ExpressionType.LessThanOrEqual);
            Assert.Contains(comparatorSet, c => c.Operator == ExpressionType.GreaterThan);

            max = comparatorSet.Single(c => c.Operator == ExpressionType.LessThanOrEqual);
            Assert.Equal(3, max.Version.Major);
            min = comparatorSet.Single(c => c.Operator == ExpressionType.GreaterThan);
            Assert.Equal(0, min.Version.Major);
             
            comparatorSet = SemanticVersion.Grammar.Range.Parse(">0.4.2").Single();
            Assert.Equal(2, comparatorSet.Count);
            Assert.Contains(comparatorSet, c => c.Operator == ExpressionType.GreaterThan);
            Assert.Contains(comparatorSet, c => c.Operator == ExpressionType.LessThan);

            max = comparatorSet.Single(c => c.Operator == ExpressionType.LessThan);
            Assert.Equal(1000000, max.Version.Major);
            min = comparatorSet.Single(c => c.Operator == ExpressionType.GreaterThan);
            Assert.Equal(4, min.Version.Minor);

            comparatorSet = SemanticVersion.Grammar.Range.Parse(">=2.7.1").Single();
            Assert.Equal(2, comparatorSet.Count);
            Assert.Contains(comparatorSet, c => c.Operator == ExpressionType.GreaterThanOrEqual);
            Assert.Contains(comparatorSet, c => c.Operator == ExpressionType.LessThan);

            max = comparatorSet.Single(c => c.Operator == ExpressionType.LessThan);
            Assert.Equal(1000000, max.Version.Major);
            min = comparatorSet.Single(c => c.Operator == ExpressionType.GreaterThanOrEqual);
            Assert.Equal(1, min.Version.Patch);

            comparatorSet = SemanticVersion.Grammar.Range.Parse("=4.6.6").Single();
            Assert.Single(comparatorSet);
            Assert.Contains(comparatorSet, c => c.Operator == ExpressionType.Equal);
        }
    }
}

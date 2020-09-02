using System.Collections.Generic;

namespace RepoExplorer
{
    public class MilestoneInfo
    {
        public MilestoneInfo(string title, IEnumerable<IssueInfo> issues)
        {
            Title = title;
            Issues.AddRange(issues);
        }

        public string Title { get; }
        public List<IssueInfo> Issues { get; } = new List<IssueInfo>();
    }
}
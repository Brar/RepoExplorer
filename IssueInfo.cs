using System.Collections.Generic;

namespace RepoExplorer
{
    public class IssueInfo
    {
        public IssueInfo(int number, bool isClosed)
        {
            Number = number;
            IsClosed = isClosed;
        }

        public int Number { get; }
        public bool IsClosed { get; }
        public List<string> Assignees { get; } = new();
    }
}
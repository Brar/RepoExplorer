using System.Collections.Generic;

namespace RepoExplorer
{
    public class IssueInfo(int number, bool isClosed)
    {
        public int Number { get; } = number;
        public bool IsClosed { get; } = isClosed;
        public List<string> Assignees { get; } = new();
    }
}
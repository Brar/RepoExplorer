using System.Collections.Generic;

namespace RepoExplorer
{
    public class RepositoryInfo
    {
        public RepositoryInfo(string name, IEnumerable<MilestoneInfo> milestones)
        {
            Name = name;
            Milestones.AddRange(milestones);
        }

        public string Name { get; }
        public List<MilestoneInfo> Milestones { get; } = new();
    }
}
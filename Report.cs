using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace RepoExplorer
{
    public class Report(string user, TextWriter output) : IAsyncDisposable
    {
        volatile bool _disposed;

        public async Task WriteRepositoryInfo(RepositoryInfo repositoryInfo)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(Report));

            await output.WriteLineAsync($"## Repository {repositoryInfo.Name}:{Environment.NewLine}");
            foreach (var milestone in repositoryInfo.Milestones.Where(m=>m.Issues.Any(i=> i.IsClosed && i.Assignees.Any() && Version.TryParse(m.Title, out _))).OrderByDescending(m => Version.Parse(m.Title)))
            {
                await output.WriteLineAsync($"### [Milestone {milestone.Title}](https://github.com/{user}/{repositoryInfo.Name}/issues?q=is%3Aissue+milestone%3A{HttpUtility.UrlEncode(milestone.Title)}){Environment.NewLine}");

                var assignees = milestone.Issues.Where(i => i.IsClosed).SelectMany(i => i.Assignees).Distinct().ToArray();
                List<(string assignee, int contributions)> stats = new List<(string assignees, int contributions)>(assignees.Length);

                foreach (var assignee in assignees)
                {
                    stats.Add((assignee, milestone.Issues.Count(i => i.IsClosed && i.Assignees.Contains(assignee))));
                }


                var assigneeContributionUrls = new List<(string assigneeUrl, string contributionsUrl)>();
                var maxAssigneeUrlLength = 0;
                var maxContributionsUrlLength = 0;
                foreach (var (assignee, contributions) in stats.OrderByDescending(s => s.contributions).ThenBy(s => s.assignee))
                {
                    var assigneeUrl = $"[@{assignee}](https://github.com/{assignee})";
                    var contributionsUrl = $"[{contributions}](https://github.com/{user}/{repositoryInfo.Name}/issues?q=is%3Aissue+milestone%3A{HttpUtility.UrlEncode(milestone.Title)}+is%3Aclosed+assignee%3A{HttpUtility.UrlEncode(assignee)})";
                    maxAssigneeUrlLength = Math.Max(maxAssigneeUrlLength, assigneeUrl.Length);
                    maxContributionsUrlLength = Math.Max(maxContributionsUrlLength, contributionsUrl.Length);
                    assigneeContributionUrls.Add((assigneeUrl, contributionsUrl));
                }

                await output.WriteLineAsync($"| Contributor{new(' ', maxAssigneeUrlLength - "Contributor".Length)} | Assigned issues{new(' ', maxContributionsUrlLength - "Assigned issues".Length)} |");
                await output.WriteLineAsync($"| {new('-', maxAssigneeUrlLength)} | {new('-', maxContributionsUrlLength)}:|");
                foreach (var (assigneeUrl, contributionsUrl) in assigneeContributionUrls)
                {
                    await output.WriteLineAsync($"| {assigneeUrl}{new(' ', maxAssigneeUrlLength - assigneeUrl.Length)} | {contributionsUrl}{new(' ', maxContributionsUrlLength - contributionsUrl.Length)} |");
                }

                await output.WriteLineAsync();
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
                return;

            _disposed = true;
            if(output != Console.Out)
            {
                await output.FlushAsync();
                output.Close();
                await output.DisposeAsync();
            }
        }
    }
}

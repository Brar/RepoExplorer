using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace RepoExplorer
{
    public class Report : IAsyncDisposable
    {
        readonly string _user;
        readonly TextWriter _output;
        volatile bool _disposed;

        public Report(string user, TextWriter output)
        {
            _user = user;
            _output = output;
        }

        public async Task WriteRepositoryInfo(RepositoryInfo repositoryInfo)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(Report));

            await _output.WriteLineAsync($"## Repository {repositoryInfo.Name}:{Environment.NewLine}");
            foreach (var milestone in repositoryInfo.Milestones.Where(m=>m.Issues.Any(i=> i.IsClosed && i.Assignees.Any() && Version.TryParse(m.Title, out _))).OrderByDescending(m => Version.Parse(m.Title)))
            {
                await _output.WriteLineAsync($"### [Milestone {milestone.Title}](https://github.com/{_user}/{repositoryInfo.Name}/issues?q=is%3Aissue+milestone%3A{HttpUtility.UrlEncode(milestone.Title)}){Environment.NewLine}");

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
                    var contributionsUrl = $"[{contributions}](https://github.com/{_user}/{repositoryInfo.Name}/issues?q=is%3Aissue+milestone%3A{HttpUtility.UrlEncode(milestone.Title)}+is%3Aclosed+assignee%3A{HttpUtility.UrlEncode(assignee)})";
                    maxAssigneeUrlLength = Math.Max(maxAssigneeUrlLength, assigneeUrl.Length);
                    maxContributionsUrlLength = Math.Max(maxContributionsUrlLength, contributionsUrl.Length);
                    assigneeContributionUrls.Add((assigneeUrl, contributionsUrl));
                }

                await _output.WriteLineAsync($"| Contributor{new(' ', maxAssigneeUrlLength - "Contributor".Length)} | Assigned issues{new(' ', maxContributionsUrlLength - "Assigned issues".Length)} |");
                await _output.WriteLineAsync($"| {new('-', maxAssigneeUrlLength)} | {new('-', maxContributionsUrlLength)}:|");
                foreach (var (assigneeUrl, contributionsUrl) in assigneeContributionUrls)
                {
                    await _output.WriteLineAsync($"| {assigneeUrl}{new(' ', maxAssigneeUrlLength - assigneeUrl.Length)} | {contributionsUrl}{new(' ', maxContributionsUrlLength - contributionsUrl.Length)} |");
                }

                await _output.WriteLineAsync();
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
                return;

            _disposed = true;
            if(_output != Console.Out)
            {
                await _output.FlushAsync();
                _output.Close();
                await _output.DisposeAsync();
            }
        }
    }
}

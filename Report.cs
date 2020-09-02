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
        private readonly TextWriter _output;
        private volatile bool _disposed;

        public Report(TextWriter output)
        {
            _output = output;
        }

        public async Task WriteRepositoryInfo(RepositoryInfo repositoryInfo)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(Report));

            await _output.WriteLineAsync($"## Repository {repositoryInfo.Name}:{Environment.NewLine}");
            foreach (var milestone in repositoryInfo.Milestones.Where(m=>m.Issues.Any(i=> i.IsClosed && i.Assignees.Any())).OrderByDescending(m => m.Title))
            {
                await _output.WriteLineAsync($"### [Milestone {milestone.Title}](https://github.com/npgsql/npgsql/issues?q=is%3Aissue+milestone%3A{HttpUtility.UrlEncode(milestone.Title)}):{Environment.NewLine}");

                var assignees = milestone.Issues.Where(i => i.IsClosed).SelectMany(i => i.Assignees).Distinct().ToArray();
                List<(string assignee, int contributions)> stats = new List<(string assignees, int contributions)>(assignees.Length);

                foreach (var assignee in assignees)
                {
                    stats.Add((assignee, milestone.Issues.Count(i => i.IsClosed && i.Assignees.Contains(assignee))));
                }

                await _output.WriteLineAsync("| Contributor                                                                        | Assigned issues                                                                                                         |");
                await _output.WriteLineAsync("| ---------------------------------------------------------------------------------- | -----------------------------------------------------------------------------------------------------------------------:|");
                foreach (var (assignee, contributions) in stats.OrderByDescending(s => s.contributions).ThenBy(s => s.assignee))
                {
                    var assigneeUrl = $"[@{assignee}](https://github.com/{assignee})";
                    var contributionsUrl = $"[{contributions}](https://github.com/npgsql/npgsql/issues?q=is%3Aissue+milestone%3A{HttpUtility.UrlEncode(milestone.Title)}+is%3Aclosed+assignee%3A{HttpUtility.UrlEncode(assignee)})";
                    await _output.WriteLineAsync($"| {assigneeUrl,-82} | {contributionsUrl,119} |");
                }

                await _output.WriteLineAsync();
                await _output.WriteLineAsync();
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
                return;

            _disposed = true;
            await _output.FlushAsync();
            _output.Close();
            await _output.DisposeAsync();
        }
    }
}

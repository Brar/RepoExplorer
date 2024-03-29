﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Octokit.GraphQL;
using static Octokit.GraphQL.Variable;

namespace RepoExplorer
{
    public class GitHubInfo(string login, string accessToken)
    {
        static readonly ICompiledQuery<IEnumerable<string>>? AllRepositoryNamesQuery = new Query()
            .RepositoryOwner(Var("owner"))
            .Repositories()
            .AllPages()
            .Select(r => r.Name).Compile();

        static readonly ICompiledQuery<IEnumerable<MilestoneInfo>>? MilestonesWithIssueNumbersQuery = new Query()
            .RepositoryOwner(Var("owner"))
            .Repository(Var("repository"))
            .Milestones()
            .AllPages()
            .Select(m => new MilestoneInfo
            (
                m.Title,
                m.Issues(null, null, null, null, null, null, null, null)
                    .AllPages()
                    .Select(i => new IssueInfo(i.Number, i.Closed)).ToList()
            )).Compile();

        static readonly ICompiledQuery<List<string>>? AssigneesQuery = new Query()
            .RepositoryOwner(Var("owner"))
            .Repository(Var("repository"))
            .Issue(Var("issueNumber"))
            .Select(i => i.Assignees(null, null, null, null).AllPages().Select(a => a.Login).ToList()).Compile();

        readonly Connection _connection = new(
            new(nameof(RepoExplorer),
                typeof(Program).Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version),
            accessToken);
        readonly Dictionary<string, object> _defaultVariables = new()
        {
            {"owner", login}
        };

        public async IAsyncEnumerable<RepositoryInfo> GetRepositoryInfos(string? repository, string? milestone)
        {
            if (repository == null)
            {
                await Console.Out.WriteLineAsync("Fetching repository names...");
                foreach (var repoName in await _connection.Run(AllRepositoryNamesQuery, _defaultVariables))
                {
                    await Console.Out.WriteLineAsync($"Fetching milestone information for repository {repoName}...");
                    yield return await GetRepositoryInfoWithAssignees(repoName,
                        await _connection.Run(MilestonesWithIssueNumbersQuery,
                            new(_defaultVariables) {{"repository", repoName}}));
                }
            }
            else
            {
                await Console.Out.WriteLineAsync($"Fetching milestone information for repository {repository}...");
                yield return await GetRepositoryInfoWithAssignees(repository,
                    await _connection.Run(MilestonesWithIssueNumbersQuery,
                        new(_defaultVariables) {{"repository", repository}}));
            }

            async Task<RepositoryInfo> GetRepositoryInfoWithAssignees(string repositoryName, IEnumerable<MilestoneInfo> milestones)
            {
                var mi = milestones.ToArray();
                foreach (var m in mi)
                {
                    if (milestone != null && !Regex.IsMatch(m.Title, milestone))
                        continue;

                    await Console.Out.WriteLineAsync($"Fetching issue information for milestone {m.Title}...");
                    foreach (var issue in m.Issues)
                    {
                        await Console.Out.WriteLineAsync($"Fetching assignee information for issue #{issue.Number}...");
                        issue.Assignees.AddRange(await _connection.Run(AssigneesQuery,
                            new(_defaultVariables)
                                {{"repository", repositoryName}, {"issueNumber", issue.Number}}));
                    }
                }

                return new(repositoryName, mi);
            }
        }
    }
}

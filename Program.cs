using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace RepoExplorer
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            var userOption =
                new Option<string>(new[] { "--user", "-u" }, "The user whose repositories should be queried");
            var repositoryOption = new Option<string?>(new[] { "--repository", "-r" }, "The repository to be queried");
            var milestoneOption = new Option<string?>(new[] { "--milestone", "-m" }, "The milestone to be queried");
            var outputFileOption =
                new Option<FileInfo?>(new[] { "--output-file", "-o" }, "Write to this file instead of STDOUT");
            var rootCommand = new RootCommand("Github repository explorer");
            rootCommand.AddOption(userOption);
            rootCommand.AddOption(repositoryOption);
            rootCommand.AddOption(milestoneOption);
            rootCommand.AddOption(outputFileOption);

            rootCommand.SetHandler(Run, userOption, repositoryOption, milestoneOption, outputFileOption);
            return await rootCommand.InvokeAsync(args);
        }

        static async Task<int> Run(string user, string? repository, string? milestone, FileInfo? outputFile)
        {
            if (string.IsNullOrWhiteSpace(user))
            {
                return 1;
            }
            try
            {
                await Console.Out.WriteAsync("Please enter your access token: ");
                var accessToken = GetAccessTokenFromConsole();
                await Console.Out.WriteLineAsync();

                var github = new GitHubInfo(user, accessToken);
                await using var report = new Report(user, outputFile != null ? new StreamWriter(outputFile.OpenWrite(), Encoding.UTF8) : Console.Out);

                await foreach (var repoInfo in github.GetRepositoryInfos(repository, milestone))
                {
                    await report.WriteRepositoryInfo(repoInfo);
                }

                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return -1;
            }

            static string GetAccessTokenFromConsole()
            {
                var accessToken = new StringBuilder(40);
                var ki = Console.ReadKey(true);
                while (ki.Key != ConsoleKey.Enter)
                {
                    if (ki.Key == ConsoleKey.Backspace && accessToken.Length > 0)
                    {
                        Console.Write("\b \b");
                        accessToken.Length -= 1;
                    }
                    else
                    {
                        var c = ki.KeyChar;
                        if (!char.IsControl(c))
                        {
                            accessToken.Append(c);
                        }
                    }
                    ki = Console.ReadKey(true);
                }
                return accessToken.ToString();
            }
        }
    }
}

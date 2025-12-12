using System.Diagnostics;
using System.Text.Json;
using CommandCentre.Models;

namespace CommandCentre.Services;

public class GitHubService
{
    private readonly RepoService _repoService;

    public GitHubService(RepoService repoService)
    {
        _repoService = repoService;
    }

    public async Task<bool> IsAuthenticated()
    {
        try
        {
            // First check if gh is installed
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = "-c \"which gh\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var whichOutput = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (string.IsNullOrEmpty(whichOutput))
            {
                Console.WriteLine("GitHub CLI (gh) is not installed");
                return false;
            }

            var ghPath = whichOutput.Trim();
            var authProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ghPath,
                    Arguments = "auth status",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            authProcess.Start();
            var output = await authProcess.StandardOutput.ReadToEndAsync();
            var error = await authProcess.StandardError.ReadToEndAsync();
            await authProcess.WaitForExitAsync();

            var fullOutput = output + error;
            
            var isAuthenticated = fullOutput.Contains("Logged in") || 
                                  fullOutput.Contains("✓") ||
                                  authProcess.ExitCode == 0;
            
            Console.WriteLine($"GitHub authenticated: {isAuthenticated}");
            return isAuthenticated;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking GitHub auth: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return false;
        }
    }

    public async Task<List<PullRequest>> GetMyOpenPullRequests()
    {
        var allPRs = new List<PullRequest>();
        var repos = _repoService.GetAllRepos();

        foreach (var repo in repos)
        {
            try
            {
                var prs = await GetRepoPullRequests(repo);
                allPRs.AddRange(prs);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching PRs for {repo.Name}: {ex.Message}");
            }
        }

        return allPRs.OrderByDescending(pr => pr.CreatedAt).ToList();
    }

    private async Task<List<PullRequest>> GetRepoPullRequests(RepoInfo repo)
    {
        var prs = new List<PullRequest>();

        // Get current git user
        var gitUser = await RunGitCommand(repo.Path, "config user.name");
        if (string.IsNullOrEmpty(gitUser))
        {
            Console.WriteLine($"No git user found for {repo.Name}");
            return prs;
        }

        Console.WriteLine($"Git user for {repo.Name}: {gitUser.Trim()}");

        // Get remote URL to determine GitHub repo
        var remoteUrl = await RunGitCommand(repo.Path, "config --get remote.origin.url");
        if (string.IsNullOrEmpty(remoteUrl))
        {
            Console.WriteLine($"No remote URL found for {repo.Name}");
            return prs;
        }

        Console.WriteLine($"Remote URL for {repo.Name}: {remoteUrl}");

        // Parse GitHub owner/repo from URL
        var (owner, repoName) = ParseGitHubUrl(remoteUrl);
        if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repoName))
        {
            Console.WriteLine($"Could not parse GitHub URL for {repo.Name}");
            return prs;
        }

        Console.WriteLine($"Fetching PRs for {owner}/{repoName}");

        // Use Git Hub CLI to fetch PRs
        var ghCommand = $"pr list --author \"{gitUser.Trim()}\" --state open --json number,title,url,statusCheckRollup,createdAt --repo {owner}/{repoName}";
        var output = await RunCommand("gh", ghCommand);

        if (string.IsNullOrEmpty(output))
        {
            Console.WriteLine($"No PR output for {repo.Name}");
            return prs;
        }

        Console.WriteLine($"PR JSON output: {output}");

        try
        {
            var prData = JsonSerializer.Deserialize<List<GitHubPR>>(output);
            if (prData == null)
            {
                Console.WriteLine($"Failed to deserialize PRs for {repo.Name}");
                return prs;
            }

            Console.WriteLine($"Found {prData.Count} PRs for {repo.Name}");

            foreach (var pr in prData)
            {
                var buildStatus = DetermineBuildStatus(pr.StatusCheckRollup);
                prs.Add(new PullRequest
                {
                    Title = pr.Title,
                    Repo = repo.Name,
                    Number = pr.Number,
                    Author = gitUser.Trim(),
                    Url = pr.Url,
                    Status = "open",
                    HasFailedChecks = buildStatus == "failure",
                    BuildStatus = buildStatus,
                    CreatedAt = pr.CreatedAt
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing PR data for {repo.Name}: {ex.Message}");
        }

        return prs;
    }

    private string DetermineBuildStatus(List<StatusCheck>? checks)
    {
        if (checks == null || !checks.Any()) return "unknown";

        if (checks.Any(c => c.State == "FAILURE" || c.State == "ERROR"))
            return "failure";
        
        if (checks.Any(c => c.State == "PENDING" || c.State == "IN_PROGRESS"))
            return "pending";
        
        if (checks.All(c => c.State == "SUCCESS"))
            return "success";

        return "unknown";
    }

    private (string owner, string repo) ParseGitHubUrl(string url)
    {
        try
        {
            if (url.StartsWith("git@github.com:"))
            {
                var parts = url.Replace("git@github.com:", "").Replace(".git", "").Split('/');
                return (parts[0], parts[1]);
            }
            else if (url.Contains("github.com"))
            {
                var uri = new Uri(url.Replace(".git", ""));
                var segments = uri.AbsolutePath.Trim('/').Split('/');
                if (segments.Length >= 2)
                {
                    return (segments[0], segments[1]);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing GitHub URL: {ex.Message}");
        }

        return ("", "");
    }

    private async Task<string> RunGitCommand(string repoPath, string arguments)
    {
        return await RunCommand("git", $"-C \"{repoPath}\" {arguments}");
    }

    private async Task<string> RunCommand(string command, string arguments)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            return output.Trim();
        }
        catch
        {
            return "";
        }
    }

    private class GitHubPR
    {
        public int Number { get; set; }
        public string Title { get; set; } = "";
        public string Url { get; set; } = "";
        public List<StatusCheck>? StatusCheckRollup { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    private class StatusCheck
    {
        public string State { get; set; } = "";
    }
}
using System.Diagnostics;
using System.Text.Json;
using CommandCentre.Models;

namespace CommandCentre.Services;

public class GitHubService
{
    private readonly RepoService _repoService;

    private List<PullRequest> allPRs = new List<PullRequest>();
    private List<PRLoadStatus> statuses = new List<PRLoadStatus>();

    public GitHubService(RepoService repoService)
    {
        _repoService = repoService;
    }

    public async Task<bool> CheckAuthenicationStatus()
    {
        try
        {
            // First check if gh is installed (cross-platform)
            string ghPath = "";
            if (OperatingSystem.IsWindows())
            {
                // Use 'where' command on Windows
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = "/C where gh",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                var whichOutput = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();
                if (string.IsNullOrWhiteSpace(whichOutput))
                {
                    Console.WriteLine("GitHub CLI (gh) is not installed");
                    return false;
                }
                ghPath = whichOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "gh";
            }
            else
            {
                // Use 'which' command on Unix
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
                if (string.IsNullOrWhiteSpace(whichOutput))
                {
                    Console.WriteLine("GitHub CLI (gh) is not installed");
                    return false;
                }
                ghPath = whichOutput.Trim();
            }

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

            return fullOutput.Contains("Logged in") ||
                                  fullOutput.Contains("✓") ||
                                  authProcess.ExitCode == 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking GitHub auth: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return false;
        }
    }

    public async Task<(List<PullRequest> prs, List<PRLoadStatus> statuses)> GetMyOpenPullRequests()
    {
        allPRs.Clear();
        statuses.Clear();

        var repos = _repoService.GetAllRepos();

        foreach (var repo in repos)
        {
            try
            {
                var (prs, status) = await GetRepoPullRequests(repo);
                allPRs.AddRange(prs);
                if (status != null)
                {
                    statuses.Add(status);
                }
            }
            catch (Exception ex)
            {
                statuses.Add(new PRLoadStatus
                {
                    RepoName = repo.Name,
                    Success = false,
                    Message = $"Error: {ex.Message}"
                });
            }
        }

        return (allPRs.OrderByDescending(pr => pr.CreatedAt).ToList(), statuses);
    }

    private async Task<(List<PullRequest> prs, PRLoadStatus? status)> GetRepoPullRequests(RepoInfo repo)
    {
        var prs = new List<PullRequest>();
        
        // Check if repo path exists
        if (!_repoService.HasActiveDirectory(repo) && repo.Type != "wsl") // wsl repos may use ~ paths
        {
            return (prs, new PRLoadStatus
            {
                RepoName = repo.Name,
                Success = false,
                Message = "No active directory found"
            });
        }

        // Get current git user
        var gitUser = await RunGitCommand(repo.Path, "config user.name");
        gitUser = "Matt-Penney"; // temp hardcode as this is correct value
        if (string.IsNullOrEmpty(gitUser))
        {
            return (prs, new PRLoadStatus
            {
                RepoName = repo.Name,
                Success = false,
                Message = "No git user configured"
            });
        }

        // Get remote URL to determine GitHub repo
        var remoteUrl = await RunGitCommand(repo.Path, "config --get remote.origin.url");
        
        // temp fix for wsl and customer-portal
        if (repo.Name == "customer-portal" && repo.Type == "wsl")
        {
            remoteUrl = await RunGitCommand("C://Repos//customer-portal", "config --get remote.origin.url");
        }
        if (string.IsNullOrEmpty(remoteUrl))
        {
            return (prs, new PRLoadStatus
            {
                RepoName = repo.Name,
                Success = false,
                Message = "No remote URL found"
            });
        }

        // Parse GitHub owner/repo from URL
        var (owner, repoName) = ParseGitHubUrl(remoteUrl);
        if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repoName))
        {
            return (prs, new PRLoadStatus
            {
                RepoName = repo.Name,
                Success = false,
                Message = "Could not parse GitHub URL"
            });
        }

        // Use GitHub CLI to fetch PRs
        var ghCommand = $"pr list --author \"@me\" --state \"open\" --json \"number,title,url,statusCheckRollup,createdAt\" --repo {owner}/{repoName}";
        var output = await RunCommand("gh", ghCommand);

        if (string.IsNullOrEmpty(output))
        {
            return (prs, new PRLoadStatus
            {
                RepoName = repo.Name,
                Success = true,
                Message = "No open PRs"
            });
        }

        try
        {
            var prData = JsonSerializer.Deserialize<List<GitHubPR>>(output);
            if (prData == null)
            {
                return (prs, new PRLoadStatus
                {
                    RepoName = repo.Name,
                    Success = false,
                    Message = "Failed to parse PR data"
                });
            }
            else if (prData.Count == 0)
            {
                return (prs, new PRLoadStatus
                {
                    RepoName = repo.Name,
                    Success = true,
                    Message = "No open PRs"
                });
            }

            foreach (var pr in prData)
            {
                var buildStatus = DetermineBuildStatus(pr.statusCheckRollup);
                prs.Add(new PullRequest
                {
                    Title = pr.title,
                    Repo = repo.Name,
                    Number = pr.number,
                    Author = gitUser.Trim(),
                    Url = pr.url,
                    Status = "open",
                    HasFailedChecks = buildStatus == "failure",
                    BuildStatus = buildStatus,
                    CreatedAt = pr.createdAt
                });
            }

            return (prs, new PRLoadStatus
            {
                RepoName = repo.Name,
                Success = true,
                Message = $"Loaded {prs.Count} PR(s)"
            });
        }
        catch (Exception ex)
        {
            return (prs, new PRLoadStatus
            {
                RepoName = repo.Name,
                Success = false,
                Message = $"Parse error: {ex.Message}"
            });
        }
    }

    private string DetermineBuildStatus(List<StatusCheck>? checks)
    {
        if (checks == null || !checks.Any()) return "unknown";

        if (checks.Any(c => c.conclusion == "FAILURE" || c.conclusion == "ERROR"))
            return "failure";
        
        if (checks.Any(c => c.conclusion == "PENDING" || c.conclusion == "IN_PROGRESS"))
            return "pending";
        
        if (checks.All(c => c.conclusion == "SUCCESS"))
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
        public int number { get; set; }
        public string title { get; set; } = "";
        public string url { get; set; } = "";
        public List<StatusCheck>? statusCheckRollup { get; set; }
        public DateTime createdAt { get; set; }
    }

    private class StatusCheck
    {
        public string conclusion { get; set; } = "";
    }
}
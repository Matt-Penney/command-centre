using System.Text.Json;
using System.Diagnostics;
using CommandCentre.Models;

namespace CommandCentre.Services;

public class RepoService
{
    private readonly string _configPath;
    private List<RepoInfo> _repos;
    
    public RepoService()
    {
        var projectDirectory = Directory.GetCurrentDirectory();
        _configPath = Path.Combine(projectDirectory, "repos.json");

        _repos = LoadRepos();
    }
    
    public List<RepoInfo> GetAllRepos() => new List<RepoInfo>(_repos);
    
    public RepoInfo? GetRepoByName(string name) => _repos.FirstOrDefault(r => r.Name == name);
    
    public RepoInfo? GetRepoByPath(string path) => _repos.FirstOrDefault(r => r.Path == path);
    
    public void AddRepo(RepoInfo repo)
    {
        _repos.Add(repo);
        SaveRepos();
    }
    
    public void RemoveRepo(string name)
    {
        _repos.RemoveAll(r => r.Name == name);
        SaveRepos();
    }
    
    public void UpdateRepo(RepoInfo repo)
    {
        var existing = _repos.FirstOrDefault(r => r.Name == repo.Name);
        if (existing != null)
        {
            existing.Path = repo.Path;
            existing.Type = repo.Type;
            SaveRepos();
        }
    }
    
    private List<RepoInfo> LoadRepos()
    {
        try
        {
            if (File.Exists(_configPath))
            {
                var json = File.ReadAllText(_configPath);
                return JsonSerializer.Deserialize<List<RepoInfo>>(json) ?? GetDefaultRepos();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading repos config: {ex.Message}");
        }
        
        // Create default config if it doesn't exist
        var defaultRepos = GetDefaultRepos();
        SaveRepos(defaultRepos);
        return defaultRepos;
    }
    
    private void SaveRepos(List<RepoInfo>? repos = null)
    {
        try
        {
            var directory = Path.GetDirectoryName(_configPath);
            if (directory != null && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            var json = JsonSerializer.Serialize(repos ?? _repos, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_configPath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving repos config: {ex.Message}");
        }
    }

    public bool OpenRepo(RepoInfo repo)
    {
        try {
            switch (repo.Type.ToLower())
            {
                case "wsl":
                    this.OpenRepoInWsl(repo);
                    break;
                case "code":
                    this.OpenRepoInVisualStudioCode(repo);
                    break;
                case "studio":
                    this.OpenRepoInVisualStudio(repo);
                    break;
                default:
                    throw new NotImplementedException($"Repo type '{repo.Type}' is not supported yet.");
            }
            return true;
        }
        catch (Exception ex)
        {
            // Console.WriteLine($"Error opening repo: {ex.Message}");
            return false;
        }
    }

    private void OpenRepoInWsl(RepoInfo repo)
    {
        var wslPath = repo.Path.Replace("/mnt/", "").Replace("/", "\\");
        
        var process = new Process {
            StartInfo = new ProcessStartInfo
            {
                FileName = "wsl",
                Arguments = $"code \"{wslPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        process.Start();
        process.WaitForExit();
    }

    private void OpenRepoInVisualStudioCode(RepoInfo repo)
    {
        var command = $"open -a 'Visual Studio Code' '{repo.Path}'";

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"{command}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };
        process.Start();
        process.WaitForExit();
    }

    private void OpenRepoInVisualStudio(RepoInfo repo)
    {
        var process = new Process
        {  
            StartInfo = new ProcessStartInfo
            {
                FileName = "devenv",
                Arguments = $"\"{repo.Path}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        process.Start();
        process.WaitForExit();
    }
    
    private List<RepoInfo> GetDefaultRepos()
    {
        return new List<RepoInfo>
        {
            new RepoInfo { Name = "customer-portal", Path = "/Users/matthewpenney/repos/customer-portal", Type = "wsl" },
            new RepoInfo { Name = "devops", Path = "/Users/matthewpenney/devops", Type = "code" },
            new RepoInfo { Name = "infrastructure", Path = "/Users/matthewpenney/repos/infrastructure", Type = "code" },
            new RepoInfo { Name = "ignite-portal", Path = "/Users/matthewpenney/repos/ignite-portal", Type = "code" },
            new RepoInfo { Name = "ignite", Path = "/Users/matthewpenney/repos/ignite", Type = "code" },
            new RepoInfo { Name = "portfolio", Path = "/Users/matthewpenney/repos/portfolio", Type = "code" }
        };
    }
}
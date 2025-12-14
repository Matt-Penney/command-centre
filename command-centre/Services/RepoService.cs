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
        _configPath = Path.Combine(Directory.GetCurrentDirectory(), "repos.json");

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
                return JsonSerializer.Deserialize<List<RepoInfo>>(json) ?? new List<RepoInfo>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading repos config: {ex.Message}");
        }
        
        return new List<RepoInfo>();
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
        catch
        {
            // Console.WriteLine($"Error opening repo: {ex.Message}");
            return false;
        }
    }

    private void OpenRepoInWsl(RepoInfo repo)
    {
        var process = new Process {
            StartInfo = new ProcessStartInfo
            {
                FileName = "wsl.exe",
                Arguments = $"-d Ubuntu --cd \"{repo.Path}\" -- bash -c \"code .; exec bash\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = false // Show the terminal window
            }
        };
        process.Start();
        process.WaitForExit();
    }

    private void OpenRepoInVisualStudioCode(RepoInfo repo)
    {
        if (OperatingSystem.IsWindows())
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/C code \"{repo.Path}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };
            process.Start();
            process.WaitForExit();
        }
        else
        {
            // Use open command on macOS/Linux
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
}
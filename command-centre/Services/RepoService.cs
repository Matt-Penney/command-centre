using System.Text.Json;
using System.Diagnostics;
using CommandCentre.Models;
using System.Threading.Tasks;

namespace CommandCentre.Services;

public class RepoService
{
    private readonly CommandService _commandService;

    private readonly string _configPath;
    private List<RepoInfo> _repos;
    
    public RepoService(CommandService commandService)
    {
        _commandService = commandService;

        _configPath = Path.Combine(Directory.GetCurrentDirectory(), "repos.json");

        _repos = LoadRepos();
    }
    
    public List<RepoInfo> GetAllRepos() => _repos;

    public List<RepoInfo> GetAllActiveRepos() => _repos.Where(r => r.IsActive.HasValue && r.IsActive.Value).ToList();
    
    public List<RepoInfo> GetFilteredRepos(string query) => _repos.Where(r => r.Name.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();

    public RepoInfo? GetByName(string name) => _repos.FirstOrDefault(r => r.Name == name);
    
    public RepoInfo? GetRepoByPath(string path) => _repos.FirstOrDefault(r => r.Path == path);
    
    public bool HasActiveDirectory(RepoInfo repo)
    {
        return System.IO.Directory.Exists(repo.Path);
    }

    private List<RepoInfo> LoadRepos()
    {
        try
        {
            if (File.Exists(_configPath))
            {
                var json = File.ReadAllText(_configPath);

                var repos = JsonSerializer.Deserialize<List<RepoInfo>>(json) ?? new List<RepoInfo>();

                foreach (var repo in repos)
                {
                    repo.IsActive = HasActiveDirectory(repo);
                }

                return repos;
            }
        }
        catch (Exception ex)
        {
            // Console.WriteLine($"Error loading repos config: {ex.Message}");
        }
        
        return new List<RepoInfo>();
    }

    public bool OpenToIDE(RepoInfo repo)
    {
        try {
            switch (repo.Type.ToLower())
            {
                case "wsl":
                    this.OpenInWsl(repo);
                    break;
                case "code":
                    this.OpenInVisualStudioCode(repo);
                    break;
                case "studio":
                    this.OpenInVisualStudio(repo);
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

    private void OpenInWsl(RepoInfo repo)
    {
        CommandInfo commandInfo = new CommandInfo
        {
            fileName = "wsl.exe",
            arguments = $"-d Ubuntu --cd \"{repo.Path}\" -- bash -c \"code .; exec bash\"",
            useShellExecute = false,
            redirectStandardOutput = true,
            redirectStandardError = true,
            createNoWindow = false
        };
        _commandService.RunCommand(commandInfo).Wait();
    }

    private void OpenInVisualStudioCode(RepoInfo repo)
    {
        CommandInfo commandInfo;
        if (OperatingSystem.IsWindows())
        {
            commandInfo = new CommandInfo
            {
                fileName = "cmd.exe",
                arguments = $"/C code \"{repo.Path}\"",
                useShellExecute = false,
                redirectStandardOutput = true,
                redirectStandardError = true,
                createNoWindow = true
            };
            _commandService.RunCommand(commandInfo).Wait();
        }
        else
        {
            commandInfo = new CommandInfo
            {
                fileName = "/bin/bash",
                arguments = $"-c \"open -a 'Visual Studio Code' '{repo.Path}'\"",
                useShellExecute = false,
                redirectStandardOutput = true,
                redirectStandardError = true,
                createNoWindow = true
            };
        }
        _commandService.RunCommand(commandInfo).Wait();
    }

    private void OpenInVisualStudio(RepoInfo repo)
    {
        CommandInfo commandInfo = new CommandInfo
        {
            fileName = "devenv",
            arguments = $"\"{repo.Path}\"",
            useShellExecute = false,
            redirectStandardOutput = true,
            redirectStandardError = true,
            createNoWindow = true
        };
        _commandService.RunCommand(commandInfo).Wait();
    }
}
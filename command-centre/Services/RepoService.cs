namespace CommandCentre.Services;

using System.Text.Json;
using System.Diagnostics;
using CommandCentre.Models;
using System.Threading.Tasks;

public class RepoService
{
    private CommandService _commandService = new CommandService();

    private readonly string _configPath = Path.Combine(Directory.GetCurrentDirectory(), "repos.json");
    private List<Repo> _repos;
    
    public RepoService()
    {
        _repos = LoadRepos();
    }
    
    public List<Repo> GetAllRepos() => _repos;

    public List<Repo> GetAllActiveRepos() => _repos.Where(r => r.IsActive).ToList();
    
    public List<Repo> GetFilteredRepos(string query) => _repos.Where(r => r.Name.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();

    public Repo? GetByName(string name) => _repos.FirstOrDefault(r => r.Name == name);
    
    public Repo? GetByPath(string path) => _repos.FirstOrDefault(r => r.Path == path);

    private List<Repo> LoadRepos()
    {
        try
        {
            if (File.Exists(_configPath))
            {
                var json = File.ReadAllText(_configPath);

                var repoDtos = JsonSerializer.Deserialize<List<RepoDTO>>(json) ?? new List<RepoDTO>();

                List<Repo> repos = new List<Repo>();
                foreach (var repoDto in repoDtos)
                {
                    Repo repo = new Repo
                    {
                        Name = repoDto.Name,
                        Path = repoDto.Path,
                        Type = repoDto.Type,
                        IsActive = HasActiveDirectory(repoDto.Path),
                        Owner = "",
                        Description = "",
                        Language = ""
                    };
                    repos.Add(repo);
                }

                return repos;
            }
        }
        catch (Exception ex)
        {
            // Console.WriteLine($"Error loading repos config: {ex.Message}");
        }
        
        return new List<Repo>();
    }
   
    private bool HasActiveDirectory(string repoPath)
    {
        return System.IO.Directory.Exists(repoPath);
    }

    public bool OpenToIDE(Repo repo)
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

    private void OpenInWsl(Repo repo)
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
        new CommandService().RunCommand(commandInfo).Wait();
    }

    private void OpenInVisualStudioCode(Repo repo)
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
        new CommandService().RunCommand(commandInfo).Wait();
    }

    private void OpenInVisualStudio(Repo repo)
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
        new CommandService().RunCommand(commandInfo).Wait();
    }
}
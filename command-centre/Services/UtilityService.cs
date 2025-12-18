namespace CommandCentre.Services;

using System.Diagnostics;
using System.Text.Json;
using CommandCentre.Models;

public class UtilityService
{
    private CommandService _commandService = new CommandService();

    private readonly string _configPath = Path.Combine(Directory.GetCurrentDirectory(), "utilities.json");
    private List<UtilityScript> _utilities = new();

    public UtilityService()
    {
        _utilities = LoadUtilities();
    }

    private List<UtilityScript> LoadUtilities()
    {
        try
        {
            if (File.Exists(_configPath))
            {  
                var json = File.ReadAllText(_configPath);
                return JsonSerializer.Deserialize<List<UtilityScript>>(json) ?? new List<UtilityScript>();
            }
        }
        catch (Exception ex)
        {
            // Console.WriteLine($"Error loading utilities: {ex.Message}");
        }

        return new List<UtilityScript>();
    }

    public List<UtilityScript> GetAllUtilities() => _utilities;

    public UtilityScript? GetByName(string name) =>_utilities.FirstOrDefault(u => u.Name == name);

    public IEnumerable<UtilityScript> GetFilteredUtilities(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return _utilities;
        
        return _utilities.Where(u => 
            u.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            u.Description.Contains(query, StringComparison.OrdinalIgnoreCase)
        );
    }

    public async Task<(bool successResult, string outputResult, string errorResult)> ExecuteUtility(UtilityScript utility)
    {
        try
        {
            var shell = utility.Type.ToLower() switch
            {
                "powershell" => "powershell",
                "python" => "python3",
                _ => "/bin/bash"
            };

            var shellArgs = utility.Type.ToLower() switch
            {
                "powershell" => $"-Command \"{utility.Command}\"",
                "python" => $"-c \"{utility.Command}\"",
                _ => $"-c \"{utility.Command}\""
            };

            if (OperatingSystem.IsWindows() && utility.RequiresAdmin)
            {
                CommandInfo commandInfo = new CommandInfo
                {
                    fileName = shell,
                    arguments = shellArgs,
                    useShellExecute = true,
                    redirectStandardOutput = false,
                    redirectStandardError = false,
                    createNoWindow = false,
                    verb = "runas"
                };
                var (adminOutput, adminError, adminExitCode) = await new CommandService().RunCommand(commandInfo);

                return (adminExitCode == 0, "No output captured when running as admin", string.Empty);
            }
            else
            {
                CommandInfo commandInfo = new CommandInfo
                {
                    fileName = shell,
                    arguments = shellArgs,
                    useShellExecute = false,
                    redirectStandardOutput = true,
                    redirectStandardError = true,
                    createNoWindow = true
                };
                var (output, error, exitCode) = await new CommandService().RunAsyncCommand(commandInfo);
                return (exitCode == 0, output, error);
            }
        }
        catch (Exception ex)
        {
            return (false, "", ex.Message);
        }
    }
}
using System.Diagnostics;
using System.Text.Json;
using CommandCentre.Models;

namespace CommandCentre.Services;

public class UtilityService
{
    private readonly string _configPath;
    private List<UtilityScript> _utilities = new();

    public UtilityService()
    {
        _configPath = Path.Combine(Directory.GetCurrentDirectory(), "utilities.json");

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
            Console.WriteLine($"Error loading utilities: {ex.Message}");
        }

        return new List<UtilityScript>();
    }

    public List<UtilityScript> GetAllUtilities() => _utilities;

    public UtilityScript? GetByName(string name) =>_utilities.FirstOrDefault(u => u.Name == name);

    public async Task<(bool Success, string Output, string Error)> ExecuteUtility(UtilityScript utility)
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
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = shell,
                        Arguments = shellArgs,
                        UseShellExecute = true,
                        Verb = "runas",
                        CreateNoWindow = false
                    }
                };
                process.Start();
                process.WaitForExit();
                return (process.ExitCode == 0, "No output captured when running as admin", string.Empty);
            }
            else
            {
                // Normal execution, capture output
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = shell,
                        Arguments = shellArgs,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                return (process.ExitCode == 0, output, error);
            }
        }
        catch (Exception ex)
        {
            return (false, "", ex.Message);
        }
    }
}
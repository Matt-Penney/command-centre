namespace CommandCentre.Services;

using CommandCentre.Models;
using System.Diagnostics;

public class CommandService
{
    public async Task<(string outputResult, string errorResult, int exitCode)> RunCommand(CommandInfo command)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = command.fileName,
            Arguments = command.arguments,
            UseShellExecute = command.useShellExecute,
            RedirectStandardOutput = command.redirectStandardOutput,
            RedirectStandardError = command.redirectStandardError,
            CreateNoWindow = command.createNoWindow,
            Verb = command.verb
        };

        using var process = new Process
        {
            StartInfo = processStartInfo
        };

        process.Start();

        string outputResult = string.Empty;
        if (command.redirectStandardOutput)
        {
            outputResult = await process.StandardOutput.ReadToEndAsync();
        }

        string errorResult = string.Empty;
        if (command.redirectStandardError)
        {
            errorResult = await process.StandardError.ReadToEndAsync();
        }

        process.WaitForExit();

        return (outputResult, errorResult, process.ExitCode);
    }

    public async Task<(string outputResult, string errorResult, int exitCode)> RunAsyncCommand(CommandInfo command)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = command.fileName,
            Arguments = command.arguments,
            UseShellExecute = command.useShellExecute,
            RedirectStandardOutput = command.redirectStandardOutput,
            RedirectStandardError = command.redirectStandardError,
            CreateNoWindow = command.createNoWindow,
            Verb = command.verb
        };

        using var process = new Process
        {
            StartInfo = processStartInfo
        };

        process.Start();

        string outputResult = string.Empty;
        if (command.redirectStandardOutput)
        {
            outputResult = await process.StandardOutput.ReadToEndAsync();
        }

        string errorResult = string.Empty;
        if (command.redirectStandardError)
        {
            errorResult = await process.StandardError.ReadToEndAsync();
        }

        await process.WaitForExitAsync();

        return (outputResult, errorResult, process.ExitCode);
    }
}
using LibGit2Sharp;
using NuggetUpdater.Models;
using NuggetUpdater.Settings;
using System.Diagnostics;

namespace NuggetUpdater.Helpers;

public static class GitHelper
{
    public static async Task<UserCredentials> GetUserCredentialsAsync(string path)
    {
        string command = "echo protocol=https`nhost=dev.azure.com`npath=/location-services/_git/repo | git credential fill";
        string output = await RunPowerShellCommandAsync(command, path);

        if (output == null)
        {
            return default;
        }

        var email = ExtractValueFromOutput(output, "username");
        var token = ExtractValueFromOutput(output, "password");

        var userCredentials = new UserCredentials
        {
            Email = email,
            Token = token
        };

        return userCredentials;
    }

    public static void CommitChanges(string commitMessage)
    {
        using var repository = new Repository(AppConfig.SolutionPath);

        var isDataChanged = repository.RetrieveStatus().IsDirty;

        if (!isDataChanged)
        {
            return;
        }

        Commands.Stage(repository, "*");

        var signature = CreateSignature(AppConfig.Email);
        repository.Commit(commitMessage, signature, signature);
    }

    private static async Task<string> RunPowerShellCommandAsync(string command, string workingDirectory)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -Command \"{command}\"",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory
        };

        using var process = Process.Start(psi);
        using var reader = process?.StandardOutput;

        var result = await reader?.ReadToEndAsync();

        return result;
    }

    private static string ExtractValueFromOutput(string output, string key) =>
        output.Split('\n', StringSplitOptions.RemoveEmptyEntries)
              .Select(line => line.Split('='))
              .FirstOrDefault(parts => parts.Length == 2 && parts[0].Trim() == key)?[1]
              .Trim();

    private static Signature CreateSignature(string email)
    {
        var userName = email.Split('@')[0];
        return new Signature(userName, email, DateTime.UtcNow);
    }
}

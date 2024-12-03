using NuggetUpdater.Models;
using System.IO;
using System.Text.Json;

namespace NuggetUpdater.Settings;

public static class AppConfig
{
    private static readonly string AppDataPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "NuggetUpdater");

    private static readonly string ConfigFileName = Path.Combine(AppDataPath, "appconfigs.json");

    static AppConfig()
    {
        var isFileExists = File.Exists(ConfigFileName);

        if (!isFileExists)
        {
            return;
        }

        var configJson = File.ReadAllText(ConfigFileName);
        var configData = JsonSerializer.Deserialize<ConfigData>(configJson);

        if (configData == null)
        {
            return;
        }

        DefaultPackageName = configData.DefaultPackageName;

        if (!string.IsNullOrWhiteSpace(configData.CustomNuGetSource))
        {
            NuGetSources.Add(configData.CustomNuGetSource);
        }
    }

    public static string SolutionPath { get; private set; } = string.Empty;
    public static string Email { get; private set; } = string.Empty;
    public static string Token { get; private set; } = string.Empty;
    public static string DefaultPackageName { get; private set; } = string.Empty;
    public static HashSet<string> NuGetSources { get; private set; } = new HashSet<string>
    {
        "https://api.nuget.org/v3/index.json"
    };

    public static bool IsMainConfigValid() =>
        !string.IsNullOrWhiteSpace(SolutionPath) &&
        Directory.Exists(SolutionPath) &&
        !string.IsNullOrWhiteSpace(Email) &&
        !string.IsNullOrWhiteSpace(Token);

    public static void InitilizeConfig(string path, string email, string token)
    {
        SolutionPath = path;
        Email = email;
        Token = token;
    }
}
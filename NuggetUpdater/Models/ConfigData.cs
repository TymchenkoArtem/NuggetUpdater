namespace NuggetUpdater.Models;

public sealed record ConfigData
{
    public string DefaultPackageName { get; set; }
    public string CustomNuGetSource { get; set; }
}

using NuGet.Common;
using NuGet.Configuration;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using NuggetUpdater.Settings;
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace NuggetUpdater.Helpers;

public static class NuGetHelper
{
    public static async Task<List<string>> LoadNuGetPackagesFromSolutionAsync(string solutionPath)
    {
        var projectFiles = Directory.EnumerateFiles(solutionPath, "*.csproj", SearchOption.AllDirectories);

        var packageTasks = projectFiles.AsParallel().Select(ExtractPackageReferencesAsync);

        var results = await Task.WhenAll(packageTasks);

        var packages = results.SelectMany(packages => packages)
                      .Distinct()
                      .OrderBy(packageName => packageName)
                      .ToList();

        return packages;
    }

    public static async Task<IReadOnlyCollection<string>> GetPackageVersionsAsync(string packageName)
    {
        foreach (var sourceUrl in AppConfig.NuGetSources)
        {
            var versions = await FetchPackageVersionsFromSourceAsync(sourceUrl, packageName);

            if (versions.Any())
            {
                var normalizedVersions = versions.Select(version => version.ToNormalizedString())
                               .OrderByDescending(v => v)
                               .ToArray();

                return normalizedVersions;
            }

        }

        return Array.Empty<string>();
    }

    public static async Task UpdatePackageVersionAsync(string projectFilePath, string packageName, string newVersion)
    {
        var doc = new XmlDocument { PreserveWhitespace = true };

        await Task.Run(() => doc.Load(projectFilePath));

        var packageReference = FindPackageReference(doc, packageName);

        if (packageReference != null)
        {
            packageReference.SetAttribute("Version", newVersion);
            await Task.Run(() => doc.Save(projectFilePath));
        }
    }

    private static async Task<IEnumerable<string>> ExtractPackageReferencesAsync(string filePath)
    {
        var doc = await Task.Run(() => XDocument.Load(filePath));

        var pachageReferences = doc.Descendants("PackageReference")
                  .Select(element => element.Attribute("Include")?.Value)
                  .Where(name => !string.IsNullOrWhiteSpace(name)); ;

        return pachageReferences;
    }

    private static XmlElement FindPackageReference(XmlDocument doc, string packageName) =>
        doc.GetElementsByTagName("PackageReference")
           .Cast<XmlElement>()
           .FirstOrDefault(node => node.GetAttribute("Include") == packageName);

    private static async Task<IEnumerable<NuGetVersion>> FetchPackageVersionsFromSourceAsync(string sourceUrl, string packageName)
    {
        var packageSource = new PackageSource(sourceUrl)
        {
            Credentials = new PackageSourceCredential(
                sourceUrl, AppConfig.Email, AppConfig.Token, isPasswordClearText: true, validAuthenticationTypesText: null)
        };

        var repository = Repository.Factory.GetCoreV3(packageSource);
        var resource = await repository.GetResourceAsync<FindPackageByIdResource>();

        if (resource == null)
        {
            return Enumerable.Empty<NuGetVersion>();
        }

        var versions = await resource.GetAllVersionsAsync(packageName, new SourceCacheContext(), NullLogger.Instance, CancellationToken.None);

        return versions;
    }
}

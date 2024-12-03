using NuggetUpdater.Helpers;
using NuggetUpdater.Settings;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using MessageBox = System.Windows.Forms.MessageBox;

namespace NuggetUpdater;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        InitializeAppAsync().ConfigureAwait(false);
    }

    private async Task InitializeAppAsync()
    {
        try
        {
            string contextMenuPath = await FetchContextMenuPathAsync();
            await InitializeSettingsAsync(contextMenuPath);

            var nugetPackages = await LoadNuGetPackagesAsync(AppConfig.SolutionPath);

            if(nugetPackages == null)
            {
                return;
            }

            UpdateUiThread(() =>
            {
                NuGetPackageComboBox.ItemsSource = nugetPackages;
                NuGetPackageComboBox.IsEnabled = nugetPackages.Any();

                var selectedIndex = Array.IndexOf(nugetPackages, AppConfig.DefaultPackageName);

                if (!string.IsNullOrWhiteSpace(AppConfig.DefaultPackageName))
                {
                    NuGetPackageComboBox.SelectedIndex = selectedIndex;
                }
            });
        }
        catch (Exception ex)
        {
            HandleError("Initialization failed", ex);
        }
    }

    private async Task InitializeSettingsAsync(string path)
    {
        var credentials = await GitHelper.GetUserCredentialsAsync(path);

        AppConfig.InitilizeConfig(path, credentials?.Email, credentials?.Token);

        var isConfigValid = AppConfig.IsMainConfigValid();

        if (!isConfigValid)
        {
            throw new InvalidOperationException("Invalid settings. Please configure the application.");
        }
    }

    private static async Task<string> FetchContextMenuPathAsync()
    {
        var args = Environment.GetCommandLineArgs();

        if (args.Length < 2)
        {
            throw new ArgumentException("Invalid context path.");
        }

        var path = await Task.FromResult(args[1]);

        return path;
    }

    private async Task<string[]> LoadNuGetPackagesAsync(string solutionPath)
    {
        var packages = await NuGetHelper.LoadNuGetPackagesFromSolutionAsync(solutionPath);
        
        return packages?.ToArray();
    }

    private async void OnNuGetPackageChanged(object sender, SelectionChangedEventArgs e)
    {
        if (NuGetPackageComboBox.SelectedItem is string selectedPackage)
        {
            try
            {
                var versions = await NuGetHelper.GetPackageVersionsAsync(selectedPackage);

                if(versions == null)
                {
                    return;
                }

                UpdateUiThread(() =>
                {
                    NuGetVersionComboBox.ItemsSource = versions;
                    NuGetVersionComboBox.IsEnabled = versions.Any();
                });
            }
            catch (Exception ex)
            {
                HandleError("Error loading package versions", ex);
            }
        }
    }

    private async void OnUpdateButtonClick(object sender, RoutedEventArgs e)
    {
        try
        {
            ValidateInputs();

            var projectFiles = Directory.EnumerateFiles(AppConfig.SolutionPath, "*.csproj", SearchOption.AllDirectories);
            var package = NuGetPackageComboBox.SelectedItem.ToString();
            var version = NuGetVersionComboBox.SelectedItem.ToString();

            await Task.WhenAll(projectFiles.Select(file => NuGetHelper.UpdatePackageVersionAsync(file, package, version)));

            GitHelper.CommitChanges($"Updated {package} to {version}");
            MessageBox.Show("Package updated successfully.", "Success", (MessageBoxButtons)MessageBoxButton.OK, (MessageBoxIcon)MessageBoxImage.Information);
            CloseApp();
        }
        catch (Exception ex)
        {
            HandleError("Update failed", ex);
        }
    }

    private void ValidateInputs()
    {
        var isValid = !string.IsNullOrEmpty(AppConfig.SolutionPath) &&
            NuGetPackageComboBox.SelectedItem != null &&
            NuGetVersionComboBox.SelectedItem != null;

        if (!isValid)
        {
            throw new InvalidOperationException("All fields must be filled out.");
        }
    }

    private void HandleError(string context, Exception ex)
    {
        ShowError($"{context}: {ex.Message}");

        if (context == "Initialization failed")
        {
            CloseApp();
        }
    }

    private void ShowError(string message) =>
        MessageBox.Show(message, "Error", (MessageBoxButtons)MessageBoxButton.OK, (MessageBoxIcon)MessageBoxImage.Error);

    private void UpdateUiThread(Action action) =>
        Dispatcher.Invoke(action);

    private void CloseApp() =>
        Environment.Exit(0);
}

// Author: Anwarul Haque (@HYPERMX7) | Contact: 7739399007
using HyperPDF.Models;
using HyperPDF.Services;
using Xunit;

namespace HyperPDF.Tests;

/// <summary>Tests the JSON backed settings persistence.</summary>
public sealed class SettingsServiceTests : IDisposable
{
    private readonly string _settingsPath = Path.Combine(
        Path.GetTempPath(),
        $"hyperpdf-settings-{Guid.NewGuid():N}",
        "settings.json");

    public void Dispose()
    {
        try
        {
            string? directory = Path.GetDirectoryName(_settingsPath);
            if (directory is not null && Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
        catch (IOException)
        {
            // Ignore clean up failures in the temporary folder.
        }
    }

    [Fact]
    public async Task SettingsArePersistedAndReloaded()
    {
        var service = new SettingsService(_settingsPath);
        service.Settings.Theme = AppTheme.Dark;
        service.Settings.ZoomMode = ZoomMode.Custom;
        service.Settings.ZoomPercent = 175;
        service.Settings.ShowThumbnails = false;

        await service.SaveAsync();

        Assert.True(File.Exists(_settingsPath));

        var reloaded = new SettingsService(_settingsPath);
        await reloaded.LoadAsync();

        Assert.Equal(AppTheme.Dark, reloaded.Settings.Theme);
        Assert.Equal(ZoomMode.Custom, reloaded.Settings.ZoomMode);
        Assert.Equal(175, reloaded.Settings.ZoomPercent);
        Assert.False(reloaded.Settings.ShowThumbnails);
    }

    [Fact]
    public async Task MissingSettingsFileYieldsDefaults()
    {
        var service = new SettingsService(_settingsPath);

        await service.LoadAsync();

        Assert.NotNull(service.Settings);
        Assert.Equal(AppTheme.Light, service.Settings.Theme);
        Assert.Equal(100, service.Settings.ZoomPercent);
    }

    [Fact]
    public async Task CorruptSettingsFileFallsBackToDefaults()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
        await File.WriteAllTextAsync(_settingsPath, "{ this is not json ");

        var service = new SettingsService(_settingsPath);
        await service.LoadAsync();

        Assert.NotNull(service.Settings);
        Assert.Equal(100, service.Settings.ZoomPercent);
    }

    [Fact]
    public async Task SavedEventIsRaised()
    {
        var service = new SettingsService(_settingsPath);
        bool raised = false;
        service.Saved += (_, _) => raised = true;

        await service.SaveAsync();

        Assert.True(raised);
    }

    [Fact]
    public void ResetToDefaultsRestoresTheDefaultValues()
    {
        var service = new SettingsService(_settingsPath);
        service.Settings.Theme = AppTheme.Dark;
        service.Settings.ZoomPercent = 400;

        service.ResetToDefaults();

        Assert.Equal(100, service.Settings.ZoomPercent);
        Assert.Equal(AppTheme.Light, service.Settings.Theme);
    }

    [Fact]
    public void DefaultSettingsFilePathLivesBelowTheApplicationDataFolder()
    {
        string path = SettingsService.DefaultSettingsFilePath();

        Assert.Contains(BrandInfo.ApplicationName, path, StringComparison.Ordinal);
        Assert.EndsWith("settings.json", path, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WindowPlacementWithoutCoordinatesSurvivesSerialisation()
    {
        var state = new WindowPlacementState
        {
            Width = 1200,
            Height = 800
        };

        string json = System.Text.Json.JsonSerializer.Serialize(state);

        var restored = System.Text.Json.JsonSerializer.Deserialize<WindowPlacementState>(json);

        Assert.NotNull(restored);
        Assert.Equal(1200, restored!.Width);
        Assert.Equal(800, restored.Height);
        Assert.False(restored.HasValidPosition);
    }

    [Fact]
    public void CloneCopiesEveryPreference()
    {
        var settings = new AppSettings
        {
            Theme = AppTheme.Dark,
            ViewMode = ViewMode.SinglePage,
            ZoomMode = ZoomMode.FitPage,
            ZoomPercent = 123,
            ShowThumbnails = false,
            ShowStatusBar = false,
            RestoreLastPosition = false,
            ShowWelcomeScreen = false
        };

        AppSettings clone = settings.Clone();

        Assert.Equal(settings.Theme, clone.Theme);
        Assert.Equal(settings.ViewMode, clone.ViewMode);
        Assert.Equal(settings.ZoomMode, clone.ZoomMode);
        Assert.Equal(settings.ZoomPercent, clone.ZoomPercent);
        Assert.Equal(settings.ShowThumbnails, clone.ShowThumbnails);
        Assert.Equal(settings.ShowStatusBar, clone.ShowStatusBar);
        Assert.Equal(settings.RestoreLastPosition, clone.RestoreLastPosition);
        Assert.Equal(settings.ShowWelcomeScreen, clone.ShowWelcomeScreen);
    }
}

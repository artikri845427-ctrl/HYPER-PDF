// Author: Anwarul Haque (@HYPERMX7) | Contact: 7739399007
using System.Text.Json;
using System.Text.Json.Serialization;
using HyperPDF.Models;

namespace HyperPDF.Services;

/// <summary>Persists the user preferences of HyperPDF.</summary>
public interface ISettingsService
{
    /// <summary>The current settings. Never null.</summary>
    AppSettings Settings { get; }

    /// <summary>Path of the settings file on disk.</summary>
    string SettingsFilePath { get; }

    /// <summary>Loads the settings from disk. Missing or corrupt files fall back to defaults.</summary>
    Task LoadAsync();

    /// <summary>Writes the settings to disk.</summary>
    Task SaveAsync();

    /// <summary>Restores the default settings (in memory only).</summary>
    void ResetToDefaults();

    /// <summary>Raised after the settings have been changed and persisted.</summary>
    event EventHandler? Saved;
}

/// <summary>JSON file based implementation of <see cref="ISettingsService"/>.</summary>
public sealed class SettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly SemaphoreSlim _writeLock = new(1, 1);

    /// <summary>Creates the service using the default location below %AppData%.</summary>
    public SettingsService()
        : this(DefaultSettingsFilePath())
    {
    }

    /// <summary>Creates the service using an explicit file location (used by the tests).</summary>
    public SettingsService(string settingsFilePath)
    {
        SettingsFilePath = settingsFilePath;
        Settings = new AppSettings();
    }

    /// <inheritdoc />
    public AppSettings Settings { get; private set; }

    /// <inheritdoc />
    public string SettingsFilePath { get; }

    /// <inheritdoc />
    public event EventHandler? Saved;

    /// <summary>Default location of the settings file.</summary>
    public static string DefaultSettingsFilePath()
    {
        string root = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(root, BrandInfo.ApplicationName, "settings.json");
    }

    /// <inheritdoc />
    public async Task LoadAsync()
    {
        try
        {
            if (!File.Exists(SettingsFilePath))
            {
                Settings = new AppSettings();
                return;
            }

            await using FileStream stream = File.OpenRead(SettingsFilePath);
            var loaded = await JsonSerializer.DeserializeAsync<AppSettings>(stream, SerializerOptions).ConfigureAwait(false);
            Settings = loaded ?? new AppSettings();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            // A damaged settings file must never prevent the application from starting.
            Settings = new AppSettings();
        }
    }

    /// <inheritdoc />
    public async Task SaveAsync()
    {
        await _writeLock.WaitAsync().ConfigureAwait(false);
        try
        {
            string? directory = Path.GetDirectoryName(SettingsFilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string temporary = SettingsFilePath + ".tmp";
            await using (FileStream stream = File.Create(temporary))
            {
                await JsonSerializer.SerializeAsync(stream, Settings, SerializerOptions).ConfigureAwait(false);
            }

            // Replace atomically so a crash can never leave a half written settings file behind.
            File.Move(temporary, SettingsFilePath, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Persisting preferences is best effort; a failure must not crash the application.
        }
        finally
        {
            _writeLock.Release();
        }

        Saved?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public void ResetToDefaults() => Settings = new AppSettings();
}

// Author: Anwarul Haque (@HYPERMX7) | Contact: 7739399007
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace HyperPDF.Services;

/// <summary>Schema of the update.json manifest served by HyperPDF.</summary>
public sealed record UpdateManifest
{
    /// <summary>Latest available version, for example "1.1.0".</summary>
    [JsonPropertyName("version")]
    public string Version { get; init; } = string.Empty;

    /// <summary>Direct download URL of the installer executable.</summary>
    [JsonPropertyName("downloadUrl")]
    public string DownloadUrl { get; init; } = string.Empty;

    /// <summary>Optional summary of the release shown in the update dialog.</summary>
    [JsonPropertyName("releaseNotes")]
    public string? ReleaseNotes { get; init; }

    /// <summary>Optional SHA256 hash of the installer; verified before installation.</summary>
    [JsonPropertyName("sha256")]
    public string? Sha256 { get; init; }
}

/// <summary>Finds, downloads and installs HyperPDF updates.</summary>
public interface IUpdateService
{
    /// <summary>URL of the update manifest.</summary>
    Uri CheckUrl { get; }

    /// <summary>Version of the running application.</summary>
    string CurrentVersion { get; }

    /// <summary>
    /// Returns the manifest of a newer release, or null when the service is up to date or
    /// unreachable. A failed check never throws, it only returns null.
    /// </summary>
    Task<UpdateManifest?> CheckForUpdatesAsync(CancellationToken cancellationToken = default);

    /// <summary>Downloads the installer and returns its local path.</summary>
    Task<string> DownloadAsync(UpdateManifest manifest, IProgress<double> progress, CancellationToken cancellationToken = default);

    /// <summary>Starts the installer and returns once the process has been launched.</summary>
    void LaunchInstaller(string installerPath);
}

/// <summary>Default implementation backed by HTTP.</summary>
public sealed class UpdateService : IUpdateService
{
    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(20)
    };

    private Uri? _checkUrl;

    /// <summary>Creates the service using the brand URL and the running version.</summary>
    public UpdateService()
        : this(null, null)
    {
    }

    /// <summary>Creates the service with an explicit manifest URL or current version (tests).</summary>
    public UpdateService(Uri? checkUrl = null, string? currentVersion = null)
    {
        _checkUrl = checkUrl;
        CurrentVersion = currentVersion ?? BrandInfo.ApplicationVersion;
    }

    /// <inheritdoc />
    public Uri CheckUrl => _checkUrl ??= new Uri(BrandInfo.UpdateCheckUrl);

    /// <inheritdoc />
    public string CurrentVersion { get; }

    /// <summary>URL override helper used by tests.</summary>
    public void SetCheckUrl(Uri url) => _checkUrl = url;

    /// <inheritdoc />
    public async Task<UpdateManifest?> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await Http.GetAsync(CheckUrl, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            UpdateManifest? manifest = ParseManifest(body);

            if (manifest is null || string.IsNullOrWhiteSpace(manifest.Version))
            {
                return null;
            }

            return IsNewerVersion(CurrentVersion, manifest.Version) ? manifest : null;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or IOException)
        {
            // An unreachable server or a damaged manifest must not crash the application.
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<string> DownloadAsync(UpdateManifest manifest, IProgress<double> progress, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(manifest.DownloadUrl))
        {
            throw new ArgumentException("The manifest does not define a download URL.", nameof(manifest));
        }

        string directory = Path.Combine(Path.GetTempPath(), BrandInfo.ApplicationName, "update");
        Directory.CreateDirectory(directory);

        string fileName = $"HyperPDF-Setup-{manifest.Version}.exe";
        string destination = Path.Combine(directory, fileName);
        string temporary = destination + ".part";

        using (var response = await Http.GetAsync(manifest.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false))
        {
            response.EnsureSuccessStatusCode();

            long? total = response.Content.Headers.ContentLength;
            await using Stream source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            await using (FileStream target = File.Create(temporary))
            {
                byte[] buffer = new byte[81920];
                long written = 0;
                int read;
                while ((read = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
                {
                    await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                    written += read;
                    if (total is long size && size > 0)
                    {
                        progress?.Report(Math.Min(100.0, written * 100.0 / size));
                    }
                    else
                    {
                        progress?.Report(-1);
                    }
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(manifest.Sha256))
        {
            VerifySha256(temporary, manifest.Sha256);
        }

        File.Move(temporary, destination, overwrite: true);
        return destination;
    }

    /// <inheritdoc />
    public void LaunchInstaller(string installerPath)
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = installerPath,
            Arguments = "/SILENT /SUPPRESSMSGBOXES /NORESTART /CLOSEAPPLICATIONS /RESTARTAPPLICATIONS",
            UseShellExecute = true
        };

        System.Diagnostics.Process.Start(startInfo);
    }

    /// <summary>Parses the JSON body of an update manifest.</summary>
    public static UpdateManifest? ParseManifest(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<UpdateManifest>(json);
    }

    /// <summary>
    /// Compares two dotted version strings. Missing numeric parts count as zero, therefore
    /// "1.0" equals "1.0.0" and "1.2" is newer than "1.1.9".
    /// </summary>
    public static bool IsNewerVersion(string currentVersion, string candidateVersion)
    {
        int[] current = ParseVersionParts(currentVersion);
        int[] candidate = ParseVersionParts(candidateVersion);

        for (int index = 0; index < Math.Max(current.Length, candidate.Length); index++)
        {
            int left = index < current.Length ? current[index] : 0;
            int right = index < candidate.Length ? candidate[index] : 0;
            if (left != right)
            {
                return right > left;
            }
        }

        return false;
    }

    private static int[] ParseVersionParts(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return [0];
        }

        Match firstPart = Regex.Match(version.Trim(), @"^\d+(?:\.\d+)*");
        if (!firstPart.Success)
        {
            return [0];
        }

        return firstPart.Value.Split('.').Select(p => int.TryParse(p, out int number) ? Math.Clamp(number, 0, ushort.MaxValue) : 0).ToArray();
    }

    private static void VerifySha256(string filePath, string expected)
    {
        string normalized = expected.Trim().Replace("-", string.Empty);
        byte[] hash = System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(filePath));
        string actual = Convert.ToHexString(hash).ToLowerInvariant();

        if (!string.Equals(actual, normalized, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("The downloaded update does not match the published hash.");
        }
    }
}

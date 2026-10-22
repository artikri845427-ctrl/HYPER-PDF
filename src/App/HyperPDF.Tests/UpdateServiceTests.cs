// Author: Anwarul Haque (@HYPERMX7) | Contact: 7739399007
using System.Text.Json;
using HyperPDF.Services;
using Xunit;

namespace HyperPDF.Tests;

/// <summary>Unit tests of the automatic update service.</summary>
public class UpdateServiceTests
{
    [Theory]
    [InlineData("1.0.0", "1.0.1", true)]
    [InlineData("1.0.0", "1.0.0", false)]
    [InlineData("1.0.0", "0.9.9", false)]
    [InlineData("1.0", "1.0.0", false)]
    [InlineData("1.2", "1.1.9", false)]
    [InlineData("1.2", "1.2.0", false)]
    [InlineData("2.0", "1.9.9", false)]
    [InlineData("1.9.9", "2.0", true)]
    [InlineData("1.9.8", "1.9.9", true)]
    [InlineData("", "1.0.0", true)]
    [InlineData("1.0.0", "", false)]
    public void IsNewerVersionComparesDottedVersions(string current, string candidate, bool expected)
    {
        Assert.Equal(expected, UpdateService.IsNewerVersion(current, candidate));
    }

    [Fact]
    public void ParseManifestReadsAllFields()
    {
        const string json = """
        {
          "version": "1.1.0",
          "downloadUrl": "https://example.com/HyperPDF-Setup-1.1.0.exe",
          "releaseNotes": "New reader mode.",
          "sha256": "ABC0123"
        }
        """;

        UpdateManifest? manifest = UpdateService.ParseManifest(json);

        Assert.NotNull(manifest);
        Assert.Equal("1.1.0", manifest!.Version);
        Assert.Equal("https://example.com/HyperPDF-Setup-1.1.0.exe", manifest.DownloadUrl);
        Assert.Equal("New reader mode.", manifest.ReleaseNotes);
        Assert.Equal("ABC0123", manifest.Sha256);
    }

    [Fact]
    public void ParseManifestToleratesEmptyText()
    {
        Assert.Null(UpdateService.ParseManifest(""));
        Assert.Null(UpdateService.ParseManifest("   "));
    }

    [Fact]
    public void CheckForUpdatesReturnsNullWhenServerIsUnreachable()
    {
        var service = new UpdateService(new Uri("http://127.0.0.1:9/update.json"), "1.0.0");

        UpdateManifest? manifest = service.CheckForUpdatesAsync().GetAwaiter().GetResult();

        Assert.Null(manifest);
    }
}

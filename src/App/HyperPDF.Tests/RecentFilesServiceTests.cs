// Author: Anwarul Haque (@HYPERMX7) | Contact: 7739399007
using HyperPDF.Models;
using HyperPDF.Services;
using Xunit;

namespace HyperPDF.Tests;

/// <summary>Tests the recent documents list.</summary>
public sealed class RecentFilesServiceTests
{
    private static (SettingsService Settings, RecentFilesService Recent) CreateServices()
    {
        var settings = new SettingsService(Path.Combine(
            Path.GetTempPath(),
            $"hyperpdf-recent-{Guid.NewGuid():N}.json"));
        return (settings, new RecentFilesService(settings));
    }

    [Fact]
    public void AddedFilesAppearAtTheTopOfTheList()
    {
        var (_, recent) = CreateServices();

        recent.Add(@"C:\Docs\first.pdf");
        recent.Add(@"C:\Docs\second.pdf");

        Assert.Equal(2, recent.Items.Count);
        Assert.Equal(@"C:\Docs\second.pdf", recent.Items[0].FilePath);
        Assert.Equal(@"C:\Docs\first.pdf", recent.Items[1].FilePath);
    }

    [Fact]
    public void AddingTheSameFileTwiceMovesItToTheTopWithoutDuplicatingIt()
    {
        var (_, recent) = CreateServices();

        recent.Add(@"C:\Docs\first.pdf");
        recent.Add(@"C:\Docs\second.pdf");
        recent.Add(@"C:\Docs\first.pdf");

        Assert.Equal(2, recent.Items.Count);
        Assert.Equal(@"C:\Docs\first.pdf", recent.Items[0].FilePath);
    }

    [Fact]
    public void RemovedFilesDisappearFromTheList()
    {
        var (_, recent) = CreateServices();

        recent.Add(@"C:\Docs\first.pdf");
        recent.Remove(@"C:\Docs\first.pdf");

        Assert.Empty(recent.Items);
    }

    [Fact]
    public void ClearRemovesEveryEntry()
    {
        var (_, recent) = CreateServices();

        recent.Add(@"C:\Docs\first.pdf");
        recent.Add(@"C:\Docs\second.pdf");
        recent.Clear();

        Assert.Empty(recent.Items);
    }

    [Fact]
    public void BlankPathsAreIgnored()
    {
        var (_, recent) = CreateServices();

        recent.Add("   ");
        recent.Add(string.Empty);

        Assert.Empty(recent.Items);
    }

    [Fact]
    public void TheListIsCappedAtTheMaximumNumberOfEntries()
    {
        var (_, recent) = CreateServices();

        for (int i = 0; i < AppSettings.MaxRecentFiles + 5; i++)
        {
            recent.Add($@"C:\Docs\document-{i}.pdf");
        }

        Assert.Equal(AppSettings.MaxRecentFiles, recent.Items.Count);
        Assert.Equal(@"C:\Docs\document-" + (AppSettings.MaxRecentFiles + 4) + ".pdf", recent.Items[0].FilePath);
    }

    [Fact]
    public void ChangedEventIsRaisedOnAddRemoveAndClear()
    {
        var (_, recent) = CreateServices();
        int changes = 0;
        recent.Changed += (_, _) => changes++;

        recent.Add(@"C:\Docs\first.pdf");
        recent.Remove(@"C:\Docs\first.pdf");
        recent.Add(@"C:\Docs\second.pdf");
        recent.Clear();

        Assert.Equal(4, changes);
    }
}

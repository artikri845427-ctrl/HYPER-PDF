// Author: Anwarul Haque (@HYPERMX7) | Contact: 7739399007
using System.Reflection;
using Xunit;

namespace HyperPDF.Tests;

/// <summary>Verifies that the branding of HyperPDF is consistent everywhere.</summary>
public sealed class BrandingTests
{
    [Fact]
    public void AssemblyVersionMatchesTheBrandVersion()
    {
        Assembly assembly = typeof(BrandInfo).Assembly;
        AssemblyName name = assembly.GetName();

        Assert.Equal(BrandInfo.ApplicationVersion, name.Version?.ToString(3));
    }

    [Fact]
    public void ProductAndCompanyMetadataCarryTheBrandValues()
    {
        Assembly assembly = typeof(BrandInfo).Assembly;

        Assert.Equal(BrandInfo.ApplicationName, assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product);
        Assert.Equal(BrandInfo.Publisher, assembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company);
        Assert.Equal(BrandInfo.Copyright, assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright);

        var metadata = assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .ToDictionary(a => a.Key, a => a.Value ?? string.Empty, StringComparer.Ordinal);

        Assert.Equal(BrandInfo.Publisher, metadata["Author"]);
        Assert.Equal(BrandInfo.ContactNumber, metadata["Contact"]);
    }

    [Fact]
    public void CreditLineContainsTheDeveloperHandleAndTheContactNumber()
    {
        Assert.Contains(BrandInfo.DeveloperHandle, BrandInfo.CreditLine, StringComparison.Ordinal);
        Assert.Contains(BrandInfo.ContactNumber, BrandInfo.CreditLine, StringComparison.Ordinal);
        Assert.Contains(BrandInfo.DeveloperHandle, BrandInfo.StatusBarSignature, StringComparison.Ordinal);
        Assert.Contains(BrandInfo.DeveloperHandle, BrandInfo.Copyright, StringComparison.Ordinal);
    }

    [Fact]
    public void ApplicationNameIsHyperPdf()
    {
        Assert.Equal("HyperPDF", BrandInfo.ApplicationName);
        Assert.Equal("1.0.0", BrandInfo.ApplicationVersion);
    }

    [Fact]
    public void DirectoryBuildPropsMatchesTheBrandConstants()
    {
        string? props = FindRepositoryFile("Directory.Build.props");
        Assert.NotNull(props);

        string content = File.ReadAllText(props!);

        Assert.Contains($"<HyperPdfProductName>{BrandInfo.ApplicationName}</HyperPdfProductName>", content, StringComparison.Ordinal);
        Assert.Contains($"<HyperPdfVersion>{BrandInfo.ApplicationVersion}</HyperPdfVersion>", content, StringComparison.Ordinal);
        Assert.Contains($"<HyperPdfAuthor>{BrandInfo.Publisher}</HyperPdfAuthor>", content, StringComparison.Ordinal);
        Assert.Contains($"<HyperPdfContact>{BrandInfo.ContactNumber}</HyperPdfContact>", content, StringComparison.Ordinal);
        Assert.Contains($"<HyperPdfRepository>{BrandInfo.RepositoryUrl}</HyperPdfRepository>", content, StringComparison.Ordinal);
    }

    [Fact]
    public void DocumentTitleContainsTheApplicationName()
    {
        Assert.Equal("report.pdf - HyperPDF", BrandInfo.DocumentTitle("report.pdf"));
        Assert.Equal(BrandInfo.ApplicationTitle, BrandInfo.DocumentTitle("   "));
    }

    private static string? FindRepositoryFile(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            string candidate = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return null;
    }
}

// Author: Anwarul Haque (@HYPERMX7) | Contact: 7739399007
namespace HyperPDF;

/// <summary>
/// Single source of truth for all HyperPDF branding information.
/// Every view, dialog, installer script, native extension and CI job must source
/// its branding values from this file (or from <c>Directory.Build.props</c>, which is
/// verified against this file by the unit tests).
/// </summary>
public static class BrandInfo
{
    /// <summary>Product name shown in the UI, task bar and Windows Apps and Features.</summary>
    public const string ApplicationName = "HyperPDF";

    /// <summary>Application version. Kept in sync with the assembly version by unit tests.</summary>
    public const string ApplicationVersion = "1.0.0";

    /// <summary>Legal name of the developer.</summary>
    public const string DeveloperName = "Anwarul Haque";

    /// <summary>Social handle / brand of the developer.</summary>
    public const string DeveloperHandle = "@HYPERMX7";

    /// <summary>Contact telephone number of the developer.</summary>
    public const string ContactNumber = "7739399007";

    /// <summary>Full copyright statement.</summary>
    public const string Copyright = "\u00A9 2026 Anwarul Haque (@HYPERMX7). All rights reserved.";

    /// <summary>Mandatory credit line shown in the About dialog, splash screen and installer.</summary>
    public const string CreditLine = "Developed by Anwarul Haque (@HYPERMX7) | Contact: 7739399007";

    /// <summary>Short signature rendered in the status bar.</summary>
    public const string StatusBarSignature = "by @HYPERMX7";

    /// <summary>Public source repository.</summary>
    public const string RepositoryUrl = "https://github.com/artikri845427-ctrl/HYPER-PDF";

    /// <summary>Company / publisher name written to the Windows registry by the installer.</summary>
    public const string Publisher = "Anwarul Haque (@HYPERMX7)";

    /// <summary>Short application description used in shell metadata and the installer.</summary>
    public const string Description =
        "HyperPDF is a fast, modern Windows PDF reader with native Explorer thumbnail and preview support.";

    /// <summary>Title used for the main window when no document is open.</summary>
    public static string ApplicationTitle => $"{ApplicationName} {ApplicationVersion}";

    /// <summary>Title used for the main window when a document is open.</summary>
    public static string DocumentTitle(string documentName) =>
        string.IsNullOrWhiteSpace(documentName) ? ApplicationTitle : $"{documentName} - {ApplicationName}";

    /// <summary>Single-line attribution combining the developer name, handle and contact number.</summary>
    public static string Attribution => $"Developed by {DeveloperName} ({DeveloperHandle}) | Contact: {ContactNumber}";

    /// <summary>Version of the PDFium native engine bundled with HyperPDF.</summary>
    public const string PdfiumVersion = "158.0.8111";

    /// <summary>
    /// URL of the update manifest. The developer publishes a new update by bumping this JSON
    /// file and attaching the matching installer to a GitHub release.
    /// </summary>
    public const string UpdateCheckUrl = "https://raw.githubusercontent.com/artikri845427-ctrl/HYPER-PDF/main/update/update.json";
}

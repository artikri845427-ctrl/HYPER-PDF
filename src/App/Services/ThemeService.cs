// Author: Anwarul Haque (@HYPERMX7) | Contact: 7739399007
using System.Windows;
using HyperPDF.Models;

namespace HyperPDF.Services;

/// <summary>Applies the light and dark theme to the application resources.</summary>
public interface IThemeService
{
    /// <summary>The theme that is currently applied.</summary>
    AppTheme CurrentTheme { get; }

    /// <summary>Applies a theme.</summary>
    void Apply(AppTheme theme);

    /// <summary>Raised after the theme changed.</summary>
    event EventHandler? ThemeChanged;
}

/// <summary>Swaps the theme resource dictionary of the running application.</summary>
public sealed class ThemeService : IThemeService
{
    private const string LightThemeUri = "/HyperPDF;component/Themes/Light.xaml";
    private const string DarkThemeUri = "/HyperPDF;component/Themes/Dark.xaml";

    /// <inheritdoc />
    public AppTheme CurrentTheme { get; private set; } = AppTheme.Light;

    /// <inheritdoc />
    public event EventHandler? ThemeChanged;

    /// <inheritdoc />
    public void Apply(AppTheme theme)
    {
        CurrentTheme = theme;

        Application? application = Application.Current;
        if (application is null)
        {
            return;
        }

        string uri = theme == AppTheme.Dark ? DarkThemeUri : LightThemeUri;

        var dictionary = new ResourceDictionary
        {
            Source = new Uri(uri, UriKind.Relative)
        };

        var merged = application.Resources.MergedDictionaries;
        for (int i = merged.Count - 1; i >= 0; i--)
        {
            var existing = merged[i];
            if (existing is ResourceDictionary candidate &&
                candidate.Source is Uri source &&
                (source.OriginalString.Contains("Themes/Light.xaml", StringComparison.OrdinalIgnoreCase) ||
                 source.OriginalString.Contains("Themes/Dark.xaml", StringComparison.OrdinalIgnoreCase)))
            {
                merged.RemoveAt(i);
            }
        }

        merged.Add(dictionary);
        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }
}

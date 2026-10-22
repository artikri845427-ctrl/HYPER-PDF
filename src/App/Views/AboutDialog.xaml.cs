// Author: Anwarul Haque (@HYPERMX7) | Contact: 7739399007
using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace HyperPDF.Views;

/// <summary>About dialog that shows the complete HyperPDF branding.</summary>
public partial class AboutDialog : Window
{
    /// <summary>Creates the dialog.</summary>
    public AboutDialog()
    {
        InitializeComponent();

        // Absolute external URIs are rejected by the XAML loader when set in markup,
        // so the repository link is assigned here instead.
        try
        {
            RepositoryLink.NavigateUri = new Uri(BrandInfo.RepositoryUrl, UriKind.Absolute);
        }
        catch (UriFormatException)
        {
            // Leave the link inert if the configured URL is malformed.
        }
    }

    /// <summary>Shows the about dialog.</summary>
    public static void Show(string title)
    {
        var dialog = new AboutDialog
        {
            Owner = Application.Current?.MainWindow
        };

        if (!string.IsNullOrWhiteSpace(title))
        {
            dialog.Title = title;
        }

        dialog.ShowDialog();
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

    private void OnCheckUpdatesClick(object sender, RoutedEventArgs e)
    {
        if (AppServices.Current is not null)
        {
            UpdateDialog.ShowCheck(AppServices.Current);
        }
    }

    private void OnRepositoryNavigate(object sender, RequestNavigateEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            // The browser could not be started.
        }

        e.Handled = true;
    }
}

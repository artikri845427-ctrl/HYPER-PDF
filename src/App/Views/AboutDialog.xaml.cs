// Author: Anwarul Haque (@HYPERMX7) | Contact: 7739399007
using System.Windows;

namespace HyperPDF.Views;

/// <summary>About dialog that shows the complete HyperPDF branding.</summary>
public partial class AboutDialog : Window
{
    /// <summary>Creates the dialog.</summary>
    public AboutDialog()
    {
        InitializeComponent();
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
}

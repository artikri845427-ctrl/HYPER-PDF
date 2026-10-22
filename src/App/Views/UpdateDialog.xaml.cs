// Author: Anwarul Haque (@HYPERMX7) | Contact: 7739399007
using System.Windows;
using HyperPDF.Services;
using HyperPDF.ViewModels;

namespace HyperPDF.Views;

/// <summary>Modal dialog driving the automatic update flow of HyperPDF.</summary>
public partial class UpdateDialog : Window
{
    private UpdateDialog(UpdateViewModel model)
    {
        InitializeComponent();
        DataContext = model;
        model.CloseRequested += OnModelCloseRequested;
        Closing += (_, _) => model.CloseRequested -= OnModelCloseRequested;
    }

    /// <summary>Shows the dialog with a manifest that has already been fetched.</summary>
    public static void ShowAvailable(UpdateManifest manifest, AppServices services)
    {
        ShowWith(services, new UpdateViewModel(services.Updates, services.Settings, manifest));
    }

    /// <summary>Shows the dialog and performs a fresh check; used by the About dialog.</summary>
    public static void ShowCheck(AppServices services)
    {
        ShowWith(services, new UpdateViewModel(services.Updates, services.Settings));
    }

    private static void ShowWith(AppServices services, UpdateViewModel model)
    {
        var dialog = new UpdateDialog(model)
        {
            Owner = Application.Current?.MainWindow
        };
        dialog.ShowDialog();
    }

    private void OnModelCloseRequested(object? sender, EventArgs e) => Close();
}

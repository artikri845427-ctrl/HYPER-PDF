// Author: Anwarul Haque (@HYPERMX7) | Contact: 7739399007
using System.Windows;

namespace HyperPDF.Views;

/// <summary>Dialog that shows the properties of the current document.</summary>
public partial class DocumentInfoDialog : Window
{
    /// <summary>Creates the dialog.</summary>
    public DocumentInfoDialog()
    {
        InitializeComponent();
    }

    /// <summary>Shows the dialog for a document.</summary>
    public static Task ShowAsync(ViewModels.DocumentInfoViewModel viewModel)
    {
        var dialog = new DocumentInfoDialog
        {
            Owner = Application.Current?.MainWindow,
            DataContext = viewModel
        };

        dialog.ShowDialog();
        return Task.CompletedTask;
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
}

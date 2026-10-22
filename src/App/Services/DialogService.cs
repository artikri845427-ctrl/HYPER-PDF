// Author: Anwarul Haque (@HYPERMX7) | Contact: 7739399007
using System.Windows;
using HyperPDF.Views;

namespace HyperPDF.Services;

/// <summary>Shows modal dialogs to the user.</summary>
public interface IDialogService
{
    /// <summary>Shows an error message.</summary>
    void ShowError(string message, string? details = null);

    /// <summary>Shows an informational message.</summary>
    void ShowInformation(string message, string? details = null);

    /// <summary>Asks a yes/no question.</summary>
    bool Confirm(string message, string? details = null);

    /// <summary>Asks for a password. Returns <c>null</c> when the user cancelled.</summary>
    string? PromptPassword(string message);
}

/// <summary>WPF implementation of <see cref="IDialogService"/>.</summary>
public sealed class DialogService : IDialogService
{
    /// <inheritdoc />
    public void ShowError(string message, string? details = null) =>
        Show(message, details, MessageBoxImage.Error, MessageBoxButton.OK);

    /// <inheritdoc />
    public void ShowInformation(string message, string? details = null) =>
        Show(message, details, MessageBoxImage.Information, MessageBoxButton.OK);

    /// <inheritdoc />
    public bool Confirm(string message, string? details = null) =>
        Show(message, details, MessageBoxImage.Question, MessageBoxButton.YesNo) == MessageBoxResult.Yes;

    /// <inheritdoc />
    public string? PromptPassword(string message)
    {
        var dialog = new PasswordDialog
        {
            Owner = Application.Current?.MainWindow
        };

        dialog.SetMessage(message);
        return dialog.ShowDialog() == true ? dialog.Password : null;
    }

    private static MessageBoxResult Show(
        string message,
        string? details,
        MessageBoxImage image,
        MessageBoxButton buttons)
    {
        string caption = BrandInfo.ApplicationName;
        string text = string.IsNullOrWhiteSpace(details) ? message : message + Environment.NewLine + Environment.NewLine + details;

        Window? owner = Application.Current?.MainWindow;
        if (owner is not null && owner.IsVisible)
        {
            return MessageBox.Show(owner, text, caption, buttons, image);
        }

        return MessageBox.Show(text, caption, buttons, image);
    }
}

// Author: Anwarul Haque (@HYPERMX7) | Contact: 7739399007
using System.Windows;
using System.Windows.Input;

namespace HyperPDF.Views;

/// <summary>Modal dialog that asks for the password of an encrypted document.</summary>
public partial class PasswordDialog : Window
{
    /// <summary>Creates the dialog.</summary>
    public PasswordDialog()
    {
        InitializeComponent();
        Loaded += (_, _) => PasswordBox.Focus();
    }

    /// <summary>The password entered by the user, or an empty string.</summary>
    public string Password => PasswordBox.Password;

    /// <summary>Sets the message that explains why the password is required.</summary>
    public void SetMessage(string message) => MessageText.Text = message;

    private void OnOkClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void OnPasswordKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            DialogResult = true;
            e.Handled = true;
        }
    }
}

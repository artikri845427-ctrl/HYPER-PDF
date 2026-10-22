// Author: Anwarul Haque (@HYPERMX7) | Contact: 7739399007
using System.Windows;
using System.Windows.Threading;
using HyperPDF.Services;
using HyperPDF.ViewModels;
using HyperPDF.Views;

namespace HyperPDF;

/// <summary>Application entry point of HyperPDF.</summary>
public partial class App : Application
{
    private SplashWindow? _splash;

    /// <summary>The service container used by the application.</summary>
    public AppServices Services { get; private set; } = null!;

    /// <inheritdoc />
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;

        _splash = new SplashWindow();
        _splash.Show();

        Services = AppServices.CreateDefault();
        await Services.Settings.LoadAsync().ConfigureAwait(true);
        Services.Theme.Apply(Services.Settings.Settings.Theme);

        var mainWindow = new MainWindow(Services);
        MainWindow = mainWindow;
        mainWindow.Show();

        _splash.Close();
        _splash = null;

        foreach (string argument in e.Args)
        {
            if (argument.StartsWith('-'))
            {
                continue;
            }

            await mainWindow.OpenPathAsync(argument).ConfigureAwait(true);
        }
    }

    /// <inheritdoc />
    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            Services.Settings.SaveAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // The preferences could not be written; the application still shuts down cleanly.
        }

        Services.Dispose();
        base.OnExit(e);
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = true;

        string message = "An unexpected error occurred.";
        string details = e.Exception?.Message ?? string.Empty;

        try
        {
            Services.Dialogs.ShowError(message, details);
        }
        catch (Exception)
        {
            // The dialog itself failed - there is nothing left to do.
        }
    }

    private static void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            try
            {
                MessageBox.Show(
                    exception.Message,
                    BrandInfo.ApplicationName,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception)
            {
                // Ignore.
            }
        }
    }
}

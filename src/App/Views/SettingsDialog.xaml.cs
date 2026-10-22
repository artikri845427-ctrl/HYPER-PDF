// Author: Anwarul Haque (@HYPERMX7) | Contact: 7739399007
using System.Windows;
using System.Windows.Controls;
using HyperPDF.Models;
using HyperPDF.Services;
using HyperPDF.ViewModels;

namespace HyperPDF.Views;

/// <summary>Dialog that exposes the user preferences of HyperPDF.</summary>
public partial class SettingsDialog : Window
{
    private readonly AppServices _services;

    /// <summary>Creates the dialog.</summary>
    public SettingsDialog(AppServices services)
    {
        _services = services;
        InitializeComponent();
        DataContext = new SettingsViewModel(services);

        Loaded += OnLoaded;
    }

    /// <summary>Shows the settings dialog.</summary>
    public static void ShowSettings()
    {
        AppServices? services = AppServices.Current;
        if (services is null)
        {
            return;
        }

        var dialog = new SettingsDialog(services)
        {
            Owner = Application.Current?.MainWindow
        };

        dialog.ShowDialog();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        SettingsViewModel viewModel = (SettingsViewModel)DataContext;

        LightThemeButton.IsChecked = viewModel.Theme == AppTheme.Light;
        DarkThemeButton.IsChecked = viewModel.Theme == AppTheme.Dark;
        ContinuousButton.IsChecked = viewModel.ViewMode == ViewMode.Continuous;
        SinglePageButton.IsChecked = viewModel.ViewMode == ViewMode.SinglePage;
        RestorePositionCheckBox.IsChecked = viewModel.RestoreLastPosition;
        ThumbnailsCheckBox.IsChecked = viewModel.ShowThumbnails;
        StatusBarCheckBox.IsChecked = viewModel.ShowStatusBar;
    }

    private void OnLightThemeChecked(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel viewModel && viewModel.Theme != AppTheme.Light)
        {
            viewModel.Theme = AppTheme.Light;
        }
    }

    private void OnDarkThemeChecked(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel viewModel && viewModel.Theme != AppTheme.Dark)
        {
            viewModel.Theme = AppTheme.Dark;
        }
    }

    private void OnContinuousChecked(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel viewModel && viewModel.ViewMode != ViewMode.Continuous)
        {
            viewModel.ViewMode = ViewMode.Continuous;
        }
    }

    private void OnSinglePageChecked(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel viewModel && viewModel.ViewMode != ViewMode.SinglePage)
        {
            viewModel.ViewMode = ViewMode.SinglePage;
        }
    }

    private void OnRestorePositionChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel viewModel)
        {
            viewModel.RestoreLastPosition = RestorePositionCheckBox.IsChecked == true;
        }
    }

    private void OnThumbnailsChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel viewModel)
        {
            viewModel.ShowThumbnails = ThumbnailsCheckBox.IsChecked == true;
        }
    }

    private void OnStatusBarChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel viewModel)
        {
            viewModel.ShowStatusBar = StatusBarCheckBox.IsChecked == true;
        }
    }

    private void OnRemoveRecentClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is RecentFileEntry entry && DataContext is SettingsViewModel viewModel)
        {
            viewModel.RemoveRecentCommand.Execute(entry);
        }
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
}

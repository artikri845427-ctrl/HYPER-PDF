// Author: Anwarul Haque (@HYPERMX7) | Contact: 7739399007
using System.Diagnostics;
using System.IO;

namespace HyperPDF.Services;

/// <summary>Opens files and folders with the Windows shell.</summary>
public interface IShellService
{
    /// <summary>Reveals a file in Windows Explorer.</summary>
    void ShowInExplorer(string filePath);

    /// <summary>Opens a folder in Windows Explorer.</summary>
    void OpenFolder(string folderPath);

    /// <summary>Opens the Windows "Default apps" settings page.</summary>
    void OpenDefaultAppsSettings();
}

/// <summary>Default <see cref="IShellService"/> implementation.</summary>
public sealed class ShellService : IShellService
{
    /// <inheritdoc />
    public void ShowInExplorer(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return;
        }

        try
        {
            using Process explorer = new();
            explorer.StartInfo.FileName = "explorer.exe";
            explorer.StartInfo.Arguments = "/select,\"" + filePath + "\"";
            explorer.StartInfo.UseShellExecute = true;
            explorer.Start();
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException or IOException)
        {
            // Explorer could not be started - nothing else to do here.
        }
    }

    /// <inheritdoc />
    public void OpenFolder(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
        {
            return;
        }

        try
        {
            using Process explorer = new();
            explorer.StartInfo.FileName = folderPath;
            explorer.StartInfo.UseShellExecute = true;
            explorer.Start();
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException or IOException)
        {
            // Ignore: the folder could not be opened.
        }
    }

    /// <inheritdoc />
    public void OpenDefaultAppsSettings()
    {
        // The registered application query parameter focuses the "Set defaults" list on
        // HyperPDF. Older Windows versions simply ignore the parameter.
        const string uri = "ms-settings:defaultapps?registeredAppUser=HyperPDF&registeredAppMachine=HyperPDF";
        try
        {
            using Process process = new();
            process.StartInfo.FileName = uri;
            process.StartInfo.UseShellExecute = true;
            process.Start();
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            try
            {
                using Process fallback = new();
                fallback.StartInfo.FileName = "ms-settings:defaultapps";
                fallback.StartInfo.UseShellExecute = true;
                fallback.Start();
            }
            catch (Exception inner) when (inner is System.ComponentModel.Win32Exception or InvalidOperationException)
            {
                // Ignore.
            }
        }
    }
}

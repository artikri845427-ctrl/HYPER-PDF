// Author: Anwarul Haque (@HYPERMX7) | Contact: 7739399007
using HyperPDF.Services.Pdfium;
using HyperPDF.ViewModels;

namespace HyperPDF.Services;

/// <summary>Opens PDF documents and turns them into view models.</summary>
public interface IDocumentService
{
    /// <summary>
    /// Opens a document. Returns <c>null</c> when the user cancelled the password prompt or
    /// the document could not be opened.
    /// </summary>
    Task<DocumentViewModel?> OpenAsync(string filePath);
}

/// <summary>Default implementation of <see cref="IDocumentService"/>.</summary>
public sealed class DocumentService : IDocumentService
{
    private readonly ISettingsService _settingsService;
    private readonly IPrintService _printService;
    private readonly IDialogService _dialogService;

    /// <summary>Creates the service.</summary>
    public DocumentService(ISettingsService settingsService, IPrintService printService, IDialogService dialogService)
    {
        _settingsService = settingsService;
        _printService = printService;
        _dialogService = dialogService;
    }

    /// <inheritdoc />
    public Task<DocumentViewModel?> OpenAsync(string filePath)
    {
        return Task.Run(() => Open(filePath));
    }

    private DocumentViewModel? Open(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return null;
        }

        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(filePath);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            _dialogService.ShowError($"'{filePath}' is not a valid file path.");
            return null;
        }

        if (!File.Exists(fullPath))
        {
            _dialogService.ShowError($"The file '{Path.GetFileName(fullPath)}' does not exist.");
            return null;
        }

        PdfDocument? document = null;
        try
        {
            document = PdfDocument.Open(fullPath);
        }
        catch (PdfOpenException ex) when (ex.Status == PdfOpenStatus.PasswordRequired)
        {
            string? password = _dialogService.PromptPassword(
                $"'{Path.GetFileName(fullPath)}' is protected by a password.\nEnter the password to open the document.");

            if (string.IsNullOrEmpty(password))
            {
                return null;
            }

            try
            {
                document = PdfDocument.Open(fullPath, password);
            }
            catch (PdfOpenException retry)
            {
                _dialogService.ShowError("The document could not be opened.", retry.Message);
                return null;
            }
        }
        catch (PdfOpenException ex)
        {
            _dialogService.ShowError("The document could not be opened.", ex.Message);
            return null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _dialogService.ShowError(
                $"'{Path.GetFileName(fullPath)}' could not be read.",
                "The file may be locked by another application or you may not have permission to read it.");
            return null;
        }

        var viewModel = new DocumentViewModel(document, _settingsService, _printService, _dialogService);
        viewModel.RestoreSessionState();
        return viewModel;
    }
}

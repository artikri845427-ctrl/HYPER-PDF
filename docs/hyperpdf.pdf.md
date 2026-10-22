# HyperPDF architecture

© 2026 Anwarul Haque (@HYPERMX7). All rights reserved. | Contact: 7739399007

HyperPDF 1.0.0 consists of three parts that share one PDFium build.

## 1. The application (WPF, .NET 8)

```
src/App
├── Views          MainWindow, DocumentView and the other XAML windows
├── Controls       PdfPageView and the reusable WPF controls
├── ViewModels     the MVVM models of the windows, the document and the settings
├── Models         Document, Page, SearchHits and the other data types
└── Services       Pdfium engine, settings, recent files, metadata scanner
```

* **MVVM**: the views only bind, every behaviour lives in a view model.
* **PDFium**: the managed engine of the `DocumentViewModel` loads, renders, extracts
  and searches the text of the document with PDFium (158.0.8111).
* **Branding**: `src/App/BrandInfo.cs` mirrors the constants of `Directory.Build.props`,
  the branding test of the test project keeps them in sync.

## 2. The native Shell extensions (C++, CMake)

```
build/native/x64   HyperPDFThumbnailProvider.dll, HyperPDFPreviewHandler.dll, pdfium.dll
build/native/x86   the same, with the 32 bit PDFium build
```

* **Thumbnail provider** — `IThumbnailProvider` + `IInitializeWithFile`. `GetThumbnail(cx, ...)`
  renders the first page into a 32 bit DIB section and returns it together with
  `WTSAT_RGB`. The registration writes:
  * `HKCR\CLSID\{6F3B1C74-2A85-4E19-9C0D-8E5A47B3D962}\InprocServer32`
  * `HKCR\.pdf\ShellEx\{e357fccd-a995-4576-b01f-234630154e96}` → class id
  * `HKCR\ThumbnailProvider\{class id}`
  * the cache key `...\Explorer\ThumbnailProviderCache\{class id}`
* **Preview handler** — `IPreviewHandler` + `IPreviewHandlerVisuals` + `IInitializeWithFile`.
  It renders the current page of the document into a child window of the preview pane and
  pages with the mouse wheel and the scroll bar. The registration writes:
  * `HKCR\CLSID\{D4A72E58-1B93-4C6F-A2D8-93C0E71B4A57}\InprocServer32`
  * `HKCR\.pdf\ShellEx\{e2a9679d-b18b-4337-9215-7c4dd6526e2b}` → class id
  * `HKCR\Software\Microsoft\Windows\CurrentVersion\PreviewHandlers\{class id}`
* **PDFium at runtime** — both extensions resolve every PDFium entry point at runtime
  (`LoadLibraryExW` from the directory of the extension). The DLL is therefore loadable by
  the Explorer even when PDFium is missing, and the x86 extension keeps using the 32 bit
  PDFium build that matches its own architecture.
* **Exports** — the module definition files keep the four COM entry points
  (`DllGetClassObject`, `DllCanUnloadNow`, `DllRegisterServer`, `DllUnregisterServer`)
  free of decorations on x86 and x64.

## 3. The installer (Inno Setup)

```
%ProgramFiles%\HyperPDF            HyperPDF.exe, HyperPDF.dll, pdfium.dll
%ProgramFiles%\HyperPDF\Shell\x64  the 64 bit Shell extensions
%ProgramFiles%\HyperPDF\Shell\x86  the 32 bit Shell extensions
```

`installer\build-installer.ps1` reads the branding from `Directory.Build.props`, generates
`installer\brand.iss` and calls the Inno Setup compiler. The installer registers the four
Shell extension DLLs with `regsvr32`, adds HyperPDF to the *Open with* list of PDF documents
and offers the download of the .NET 8 Desktop Runtime when it is missing.

## Test suite (xUnit)

`src/App/HyperPDF.Tests` covers the branding, the page range parser, the settings, the
recent files, the metadata scanner and the complete PDFium pipeline of the application
(open, page count, page size, render, text extraction, search, highlighting).

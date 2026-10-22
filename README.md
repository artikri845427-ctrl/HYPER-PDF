# HyperPDF

**HyperPDF** is a fast, modern PDF reader for Windows 10 and Windows 11. It is written in
C# with WPF and renders the documents with [PDFium](https://pdfium.googlesource.com/pdfium/).
Next to the application HyperPDF installs two native Shell extensions, so Windows Explorer
shows a thumbnail and a preview of a PDF file without opening it.

* Product: **HyperPDF**
* Version: **1.0.0**
* Author: **Anwarul Haque (@HYPERMX7)**
* Contact: **7739399007**
* Copyright: **© 2026 Anwarul Haque (@HYPERMX7). All rights reserved.**

Developed by Anwarul Haque (@HYPERMX7) | Contact: 7739399007

---

## Features

### Application

* Open single documents or all documents of a folder, with a continuous scroll view.
* Zoom between 10 % and 400 %, fit to width, fit to page and automatic page fitting.
* Rotate the pages, page navigation, and a page overview of the current document.
* Full text search including a list of all hits and highlighting inside the pages.
* Print the document, copy the selected text, and copy a page as an image.
* Recent files, favourites and a session that is restored after a restart.
* Dark and light theme, keyboard shortcuts and a command palette.

### Shell extensions

* **Thumbnail provider** renders the first page of a PDF for the Windows Explorer.
* **Preview handler** shows the pages of a PDF in the preview pane of the Explorer.
* Both extensions are written in C++ and load `pdfium.dll` at runtime, so the Explorer can
  load them even when PDFium is missing. The x64 and the x86 extension use their own
  matching PDFium build.

## Requirements

* Windows 10 (1809) or newer, 64 bit
* .NET 8 Desktop Runtime (the installer offers the download when it is missing)

## Installation

Download `HyperPDF-Setup-1.0.0.exe` and run it. The installer

* installs the application into `%ProgramFiles%\HyperPDF`,
* installs the Shell extensions into `%ProgramFiles%\HyperPDF\Shell\x64` and `...\Shell\x86`,
* registers the Shell extensions with `regsvr32`,
* adds HyperPDF to the *Open with* list of PDF documents,
* creates the start menu entry and, optionally, a desktop shortcut.

Uninstalling removes all of it again.

## Building from the source

```powershell
# The application (framework dependent, the .NET 8 Desktop Runtime is needed)
tools\build-app.ps1

# The native Shell extensions for x64 and x86
tools\build-native.ps1

# The installer, it calls both build scripts above
installer\build-installer.ps1
```

Requirements for the build:

* .NET 8 SDK
* Visual Studio Build Tools with the *Desktop development with C++* workload
* CMake 3.20 or newer
* Inno Setup 6 (only for the installer)

The PDFium binaries are downloaded by `tools\fetch-pdfium.ps1`, the pinned release is
`158.0.8111`.

## Tests

```powershell
dotnet test HyperPDF.sln
```

The test project covers the branding, the page range parser, the settings, the recent files,
the metadata scanner and the complete PDFium pipeline (open, page count, page size, render,
text extraction, search and highlighting).

## Repository layout

| Path                        | Content                                                     |
| --------------------------- | ----------------------------------------------------------- |
| `src/App`                   | The WPF application (MVVM).                                  |
| `src/App/HyperPDF.Tests`    | The xUnit test project.                                      |
| `src/ThumbnailProvider`     | The native Shell extensions (C++).                           |
| `tools`                     | Build scripts for the application, the extensions and PDFium. |
| `installer`                 | The Inno Setup script and its build script.                  |
| `assets`                    | The icon and the sample document.                            |
| `docs`                      | The additional documentation.                                 |

## License

HyperPDF is released under the MIT License, see [LICENSE](LICENSE).

Third party components:

* [PDFium](https://pdfium.googlesource.com/pdfium/) – BSD 3-Clause License
* [bblanchon.PDFium.Win32](https://www.nuget.org/packages/bblanchon.PDFium.Win32) – MIT License
* [Inno Setup](https://jrsoftware.org/isinfo.php) – used to build the installer

© 2026 Anwarul Haque (@HYPERMX7). All rights reserved.

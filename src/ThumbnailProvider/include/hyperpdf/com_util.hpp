// Author: Anwarul Haque (@HYPERMX7) | Contact: 7739399007
//
// Small helpers that are shared by the thumbnail provider and the preview handler.

#ifndef HYPERPDF_COM_UTIL_HPP
#define HYPERPDF_COM_UTIL_HPP

#include <windows.h>

#include <objbase.h>

#include <string>

namespace hyperpdf {
namespace com {

/// Returns the module file name of the given module (or of the executable that hosts the code).
std::wstring ModulePath(HMODULE module);

/// Returns the module that contains the given code address.
HMODULE ModuleOfAddress(const void* address);

/// Returns the directory that contains the given module.
std::wstring ModuleDirectory(HMODULE module);

/// Converts a UTF-16 string to UTF-8 (the encoding expected by PDFium).
std::string ToUtf8(const std::wstring& value);

/// Converts a UTF-8 string to UTF-16.
std::wstring FromUtf8(const std::string& value);

/// Parses a CLSID from its textual representation, for example "{00000000-0000-0000-0000-000000000000}".
bool ParseClsid(const wchar_t* text, CLSID* clsid);

/// Writes a CLSID into the "{...}" form.
std::wstring FormatClsid(const CLSID& clsid);

/// Registers a COM class in HKCR\CLSID\{clsid}.
/// The implementation of the class is stored as an InprocServer32 entry.
bool RegisterComServer(const CLSID& clsid, const std::wstring& description, const std::wstring& dllPath);

/// Removes a COM class from HKCR\CLSID\{clsid}.
void UnregisterComServer(const CLSID& clsid);

/// Registers the association of a Shell extension with a file extension.
/// The shellExId is the well known identifier of the extension point, for example
/// IID_IThumbnailProvider or IID_IShellPreviewHandler. The classId is the class that
/// implements the extension.
/// For example: HKCR\.pdf\ShellEx\{e357fccd-a995-4576-b01f-234630154e96} = {classid}.
bool RegisterShellExtension(const wchar_t* fileExtension, const wchar_t* shellExId, const CLSID& classId);

/// Removes a Shell extension association.
void UnregisterShellExtension(const wchar_t* fileExtension, const wchar_t* shellExId);

/// Registers an arbitrary registry value below HKEY_CLASSES_ROOT.
bool RegisterRootValue(const wchar_t* subKey, const wchar_t* valueName, const std::wstring& data);

/// Removes an arbitrary registry value below HKEY_CLASSES_ROOT.
void UnregisterRootValue(const wchar_t* subKey, const wchar_t* valueName);

}  // namespace com
}  // namespace hyperpdf

#endif  // HYPERPDF_COM_UTIL_HPP

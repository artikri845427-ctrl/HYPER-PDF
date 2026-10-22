// Author: Anwarul Haque (@HYPERMX7) | Contact: 7739399007
//
// Entry points of HyperPDFThumbnailProvider.dll.

#include <windows.h>

#include <new>

#include "hyperpdf/brand.hpp"
#include "hyperpdf/class_factory.hpp"
#include "hyperpdf/com_util.hpp"
#include "thumbnail_provider.hpp"

namespace {

/// Class identifier of the HyperPDF thumbnail provider.
const wchar_t* const kThumbnailProviderClsid = L"{6F3B1C74-2A85-4E19-9C0D-8E5A47B3D962}";

}  // namespace

BOOL WINAPI DllMain(HINSTANCE instance, DWORD reason, LPVOID) {
    if (reason == DLL_PROCESS_ATTACH) {
        DisableThreadLibraryCalls(instance);
    }

    return TRUE;
}

STDAPI DllGetClassObject(REFCLSID rclsid, REFIID riid, LPVOID* object) {
    if (object == nullptr) {
        return E_POINTER;
    }

    *object = nullptr;

    CLSID clsid = {};
    if (FAILED(::CLSIDFromString(kThumbnailProviderClsid, &clsid)) || clsid != rclsid) {
        return CLASS_E_CLASSNOTAVAILABLE;
    }

    auto* factory = new (std::nothrow) hyperpdf::ClassFactory<hyperpdf::ThumbnailProvider>();
    if (factory == nullptr) {
        return E_OUTOFMEMORY;
    }

    const HRESULT result = factory->QueryInterface(riid, object);
    factory->Release();
    return result;
}

STDAPI DllCanUnloadNow() { return S_OK; }

STDAPI DllRegisterServer() {
    const HMODULE module = ::GetModuleHandleW(L"HyperPDFThumbnailProvider.dll");
    const std::wstring path = hyperpdf::com::ModulePath(module);
    if (path.empty()) {
        return E_FAIL;
    }

    CLSID clsid = {};
    if (FAILED(::CLSIDFromString(kThumbnailProviderClsid, &clsid))) {
        return E_FAIL;
    }

    if (!hyperpdf::com::RegisterComServer(clsid, L"HyperPDF Thumbnail Provider", path)) {
        return E_FAIL;
    }

    // Windows 7 and newer look the provider up below \ThumbnailProvider.
    if (!hyperpdf::com::RegisterRootValue(L"ThumbnailProvider\\{6F3B1C74-2A85-4E19-9C0D-8E5A47B3D962}", nullptr,
                                          kThumbnailProviderClsid)) {
        return E_FAIL;
    }

    // The cache key makes Explorer reuse the rendered thumbnails.
    if (!hyperpdf::com::RegisterRootValue(
            L"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\ThumbnailProviderCache\\"
            L"{6F3B1C74-2A85-4E19-9C0D-8E5A47B3D962}",
            nullptr, hyperpdf::brand::kProductName)) {
        return E_FAIL;
    }

    if (!hyperpdf::com::RegisterShellExtension(hyperpdf::brand::kPdfExtension,
                                               L"{e357fccd-a995-4576-b01f-234630154e96}", clsid)) {
        return E_FAIL;
    }

    return S_OK;
}

STDAPI DllUnregisterServer() {
    CLSID clsid = {};
    if (FAILED(::CLSIDFromString(kThumbnailProviderClsid, &clsid))) {
        return E_FAIL;
    }

    hyperpdf::com::UnregisterShellExtension(hyperpdf::brand::kPdfExtension,
                                            L"{e357fccd-a995-4576-b01f-234630154e96}");
    hyperpdf::com::UnregisterRootValue(
        L"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\ThumbnailProviderCache\\"
        L"{6F3B1C74-2A85-4E19-9C0D-8E5A47B3D962}",
        nullptr);
    hyperpdf::com::UnregisterRootValue(L"ThumbnailProvider\\{6F3B1C74-2A85-4E19-9C0D-8E5A47B3D962}", nullptr);
    hyperpdf::com::UnregisterComServer(clsid);

    return S_OK;
}

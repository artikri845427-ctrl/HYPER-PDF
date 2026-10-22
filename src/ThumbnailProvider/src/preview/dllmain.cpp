// Author: Anwarul Haque (@HYPERMX7) | Contact: 7739399007
//
// Entry points of HyperPDFPreviewHandler.dll.

#include <windows.h>

#include <new>

#include "hyperpdf/brand.hpp"
#include "hyperpdf/class_factory.hpp"
#include "hyperpdf/com_util.hpp"
#include "preview_handler.hpp"

namespace {

/// Class identifier of the HyperPDF preview handler.
const wchar_t* const kPreviewHandlerClsid = L"{D4A72E58-1B93-4C6F-A2D8-93C0E71B4A57}";

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
    if (FAILED(::CLSIDFromString(kPreviewHandlerClsid, &clsid)) || clsid != rclsid) {
        return CLASS_E_CLASSNOTAVAILABLE;
    }

    auto* factory = new (std::nothrow) hyperpdf::ClassFactory<hyperpdf::PreviewHandler>();
    if (factory == nullptr) {
        return E_OUTOFMEMORY;
    }

    const HRESULT result = factory->QueryInterface(riid, object);
    factory->Release();
    return result;
}

STDAPI DllCanUnloadNow() { return S_OK; }

STDAPI DllRegisterServer() {
    const HMODULE module = ::GetModuleHandleW(L"HyperPDFPreviewHandler.dll");
    const std::wstring path = hyperpdf::com::ModulePath(module);
    if (path.empty()) {
        return E_FAIL;
    }

    CLSID clsid = {};
    if (FAILED(::CLSIDFromString(kPreviewHandlerClsid, &clsid))) {
        return E_FAIL;
    }

    if (!hyperpdf::com::RegisterComServer(clsid, L"HyperPDF Preview Handler", path)) {
        return E_FAIL;
    }

    // The Shell looks the preview handler up below this key.
    if (!hyperpdf::com::RegisterRootValue(L"Software\\Microsoft\\Windows\\CurrentVersion\\PreviewHandlers\\"
                                          L"{D4A72E58-1B93-4C6F-A2D8-93C0E71B4A57}",
                                          nullptr, kPreviewHandlerClsid)) {
        return E_FAIL;
    }

    if (!hyperpdf::com::RegisterShellExtension(hyperpdf::brand::kPdfExtension,
                                               L"{e2a9679d-b18b-4337-9215-7c4dd6526e2b}", clsid)) {
        return E_FAIL;
    }

    return S_OK;
}

STDAPI DllUnregisterServer() {
    CLSID clsid = {};
    if (FAILED(::CLSIDFromString(kPreviewHandlerClsid, &clsid))) {
        return E_FAIL;
    }

    hyperpdf::com::UnregisterShellExtension(hyperpdf::brand::kPdfExtension,
                                            L"{e2a9679d-b18b-4337-9215-7c4dd6526e2b}");
    hyperpdf::com::UnregisterRootValue(L"Software\\Microsoft\\Windows\\CurrentVersion\\PreviewHandlers\\"
                                       L"{D4A72E58-1B93-4C6F-A2D8-93C0E71B4A57}",
                                       nullptr);
    hyperpdf::com::UnregisterComServer(clsid);

    return S_OK;
}

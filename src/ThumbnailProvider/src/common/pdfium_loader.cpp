// Author: Anwarul Haque (@HYPERMX7) | Contact: 7739399007
#include "hyperpdf/pdfium_loader.hpp"

#include <string>

namespace hyperpdf {
namespace {

/// Resolves an entry point and reports whether it exists.
template <typename T>
bool ResolveEntry(HMODULE module, const char* name, T* target) {
    auto proc = reinterpret_cast<T>(GetProcAddress(module, name));
    if (proc == nullptr) {
        return false;
    }

    *target = proc;
    return true;
}

}  // namespace

PdfiumLibrary& PdfiumLibrary::Instance() {
    static PdfiumLibrary instance;
    return instance;
}

PdfiumLibrary::~PdfiumLibrary() { Unload(); }

void PdfiumLibrary::Reset() {
    InitLibrary = nullptr;
    DestroyLibrary = nullptr;
    LoadDocument = nullptr;
    GetPageCount = nullptr;
    LoadPage = nullptr;
    GetPageWidthF = nullptr;
    GetPageHeightF = nullptr;
    ClosePage = nullptr;
    CloseDocument = nullptr;
    GetLastError = nullptr;
    GetSecurityHandlerRevision = nullptr;
    BitmapCreate = nullptr;
    BitmapFillRect = nullptr;
    BitmapDestroy = nullptr;
    BitmapGetBuffer = nullptr;
    BitmapGetStride = nullptr;
    RenderPageBitmap = nullptr;
    ready_ = false;
    library_initialised_ = false;
}

bool PdfiumLibrary::Load(HMODULE module) {
    if (ready_) {
        return true;
    }

    HMODULE candidate = nullptr;

    // 1. The directory of the Shell extension itself.
    if (module != nullptr) {
        wchar_t path[MAX_PATH] = {0};
        const DWORD length = GetModuleFileNameW(module, path, MAX_PATH);
        if (length > 0 && length < MAX_PATH) {
            std::wstring directory(path);
            const size_t separator = directory.find_last_of(L"\\/");
            if (separator != std::wstring::npos) {
                directory.resize(separator + 1);
                candidate = LoadLibraryExW((directory + L"pdfium.dll").c_str(), nullptr,
                                           LOAD_LIBRARY_SEARCH_DEFAULT_DIRS);
            }
        }
    }

    // 2. The directory of the hosting executable.
    if (candidate == nullptr) {
        wchar_t path[MAX_PATH] = {0};
        const DWORD length = GetModuleFileNameW(nullptr, path, MAX_PATH);
        if (length > 0 && length < MAX_PATH) {
            std::wstring directory(path);
            const size_t separator = directory.find_last_of(L"\\/");
            if (separator != std::wstring::npos) {
                directory.resize(separator + 1);
                candidate = LoadLibraryExW((directory + L"pdfium.dll").c_str(), nullptr,
                                           LOAD_LIBRARY_SEARCH_DEFAULT_DIRS);
            }
        }
    }

    // 3. The regular search path.
    if (candidate == nullptr) {
        candidate = LoadLibraryW(L"pdfium.dll");
    }

    if (candidate == nullptr) {
        return false;
    }

    if (!Resolve(candidate)) {
        FreeLibrary(candidate);
        return false;
    }

    module_ = candidate;
    owns_module_ = true;
    ready_ = true;

    if (InitLibrary != nullptr) {
        InitLibrary();
        library_initialised_ = true;
    }

    return true;
}

void PdfiumLibrary::Unload() {
    if (library_initialised_ && DestroyLibrary != nullptr) {
        DestroyLibrary();
        library_initialised_ = false;
    }

    if (module_ != nullptr && owns_module_) {
        FreeLibrary(module_);
    }

    module_ = nullptr;
    owns_module_ = false;
    Reset();
}

bool PdfiumLibrary::Resolve(HMODULE module) {
    bool complete = true;

    complete &= ResolveEntry(module, "FPDF_InitLibrary", &InitLibrary);
    complete &= ResolveEntry(module, "FPDF_DestroyLibrary", &DestroyLibrary);
    complete &= ResolveEntry(module, "FPDF_LoadDocument", &LoadDocument);
    complete &= ResolveEntry(module, "FPDF_GetPageCount", &GetPageCount);
    complete &= ResolveEntry(module, "FPDF_LoadPage", &LoadPage);
    complete &= ResolveEntry(module, "FPDF_GetPageWidthF", &GetPageWidthF);
    complete &= ResolveEntry(module, "FPDF_GetPageHeightF", &GetPageHeightF);
    complete &= ResolveEntry(module, "FPDF_ClosePage", &ClosePage);
    complete &= ResolveEntry(module, "FPDF_CloseDocument", &CloseDocument);
    complete &= ResolveEntry(module, "FPDF_GetLastError", &GetLastError);
    complete &= ResolveEntry(module, "FPDF_GetSecurityHandlerRevision", &GetSecurityHandlerRevision);
    complete &= ResolveEntry(module, "FPDFBitmap_Create", &BitmapCreate);
    complete &= ResolveEntry(module, "FPDFBitmap_FillRect", &BitmapFillRect);
    complete &= ResolveEntry(module, "FPDFBitmap_Destroy", &BitmapDestroy);
    complete &= ResolveEntry(module, "FPDFBitmap_GetBuffer", &BitmapGetBuffer);
    complete &= ResolveEntry(module, "FPDFBitmap_GetStride", &BitmapGetStride);
    complete &= ResolveEntry(module, "FPDF_RenderPageBitmap", &RenderPageBitmap);

    if (!complete) {
        Reset();
    }

    return complete;
}

}  // namespace hyperpdf

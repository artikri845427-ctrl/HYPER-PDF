// Author: Anwarul Haque (@HYPERMX7) | Contact: 7739399007
//
// Runtime loader for the PDFium engine.

#ifndef HYPERPDF_PDFIUM_LOADER_HPP
#define HYPERPDF_PDFIUM_LOADER_HPP

#include <windows.h>

#include "hyperpdf/pdfium_api.hpp"

namespace hyperpdf {

/// Loads pdfium.dll on demand and exposes the entry points that the Shell extensions need.
class PdfiumLibrary {
public:
    /// Returns the process wide instance.
    static PdfiumLibrary& Instance();

    /// Loads the PDFium library that sits next to the given module.
    /// The lookup order is: the directory of the module, the directory of the executable and
    /// finally the standard search path of the operating system.
    bool Load(HMODULE module);

    /// True when every required entry point could be resolved.
    bool IsReady() const { return ready_; }

    /// Releases the library. The function pointers stay valid until the library is loaded again.
    void Unload();

    pdfium::FPDF_InitLibrary_t InitLibrary = nullptr;
    pdfium::FPDF_DestroyLibrary_t DestroyLibrary = nullptr;
    pdfium::FPDF_LoadDocument_t LoadDocument = nullptr;
    pdfium::FPDF_GetPageCount_t GetPageCount = nullptr;
    pdfium::FPDF_LoadPage_t LoadPage = nullptr;
    pdfium::FPDF_GetPageWidthF_t GetPageWidthF = nullptr;
    pdfium::FPDF_GetPageHeightF_t GetPageHeightF = nullptr;
    pdfium::FPDF_ClosePage_t ClosePage = nullptr;
    pdfium::FPDF_CloseDocument_t CloseDocument = nullptr;
    pdfium::FPDF_GetLastError_t GetLastError = nullptr;
    pdfium::FPDF_GetSecurityHandlerRevision_t GetSecurityHandlerRevision = nullptr;
    pdfium::FPDFBitmap_Create_t BitmapCreate = nullptr;
    pdfium::FPDFBitmap_FillRect_t BitmapFillRect = nullptr;
    pdfium::FPDFBitmap_Destroy_t BitmapDestroy = nullptr;
    pdfium::FPDFBitmap_GetBuffer_t BitmapGetBuffer = nullptr;
    pdfium::FPDFBitmap_GetStride_t BitmapGetStride = nullptr;
    pdfium::FPDF_RenderPageBitmap_t RenderPageBitmap = nullptr;

private:
    PdfiumLibrary() = default;
    ~PdfiumLibrary();

    PdfiumLibrary(const PdfiumLibrary&) = delete;
    PdfiumLibrary& operator=(const PdfiumLibrary&) = delete;

    bool Resolve(HMODULE module);
    void Reset();

    HMODULE module_ = nullptr;
    bool owns_module_ = false;
    bool ready_ = false;
    bool library_initialised_ = false;
};

}  // namespace hyperpdf

#endif  // HYPERPDF_PDFIUM_LOADER_HPP

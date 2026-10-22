// Author: Anwarul Haque (@HYPERMX7) | Contact: 7739399007
//
// Minimal declaration of the PDFium API that the Shell extensions use.
//
// The extensions load pdfium.dll at runtime instead of linking against it. That keeps the
// extensions loadable even when PDFium is missing (Explorer would otherwise refuse to load
// the extension) and it allows the x64 and the x86 extension to use their own PDFium build.
//
// The pinned PDFium release (158.0.8111) exports the public API with the cdecl convention
// on Windows, which is the default calling convention of the compiler.

#ifndef HYPERPDF_PDFIUM_API_HPP
#define HYPERPDF_PDFIUM_API_HPP

#include <windows.h>

namespace hyperpdf {
namespace pdfium {

using FPDF_DOCUMENT = void*;
using FPDF_PAGE = void*;
using FPDF_BITMAP = void*;

using FPDF_InitLibrary_t = void(__cdecl*)();
using FPDF_DestroyLibrary_t = void(__cdecl*)();
using FPDF_LoadDocument_t = FPDF_DOCUMENT(__cdecl*)(const char* file_path, const char* password);
using FPDF_GetPageCount_t = int(__cdecl*)(FPDF_DOCUMENT document);
using FPDF_LoadPage_t = FPDF_PAGE(__cdecl*)(FPDF_DOCUMENT document, int page_index);
using FPDF_GetPageWidthF_t = float(__cdecl*)(FPDF_PAGE page);
using FPDF_GetPageHeightF_t = float(__cdecl*)(FPDF_PAGE page);
using FPDF_ClosePage_t = void(__cdecl*)(FPDF_PAGE page);
using FPDF_CloseDocument_t = void(__cdecl*)(FPDF_DOCUMENT document);
using FPDF_GetLastError_t = unsigned long(__cdecl*)();
using FPDF_GetSecurityHandlerRevision_t = int(__cdecl*)(FPDF_DOCUMENT document);
using FPDFBitmap_Create_t = FPDF_BITMAP(__cdecl*)(int width, int height, int alpha);
using FPDFBitmap_FillRect_t = void(__cdecl*)(FPDF_BITMAP bitmap, int left, int top, int width, int height,
                                             unsigned long color);
using FPDFBitmap_Destroy_t = void(__cdecl*)(FPDF_BITMAP bitmap);
using FPDFBitmap_GetBuffer_t = void*(__cdecl*)(FPDF_BITMAP bitmap);
using FPDFBitmap_GetStride_t = int(__cdecl*)(FPDF_BITMAP bitmap);
using FPDF_RenderPageBitmap_t = void(__cdecl*)(FPDF_BITMAP bitmap, FPDF_PAGE page, int start_x, int start_y,
                                               int size_x, int size_y, int rotate, int flags);

// Flags accepted by FPDF_RenderPageBitmap.
inline constexpr int kRenderAnnot = 0x01;

}  // namespace pdfium
}  // namespace hyperpdf

#endif  // HYPERPDF_PDFIUM_API_HPP

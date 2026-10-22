// Author: Anwarul Haque (@HYPERMX7) | Contact: 7739399007
#include "thumbnail_provider.hpp"

#include <algorithm>
#include <cstring>
#include <vector>

#include "hyperpdf/com_util.hpp"
#include "hyperpdf/pdfium_loader.hpp"

namespace hyperpdf {
namespace {

/// Renders the first page of a document and returns the pixel data (BGRA, top down).
struct RenderedThumbnail {
    int width = 0;
    int height = 0;
    int stride = 0;
    std::vector<unsigned char> pixels;
};

/// Renders the first page of a PDF file into a BGRA buffer.
bool RenderFirstPage(const std::wstring& filePath, int targetWidth, RenderedThumbnail* result) {
    if (result == nullptr) {
        return false;
    }

    PdfiumLibrary& library = PdfiumLibrary::Instance();
    if (!library.Load(com::ModuleOfAddress(reinterpret_cast<const void*>(&RenderFirstPage)))) {
        return false;
    }

    const std::string utf8Path = com::ToUtf8(filePath);
    if (utf8Path.empty()) {
        return false;
    }

    pdfium::FPDF_DOCUMENT document = library.LoadDocument(utf8Path.c_str(), "");
    if (document == nullptr) {
        return false;
    }

    pdfium::FPDF_PAGE page = nullptr;
    pdfium::FPDF_BITMAP bitmap = nullptr;
    bool success = false;

    const int pageCount = library.GetPageCount(document);
    if (pageCount > 0) {
        page = library.LoadPage(document, 0);
    }

    if (page != nullptr) {
        const float pageWidth = library.GetPageWidthF(page);
        const float pageHeight = library.GetPageHeightF(page);

        if (pageWidth > 0.0f && pageHeight > 0.0f && targetWidth > 0) {
            // The Shell only provides the maximum width, the height follows the aspect ratio
            // of the page.
            const double scale = static_cast<double>(targetWidth) / pageWidth;

            int width = static_cast<int>(pageWidth * scale + 0.5);
            int height = static_cast<int>(pageHeight * scale + 0.5);
            width = std::max(1, std::min(width, targetWidth));
            height = std::max(1, height);

            // alpha = 1 keeps the page opaque, the background is filled with white.
            bitmap = library.BitmapCreate(width, height, 1);
            if (bitmap != nullptr) {
                library.BitmapFillRect(bitmap, 0, 0, width, height, 0xFFFFFFFFUL);
                library.RenderPageBitmap(bitmap, page, 0, 0, width, height, 0, pdfium::kRenderAnnot);

                void* buffer = library.BitmapGetBuffer(bitmap);
                const int stride = library.BitmapGetStride(bitmap);
                if (buffer != nullptr && stride >= width * 4) {
                    const size_t required = static_cast<size_t>(stride) * static_cast<size_t>(height);
                    result->pixels.resize(required);
                    std::memcpy(result->pixels.data(), buffer, required);
                    result->width = width;
                    result->height = height;
                    result->stride = stride;
                    success = true;
                }
            }
        }
    }

    if (bitmap != nullptr) {
        library.BitmapDestroy(bitmap);
    }

    if (page != nullptr) {
        library.ClosePage(page);
    }

    library.CloseDocument(document);
    return success;
}

}  // namespace

HRESULT STDMETHODCALLTYPE ThumbnailProvider::QueryInterface(REFIID riid, void** object) {
    if (object == nullptr) {
        return E_POINTER;
    }

    *object = nullptr;

    if (riid == IID_IUnknown || riid == IID_IThumbnailProvider) {
        *object = static_cast<IThumbnailProvider*>(this);
        AddRef();
        return S_OK;
    }

    if (riid == IID_IInitializeWithFile) {
        *object = static_cast<IInitializeWithFile*>(this);
        AddRef();
        return S_OK;
    }

    return E_NOINTERFACE;
}

ULONG STDMETHODCALLTYPE ThumbnailProvider::AddRef() { return references_.fetch_add(1) + 1; }

ULONG STDMETHODCALLTYPE ThumbnailProvider::Release() {
    const ULONG remaining = references_.fetch_sub(1) - 1;
    if (remaining == 0) {
        delete this;
    }

    return remaining;
}

HRESULT STDMETHODCALLTYPE ThumbnailProvider::Initialize(LPCWSTR file_path, DWORD) {
    if (file_path == nullptr) {
        file_path_ = std::wstring();
        return E_INVALIDARG;
    }

    file_path_ = std::wstring(file_path);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE ThumbnailProvider::GetThumbnail(UINT cx, HBITMAP* bitmap, WTS_ALPHATYPE* alpha) {
    if (bitmap == nullptr) {
        return E_INVALIDARG;
    }

    if (cx == 0) {
        return E_INVALIDARG;
    }

    if (file_path_.empty()) {
        return E_FAIL;
    }

    RenderedThumbnail thumbnail;
    if (!RenderFirstPage(file_path_, static_cast<int>(cx), &thumbnail)) {
        return E_FAIL;
    }

    // The Shell expects a DIB section with 32 bit pixels, PDFium already writes BGRA bytes.
    BITMAPINFO info = {};
    info.bmiHeader.biSize = sizeof(BITMAPINFOHEADER);
    info.bmiHeader.biWidth = thumbnail.width;
    info.bmiHeader.biHeight = -thumbnail.height;  // top down
    info.bmiHeader.biPlanes = 1;
    info.bmiHeader.biBitCount = 32;
    info.bmiHeader.biCompression = BI_RGB;

    void* bits = nullptr;
    HDC screen = ::GetDC(nullptr);
    HBITMAP result = ::CreateDIBSection(screen, &info, DIB_RGB_COLORS, &bits, nullptr, 0);
    ::ReleaseDC(nullptr, screen);

    if (result == nullptr || bits == nullptr) {
        if (result != nullptr) {
            ::DeleteObject(result);
        }

        return E_OUTOFMEMORY;
    }

    const size_t rowSize = static_cast<size_t>(thumbnail.width) * 4;
    for (int row = 0; row < thumbnail.height; ++row) {
        const unsigned char* source =
            thumbnail.pixels.data() + (static_cast<size_t>(row) * static_cast<size_t>(thumbnail.stride));
        unsigned char* destination = static_cast<unsigned char*>(bits) + (static_cast<size_t>(row) * rowSize);
        std::memcpy(destination, source, rowSize);

        // The thumbnail is opaque, therefore the alpha channel is forced.
        for (int x = 3; x < static_cast<int>(rowSize); x += 4) {
            destination[x] = 0xFF;
        }
    }

    *bitmap = result;

    if (alpha != nullptr) {
        *alpha = WTSAT_RGB;
    }

    return S_OK;
}

}  // namespace hyperpdf

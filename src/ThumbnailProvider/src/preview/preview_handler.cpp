// Author: Anwarul Haque (@HYPERMX7) | Contact: 7739399007
#include "preview_handler.hpp"

#include <algorithm>
#include <cstring>
#include <vector>

#include "hyperpdf/brand.hpp"
#include "hyperpdf/com_util.hpp"
#include "hyperpdf/pdfium_loader.hpp"

namespace hyperpdf {
namespace {

constexpr wchar_t kWindowClass[] = L"HyperPDFPreviewHandlerWindow";
constexpr UINT_PTR kScrollBarId = 1;

ATOM g_window_class = 0;

HMODULE PreviewModule() { return ::GetModuleHandleW(L"HyperPDFPreviewHandler.dll"); }

/// Registers the window class of the preview surface.
bool EnsureWindowClass() {
    if (g_window_class != 0) {
        return true;
    }

    WNDCLASSEXW wc = {};
    wc.cbSize = sizeof(wc);
    wc.style = CS_GLOBALCLASS | CS_HREDRAW | CS_VREDRAW;
    wc.lpfnWndProc = PreviewWindowProc;
    wc.hInstance = PreviewModule();
    wc.hCursor = ::LoadCursorW(nullptr, IDC_ARROW);
    wc.hbrBackground = reinterpret_cast<HBRUSH>(COLOR_WINDOW + 1);
    wc.lpszClassName = kWindowClass;

    g_window_class = ::RegisterClassExW(&wc);
    return g_window_class != 0;
}

/// Renders a page of a document into a 32 bit DIB section.
HBITMAP RenderPageToBitmap(PdfiumLibrary& library, const std::wstring& filePath, int pageIndex, int width,
                            int height, int* bitmapWidth, int* bitmapHeight) {
    const std::string utf8Path = com::ToUtf8(filePath);
    if (utf8Path.empty()) {
        return nullptr;
    }

    pdfium::FPDF_DOCUMENT document = library.LoadDocument(utf8Path.c_str(), "");
    if (document == nullptr) {
        return nullptr;
    }

    HBITMAP result = nullptr;
    pdfium::FPDF_PAGE page = nullptr;
    pdfium::FPDF_BITMAP pdfiumBitmap = nullptr;

    const int pageCount = library.GetPageCount(document);
    if (pageIndex < 0 || pageIndex >= pageCount) {
        library.CloseDocument(document);
        return nullptr;
    }

    page = library.LoadPage(document, pageIndex);

    if (page != nullptr) {
        const float pageWidth = library.GetPageWidthF(page);
        const float pageHeight = library.GetPageHeightF(page);

        if (pageWidth > 0.0f && pageHeight > 0.0f && width > 0 && height > 0) {
            const double scale =
                std::min(static_cast<double>(width) / pageWidth, static_cast<double>(height) / pageHeight);
            const int renderWidth = std::max(1, static_cast<int>(pageWidth * scale + 0.5));
            const int renderHeight = std::max(1, static_cast<int>(pageHeight * scale + 0.5));

            pdfiumBitmap = library.BitmapCreate(renderWidth, renderHeight, 1);
            if (pdfiumBitmap != nullptr) {
                library.BitmapFillRect(pdfiumBitmap, 0, 0, renderWidth, renderHeight, 0xFFFFFFFFUL);
                library.RenderPageBitmap(pdfiumBitmap, page, 0, 0, renderWidth, renderHeight, 0,
                                        pdfium::kRenderAnnot);

                void* buffer = library.BitmapGetBuffer(pdfiumBitmap);
                const int stride = library.BitmapGetStride(pdfiumBitmap);

                if (buffer != nullptr && stride >= renderWidth * 4) {
                    BITMAPINFO info = {};
                    info.bmiHeader.biSize = sizeof(BITMAPINFOHEADER);
                    info.bmiHeader.biWidth = renderWidth;
                    info.bmiHeader.biHeight = -renderHeight;  // top down
                    info.bmiHeader.biPlanes = 1;
                    info.bmiHeader.biBitCount = 32;
                    info.bmiHeader.biCompression = BI_RGB;

                    void* bits = nullptr;
                    HDC screen = ::GetDC(nullptr);
                    result = ::CreateDIBSection(screen, &info, DIB_RGB_COLORS, &bits, nullptr, 0);
                    ::ReleaseDC(nullptr, screen);

                    if (result != nullptr && bits != nullptr) {
                        for (int row = 0; row < renderHeight; ++row) {
                            const unsigned char* source =
                                static_cast<const unsigned char*>(buffer) + static_cast<size_t>(row) * stride;
                            unsigned char* destination =
                                static_cast<unsigned char*>(bits) +
                                (static_cast<size_t>(row) * static_cast<size_t>(renderWidth) * 4);
                            std::memcpy(destination, source, static_cast<size_t>(renderWidth) * 4);

                            // The preview surface is opaque, therefore the alpha channel is forced.
                            for (int x = 3; x < renderWidth * 4; x += 4) {
                                destination[x] = 0xFF;
                            }
                        }

                        if (bitmapWidth != nullptr) {
                            *bitmapWidth = renderWidth;
                        }

                        if (bitmapHeight != nullptr) {
                            *bitmapHeight = renderHeight;
                        }
                    } else if (result != nullptr) {
                        ::DeleteObject(result);
                        result = nullptr;
                    }
                }
            }
        }
    }

    if (pdfiumBitmap != nullptr) {
        library.BitmapDestroy(pdfiumBitmap);
    }

    if (page != nullptr) {
        library.ClosePage(page);
    }

    library.CloseDocument(document);
    return result;
}

}  // namespace

// The window procedure of the preview surface. It forwards the interesting messages to the
// PreviewHandler instance that created the window.
LRESULT CALLBACK PreviewWindowProc(HWND window, UINT message, WPARAM wparam, LPARAM lparam) {
    auto* handler = reinterpret_cast<PreviewHandler*>(::GetWindowLongPtrW(window, GWLP_USERDATA));

    switch (message) {
        case WM_CREATE: {
            auto* create = reinterpret_cast<CREATESTRUCTW*>(lparam);
            ::SetWindowLongPtrW(window, GWLP_USERDATA, reinterpret_cast<LONG_PTR>(create->lpCreateParams));
            return 0;
        }

        case WM_ERASEBKGND:
            return 1;

        case WM_PAINT: {
            PAINTSTRUCT paint = {};
            HDC context = ::BeginPaint(window, &paint);
            RECT client = {};
            ::GetClientRect(window, &client);

            if (handler != nullptr) {
                handler->Paint(context, client);
            } else {
                ::FillRect(context, &client, reinterpret_cast<HBRUSH>(COLOR_WINDOW + 1));
            }

            ::EndPaint(window, &paint);
            return 0;
        }

        case WM_SIZE: {
            if (handler != nullptr) {
                handler->Resize();
            }

            return 0;
        }

        case WM_VSCROLL: {
            if (handler != nullptr) {
                handler->wparam_ = wparam;
                handler->Scroll(LOWORD(wparam));
            }

            return 0;
        }

        case WM_MOUSEWHEEL: {
            if (handler != nullptr) {
                const int delta = GET_WHEEL_DELTA_WPARAM(wparam);
                handler->Scroll(delta > 0 ? SB_LINEUP : SB_LINEDOWN);
            }

            return 0;
        }

        case WM_KEYDOWN: {
            if (handler != nullptr) {
                switch (wparam) {
                    case VK_PRIOR:
                    case VK_UP:
                        handler->Scroll(SB_LINEUP);
                        return 0;
                    case VK_NEXT:
                    case VK_DOWN:
                        handler->Scroll(SB_LINEDOWN);
                        return 0;
                    default:
                        break;
                }
            }

            break;
        }

        default:
            break;
    }

    return ::DefWindowProcW(window, message, wparam, lparam);
}

void PreviewHandler::Paint(HDC context, const RECT& client) {
    const COLORREF background = background_color_;
    ::FillRect(context, &client, reinterpret_cast<HBRUSH>(static_cast<ULONG_PTR>(background) + 1));

    if (default_preview_) {
        // The default preview shows the name of the document and the product name.
        std::wstring fileName = file_path_;
        const size_t separator = fileName.find_last_of(L"\\/");
        if (separator != std::wstring::npos) {
            fileName = fileName.substr(separator + 1);
        }

        const std::wstring caption = fileName.empty() ? std::wstring(brand::kProductName) : fileName;

        ::SetBkMode(context, TRANSPARENT);
        ::SetTextColor(context, text_color_);
        if (font_.lfHeight != 0) {
            HFONT font = ::CreateFontIndirectW(&font_);
            if (font != nullptr) {
                HGDIOBJ previous = ::SelectObject(context, font);
                RECT text = client;
                ::DrawTextW(context, caption.c_str(), static_cast<int>(caption.length()), &text,
                            DT_CENTER | DT_SINGLELINE | DT_VCENTER | DT_END_ELLIPSIS);
                ::SelectObject(context, previous);
                ::DeleteObject(font);
                return;
            }
        }

        RECT text = client;
        ::DrawTextW(context, caption.c_str(), static_cast<int>(caption.length()), &text, DT_CENTER | DT_SINGLELINE);
        return;
    }

    if (page_bitmap_ == nullptr) {
        return;
    }

    const int x = (client.right - client.left - page_bitmap_width_) / 2;
    const int y = (client.bottom - client.top - page_bitmap_height_) / 2;

    HDC memory = ::CreateCompatibleDC(context);
    if (memory == nullptr) {
        return;
    }

    HGDIOBJ previous = ::SelectObject(memory, page_bitmap_);
    ::BitBlt(context, x, y, page_bitmap_width_, page_bitmap_height_, memory, 0, 0, SRCCOPY);
    ::SelectObject(memory, previous);
    ::DeleteDC(memory);
}

void PreviewHandler::Resize() {
    if (preview_window_ == nullptr || default_preview_ || file_path_.empty()) {
        return;
    }

    RECT client = {};
    ::GetClientRect(preview_window_, &client);
    const int width = client.right - client.left;
    const int height = client.bottom - client.top;

    if (width <= 0 || height <= 0) {
        return;
    }

    if (page_bitmap_ != nullptr) {
        ::DeleteObject(page_bitmap_);
        page_bitmap_ = nullptr;
    }

    page_bitmap_ = RenderPageToBitmap(PdfiumLibrary::Instance(), file_path_, page_index_, width, height,
                                      &page_bitmap_width_, &page_bitmap_height_);

    ::InvalidateRect(preview_window_, nullptr, TRUE);
}

void PreviewHandler::Scroll(int command) {
    if (page_count_ <= 0) {
        return;
    }

    int target = page_index_;

    switch (command) {
        case SB_LINEUP:
            target = page_index_ - 1;
            break;
        case SB_LINEDOWN:
            target = page_index_ + 1;
            break;
        case SB_PAGEUP:
            target = page_index_ - 1;
            break;
        case SB_PAGEDOWN:
            target = page_index_ + 1;
            break;
        case SB_THUMBPOSITION:
            target = static_cast<int>(LOWORD(wparam_));
            break;
        default:
            break;
    }

    if (target < 0) {
        target = 0;
    }

    if (target > page_count_ - 1) {
        target = page_count_ - 1;
    }

    if (target == page_index_) {
        return;
    }

    page_index_ = target;
    Resize();
}

HRESULT STDMETHODCALLTYPE PreviewHandler::QueryInterface(REFIID riid, void** object) {
    if (object == nullptr) {
        return E_POINTER;
    }

    *object = nullptr;

    if (riid == IID_IUnknown || riid == IID_IPreviewHandler) {
        *object = static_cast<IPreviewHandler*>(this);
        AddRef();
        return S_OK;
    }

    if (riid == IID_IPreviewHandlerVisuals) {
        *object = static_cast<IPreviewHandlerVisuals*>(this);
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

ULONG STDMETHODCALLTYPE PreviewHandler::AddRef() { return references_.fetch_add(1) + 1; }

ULONG STDMETHODCALLTYPE PreviewHandler::Release() {
    const ULONG remaining = references_.fetch_sub(1) - 1;
    if (remaining == 0) {
        delete this;
    }

    return remaining;
}

HRESULT STDMETHODCALLTYPE PreviewHandler::Initialize(LPCWSTR file_path, DWORD) {
    if (file_path == nullptr) {
        file_path_ = std::wstring();
        return E_INVALIDARG;
    }

    file_path_ = std::wstring(file_path);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE PreviewHandler::SetWindow(HWND window, const RECT* rect) {
    host_window_ = window;

    if (rect != nullptr) {
        host_rect_ = *rect;
    }

    return S_OK;
}

HRESULT STDMETHODCALLTYPE PreviewHandler::SetRect(const RECT* rect) {
    if (rect != nullptr) {
        host_rect_ = *rect;
    }

    if (preview_window_ != nullptr) {
        ::SetWindowPos(preview_window_, nullptr, host_rect_.left, host_rect_.top,
                       host_rect_.right - host_rect_.left, host_rect_.bottom - host_rect_.top,
                       SWP_NOZORDER | SWP_NOACTIVATE);
        ::InvalidateRect(preview_window_, nullptr, TRUE);
    }

    return S_OK;
}

HRESULT STDMETHODCALLTYPE PreviewHandler::DoPreview() {
    if (host_window_ == nullptr || file_path_.empty()) {
        return E_FAIL;
    }

    if (!EnsureWindowClass()) {
        return E_FAIL;
    }

    PdfiumLibrary& library = PdfiumLibrary::Instance();
    if (!library.Load(com::ModuleOfAddress(reinterpret_cast<const void*>(&EnsureWindowClass)))) {
        return E_FAIL;
    }

    const std::string utf8Path = com::ToUtf8(file_path_);
    if (utf8Path.empty()) {
        return E_FAIL;
    }

    page_count_ = 0;
    page_index_ = 0;
    default_preview_ = false;

    pdfium::FPDF_DOCUMENT document = library.LoadDocument(utf8Path.c_str(), "");
    if (document != nullptr) {
        page_count_ = library.GetPageCount(document);
        library.CloseDocument(document);
    }

    if (page_count_ <= 0) {
        // The document cannot be rendered, the default preview is shown instead.
        default_preview_ = true;
    }

    if (preview_window_ == nullptr) {
        preview_window_ = ::CreateWindowExW(0, kWindowClass, brand::kProductName, WS_CHILD | WS_VISIBLE | WS_TABSTOP,
                                            host_rect_.left, host_rect_.top, host_rect_.right - host_rect_.left,
                                            host_rect_.bottom - host_rect_.top, host_window_, nullptr, PreviewModule(),
                                            this);

        if (preview_window_ == nullptr) {
            return E_FAIL;
        }
    } else {
        ::SetWindowPos(preview_window_, nullptr, host_rect_.left, host_rect_.top, host_rect_.right - host_rect_.left,
                       host_rect_.bottom - host_rect_.top, SWP_NOZORDER | SWP_NOACTIVATE);
    }

    Resize();
    ::InvalidateRect(preview_window_, nullptr, TRUE);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE PreviewHandler::Unload() {
    if (preview_window_ != nullptr) {
        ::DestroyWindow(preview_window_);
        preview_window_ = nullptr;
    }

    if (scroll_bar_ != nullptr) {
        ::DestroyWindow(scroll_bar_);
        scroll_bar_ = nullptr;
    }

    if (page_bitmap_ != nullptr) {
        ::DeleteObject(page_bitmap_);
        page_bitmap_ = nullptr;
    }

    page_count_ = 0;
    page_index_ = 0;
    host_window_ = nullptr;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE PreviewHandler::SetFocus() {
    if (preview_window_ != nullptr) {
        ::SetFocus(preview_window_);
    }

    return S_OK;
}

HRESULT STDMETHODCALLTYPE PreviewHandler::QueryFocus(HWND* window) {
    if (window == nullptr) {
        return E_POINTER;
    }

    *window = preview_window_ != nullptr ? preview_window_ : host_window_;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE PreviewHandler::TranslateAccelerator(MSG* message) {
    if (message == nullptr) {
        return E_INVALIDARG;
    }

    if (message->message == WM_KEYDOWN) {
        switch (message->wParam) {
            case VK_PRIOR:
            case VK_UP:
                Scroll(SB_LINEUP);
                return S_OK;
            case VK_NEXT:
            case VK_DOWN:
                Scroll(SB_LINEDOWN);
                return S_OK;
            default:
                break;
        }
    }

    return S_FALSE;
}

HRESULT STDMETHODCALLTYPE PreviewHandler::SetBackgroundColor(COLORREF color) {
    background_color_ = color;
    if (preview_window_ != nullptr) {
        ::InvalidateRect(preview_window_, nullptr, TRUE);
    }

    return S_OK;
}

HRESULT STDMETHODCALLTYPE PreviewHandler::SetFont(const LOGFONTW* font) {
    if (font == nullptr) {
        return E_INVALIDARG;
    }

    font_ = *font;
    if (preview_window_ != nullptr) {
        ::InvalidateRect(preview_window_, nullptr, TRUE);
    }

    return S_OK;
}

HRESULT STDMETHODCALLTYPE PreviewHandler::SetTextColor(COLORREF color) {
    text_color_ = color;
    if (preview_window_ != nullptr) {
        ::InvalidateRect(preview_window_, nullptr, TRUE);
    }

    return S_OK;
}

}  // namespace hyperpdf

// Author: Anwarul Haque (@HYPERMX7) | Contact: 7739399007
//
// The HyperPDF preview handler: it renders the pages of a PDF file inside the preview pane
// of Windows Explorer.

#ifndef HYPERPDF_PREVIEW_HANDLER_HPP
#define HYPERPDF_PREVIEW_HANDLER_HPP

#include <windows.h>

#include <atomic>
#include <string>

#include <shobjidl.h>

namespace hyperpdf {

class PreviewHandler : public IPreviewHandler,
                        public IPreviewHandlerVisuals,
                        public IInitializeWithFile {
public:
    PreviewHandler() : references_(1) {}

    // IUnknown
    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, void** object) override;
    ULONG STDMETHODCALLTYPE AddRef() override;
    ULONG STDMETHODCALLTYPE Release() override;

    // IInitializeWithFile
    HRESULT STDMETHODCALLTYPE Initialize(LPCWSTR file_path, DWORD mode) override;

    // IPreviewHandler
    HRESULT STDMETHODCALLTYPE SetWindow(HWND window, const RECT* rect) override;
    HRESULT STDMETHODCALLTYPE SetRect(const RECT* rect) override;
    HRESULT STDMETHODCALLTYPE DoPreview() override;
    HRESULT STDMETHODCALLTYPE Unload() override;
    HRESULT STDMETHODCALLTYPE SetFocus() override;
    HRESULT STDMETHODCALLTYPE QueryFocus(HWND* window) override;
    HRESULT STDMETHODCALLTYPE TranslateAccelerator(MSG* message) override;

    // IPreviewHandlerVisuals
    HRESULT STDMETHODCALLTYPE SetBackgroundColor(COLORREF color) override;
    HRESULT STDMETHODCALLTYPE SetFont(const LOGFONTW* font) override;
    HRESULT STDMETHODCALLTYPE SetTextColor(COLORREF color) override;

    // Helpers that are used by the window procedure of the preview surface.
    void Paint(HDC context, const RECT& client);
    void Resize();
    void Scroll(int command);

    /// The scroll information of the last WM_VSCROLL message.
    WPARAM wparam_ = 0;

private:
    virtual ~PreviewHandler() = default;

    std::atomic<ULONG> references_;
    std::wstring file_path_;
    HWND host_window_ = nullptr;
    HWND preview_window_ = nullptr;
    HWND scroll_bar_ = nullptr;
    RECT host_rect_ = {0, 0, 0, 0};
    COLORREF background_color_ = RGB(255, 255, 255);
    COLORREF text_color_ = RGB(32, 32, 32);
    LOGFONTW font_ = {};
    HBITMAP page_bitmap_ = nullptr;
    int page_bitmap_width_ = 0;
    int page_bitmap_height_ = 0;
    int page_index_ = 0;
    int page_count_ = 0;
    bool default_preview_ = false;
};

/// Window procedure of the preview surface.
LRESULT CALLBACK PreviewWindowProc(HWND window, UINT message, WPARAM wparam, LPARAM lparam);

}  // namespace hyperpdf

#endif  // HYPERPDF_PREVIEW_HANDLER_HPP

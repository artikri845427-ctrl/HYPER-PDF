// Author: Anwarul Haque (@HYPERMX7) | Contact: 7739399007
//
// The HyperPDF thumbnail provider: it renders the first page of a PDF file into the
// thumbnail that Windows Explorer displays.

#ifndef HYPERPDF_THUMBNAIL_PROVIDER_HPP
#define HYPERPDF_THUMBNAIL_PROVIDER_HPP

#include <windows.h>

#include <atomic>
#include <string>

#include <shobjidl.h>
#include <thumbcache.h>

namespace hyperpdf {

class ThumbnailProvider : public IThumbnailProvider, public IInitializeWithFile {
public:
    ThumbnailProvider() : references_(1) {}

    // IUnknown
    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, void** object) override;
    ULONG STDMETHODCALLTYPE AddRef() override;
    ULONG STDMETHODCALLTYPE Release() override;

    // IInitializeWithFile
    HRESULT STDMETHODCALLTYPE Initialize(LPCWSTR file_path, DWORD mode) override;

    // IThumbnailProvider
    HRESULT STDMETHODCALLTYPE GetThumbnail(UINT cx, HBITMAP* bitmap, WTS_ALPHATYPE* alpha) override;

private:
    virtual ~ThumbnailProvider() = default;

    std::atomic<ULONG> references_;
    std::wstring file_path_;
};

}  // namespace hyperpdf

#endif  // HYPERPDF_THUMBNAIL_PROVIDER_HPP

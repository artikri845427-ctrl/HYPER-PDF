// Author: Anwarul Haque (@HYPERMX7) | Contact: 7739399007
//
// A minimal class factory that is shared by the thumbnail provider and the preview handler.

#ifndef HYPERPDF_CLASS_FACTORY_HPP
#define HYPERPDF_CLASS_FACTORY_HPP

#include <windows.h>

#include <objbase.h>

#include <atomic>
#include <new>

namespace hyperpdf {

/// Creates instances of a single COM class.
template <typename T>
class ClassFactory : public IClassFactory {
public:
    ClassFactory() : count_(1) {}

    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, void** object) override {
        if (object == nullptr) {
            return E_POINTER;
        }

        *object = nullptr;

        if (riid == IID_IUnknown || riid == IID_IClassFactory) {
            *object = static_cast<IClassFactory*>(this);
            AddRef();
            return S_OK;
        }

        return E_NOINTERFACE;
    }

    ULONG STDMETHODCALLTYPE AddRef() override { return static_cast<ULONG>(count_.fetch_add(1) + 1); }

    ULONG STDMETHODCALLTYPE Release() override {
        const LONG remaining = count_.fetch_sub(1) - 1;
        if (remaining == 0) {
            delete this;
        }

        return static_cast<ULONG>(remaining);
    }

    HRESULT STDMETHODCALLTYPE CreateInstance(IUnknown* outer, REFIID riid, void** object) override {
        if (object == nullptr) {
            return E_POINTER;
        }

        *object = nullptr;

        if (outer != nullptr) {
            return CLASS_E_NOAGGREGATION;
        }

        auto* instance = new (std::nothrow) T();
        if (instance == nullptr) {
            return E_OUTOFMEMORY;
        }

        const HRESULT result = instance->QueryInterface(riid, object);
        instance->Release();
        return result;
    }

    HRESULT STDMETHODCALLTYPE LockServer(BOOL lock) override {
        if (lock) {
            LockCount().fetch_add(1);
        } else {
            LockCount().fetch_sub(1);
        }

        return S_OK;
    }

    /// Number of active server locks of this class.
    static std::atomic<LONG>& LockCount() {
        static std::atomic<LONG> count{0};
        return count;
    }

protected:
    virtual ~ClassFactory() = default;

private:
    /// Number of outstanding references of the factory itself.
    std::atomic<LONG> count_;
};

}  // namespace hyperpdf

#endif  // HYPERPDF_CLASS_FACTORY_HPP

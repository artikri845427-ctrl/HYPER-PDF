// Author: Anwarul Haque (@HYPERMX7) | Contact: 7739399007
#include "hyperpdf/com_util.hpp"

#include <string>
#include <vector>

namespace hyperpdf {
namespace com {
namespace {

/// Creates a registry key below HKEY_CLASSES_ROOT.
HKEY CreateKey(const wchar_t* subKey) {
    HKEY key = nullptr;
    if (RegCreateKeyExW(HKEY_CLASSES_ROOT, subKey, 0, nullptr, REG_OPTION_NON_VOLATILE, KEY_WRITE, nullptr, &key,
                        nullptr) != ERROR_SUCCESS) {
        return nullptr;
    }

    return key;
}

/// Removes a registry key (recursively) below HKEY_CLASSES_ROOT.
void DeleteKeyTree(const wchar_t* subKey) {
    HKEY key = nullptr;
    if (RegOpenKeyExW(HKEY_CLASSES_ROOT, subKey, 0, KEY_READ | KEY_WRITE, &key) != ERROR_SUCCESS) {
        return;
    }

    // Enumerate and delete the sub keys first.
    for (;;) {
        wchar_t name[256] = {0};
        DWORD size = static_cast<DWORD>(sizeof(name) / sizeof(name[0]));
        LSTATUS status = RegEnumKeyExW(key, 0, name, &size, nullptr, nullptr, nullptr, nullptr);
        if (status != ERROR_SUCCESS) {
            break;
        }

        std::wstring child = std::wstring(subKey) + L"\\" + name;
        DeleteKeyTree(child.c_str());
    }

    RegCloseKey(key);
    RegDeleteKeyW(HKEY_CLASSES_ROOT, subKey);
}

/// Writes a string value.
bool SetStringValue(HKEY key, const wchar_t* valueName, const std::wstring& data) {
    return RegSetValueExW(key, valueName, 0, REG_SZ, reinterpret_cast<const BYTE*>(data.c_str()),
                          static_cast<DWORD>((data.length() + 1) * sizeof(wchar_t))) == ERROR_SUCCESS;
}

}  // namespace

std::wstring ModulePath(HMODULE module) {
    std::vector<wchar_t> buffer(MAX_PATH);
    for (;;) {
        const DWORD length =
            GetModuleFileNameW(module, buffer.data(), static_cast<DWORD>(buffer.size()));
        if (length == 0) {
            return std::wstring();
        }

        if (length < buffer.size() - 1) {
            return std::wstring(buffer.data(), length);
        }

        if (buffer.size() >= 32768) {
            return std::wstring();
        }

        buffer.resize(buffer.size() * 2);
    }
}

HMODULE ModuleOfAddress(const void* address) {
    HMODULE module = nullptr;
    if (::GetModuleHandleExW(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT,
                             static_cast<LPCWSTR>(address), &module) == 0) {
        return nullptr;
    }

    return module;
}

std::wstring ModuleDirectory(HMODULE module) {
    const std::wstring path = ModulePath(module);
    const size_t separator = path.find_last_of(L"\\/");
    if (separator == std::wstring::npos) {
        return std::wstring();
    }

    return path.substr(0, separator + 1);
}

std::string ToUtf8(const std::wstring& value) {
    if (value.empty()) {
        return std::string();
    }

    const int size = ::WideCharToMultiByte(CP_UTF8, 0, value.c_str(), static_cast<int>(value.length()), nullptr, 0,
                                           nullptr, nullptr);
    if (size <= 0) {
        return std::string();
    }

    std::string result(static_cast<size_t>(size), '\0');
    ::WideCharToMultiByte(CP_UTF8, 0, value.c_str(), static_cast<int>(value.length()), result.data(), size, nullptr,
                          nullptr);
    return result;
}

std::wstring FromUtf8(const std::string& value) {
    if (value.empty()) {
        return std::wstring();
    }

    const int size = ::MultiByteToWideChar(CP_UTF8, 0, value.c_str(), static_cast<int>(value.length()), nullptr, 0);
    if (size <= 0) {
        return std::wstring();
    }

    std::wstring result(static_cast<size_t>(size), L'\0');
    ::MultiByteToWideChar(CP_UTF8, 0, value.c_str(), static_cast<int>(value.length()), result.data(), size);
    return result;
}

bool ParseClsid(const wchar_t* text, CLSID* clsid) {
    if (text == nullptr || clsid == nullptr) {
        return false;
    }

    return SUCCEEDED(::CLSIDFromString(text, clsid));
}

std::wstring FormatClsid(const CLSID& clsid) {
    wchar_t buffer[64] = {0};
    ::StringFromGUID2(clsid, buffer, static_cast<int>(sizeof(buffer) / sizeof(buffer[0])));
    return std::wstring(buffer);
}

bool RegisterComServer(const CLSID& clsid, const std::wstring& description, const std::wstring& dllPath) {
    const std::wstring classKey = L"CLSID\\" + FormatClsid(clsid);
    const std::wstring serverKey = classKey + L"\\InprocServer32";

    HKEY key = CreateKey(serverKey.c_str());
    if (key == nullptr) {
        return false;
    }

    SetStringValue(key, nullptr, dllPath);
    SetStringValue(key, L"ThreadingModel", L"Both");
    RegCloseKey(key);

    HKEY classHandle = CreateKey(classKey.c_str());
    if (classHandle == nullptr) {
        return false;
    }

    SetStringValue(classHandle, nullptr, description);
    SetStringValue(classHandle, L"AppID", L"{9e4b3cd3-8056-467f-9954-6e44d12843c9}");
    RegCloseKey(classHandle);

    return true;
}

void UnregisterComServer(const CLSID& clsid) {
    const std::wstring classKey = L"CLSID\\" + FormatClsid(clsid);
    DeleteKeyTree(classKey.c_str());
}

bool RegisterShellExtension(const wchar_t* fileExtension, const wchar_t* shellExId, const CLSID& classId) {
    if (fileExtension == nullptr || shellExId == nullptr) {
        return false;
    }

    const std::wstring subKey = std::wstring(fileExtension) + L"\\ShellEx\\" + shellExId;
    return RegisterRootValue(subKey.c_str(), nullptr, FormatClsid(classId));
}

void UnregisterShellExtension(const wchar_t* fileExtension, const wchar_t* shellExId) {
    if (fileExtension == nullptr || shellExId == nullptr) {
        return;
    }

    const std::wstring subKey = std::wstring(fileExtension) + L"\\ShellEx\\" + shellExId;
    UnregisterRootValue(subKey.c_str(), nullptr);
}

bool RegisterRootValue(const wchar_t* subKey, const wchar_t* valueName, const std::wstring& data) {
    HKEY key = CreateKey(subKey);
    if (key == nullptr) {
        return false;
    }

    const bool result = SetStringValue(key, valueName, data);
    RegCloseKey(key);
    return result;
}

void UnregisterRootValue(const wchar_t* subKey, const wchar_t* valueName) {
    HKEY key = nullptr;
    if (RegOpenKeyExW(HKEY_CLASSES_ROOT, subKey, 0, KEY_SET_VALUE, &key) != ERROR_SUCCESS) {
        return;
    }

    RegDeleteValueW(key, valueName);
    RegCloseKey(key);

    // The key is removed as well when it does not contain any other value.
    HKEY empty = nullptr;
    if (RegOpenKeyExW(HKEY_CLASSES_ROOT, subKey, 0, KEY_READ, &empty) == ERROR_SUCCESS) {
        RegCloseKey(empty);
        RegDeleteKeyW(HKEY_CLASSES_ROOT, subKey);
    }
}

}  // namespace com
}  // namespace hyperpdf

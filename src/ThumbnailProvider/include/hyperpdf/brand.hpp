// Author: Anwarul Haque (@HYPERMX7) | Contact: 7739399007
//
// Single source of truth for the branding of the native Shell extensions.
// The values must stay in sync with src/App/BrandInfo.cs and Directory.Build.props.

#ifndef HYPERPDF_BRAND_HPP
#define HYPERPDF_BRAND_HPP

namespace hyperpdf {
namespace brand {

inline constexpr wchar_t kProductName[] = L"HyperPDF";
inline constexpr wchar_t kVersion[] = L"1.0.0";
inline constexpr wchar_t kCompany[] = L"Anwarul Haque (@HYPERMX7)";
inline constexpr wchar_t kAuthor[] = L"Anwarul Haque (@HYPERMX7)";
inline constexpr wchar_t kCopyright[] = L"\u00A9 2026 Anwarul Haque (@HYPERMX7). All rights reserved.";
inline constexpr wchar_t kDescription[] =
    L"HyperPDF is a fast, modern Windows PDF reader with native Explorer thumbnail and preview support.";

// The file association the Shell extensions are registered for.
inline constexpr wchar_t kPdfExtension[] = L".pdf";

}  // namespace brand
}  // namespace hyperpdf

#endif  // HYPERPDF_BRAND_HPP

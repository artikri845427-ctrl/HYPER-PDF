# Author: Anwarul Haque (@HYPERMX7) | Contact: 7739399007
#
# Builds the two native Shell extensions of HyperPDF:
#   * HyperPDFThumbnailProvider.dll - the Explorer thumbnail provider
#   * HyperPDFPreviewHandler.dll   - the Explorer preview handler
#
# The extensions are built for x64 and for x86. Each architecture has its own directory
# because the matching PDFium build (pdfium.dll) is loaded from the directory of the
# extension at runtime.

cmake_minimum_required(VERSION 3.20)

project(HyperPDFNative LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

if(NOT CMAKE_RUNTIME_OUTPUT_DIRECTORY)
    set(CMAKE_RUNTIME_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/bin")
endif()

# The branding lives in Directory.Build.props, CMake reads the same values.
file(READ "${CMAKE_CURRENT_SOURCE_DIR}/../../Directory.Build.props" HYPERPDF_BRAND_PROPS)
string(REGEX MATCH "HyperPdfVersion>([0-9.]+)<" _version_match "${HYPERPDF_BRAND_PROPS}")
set(HYPERPDF_VERSION "${CMAKE_MATCH_1}")
if(NOT HYPERPDF_VERSION)
    set(HYPERPDF_VERSION "1.0.0")
endif()

string(REGEX MATCH "HyperPdfCompany>([^<]+)<" _company_match "${HYPERPDF_BRAND_PROPS}")
set(HYPERPDF_COMPANY "${CMAKE_MATCH_1}")
if(NOT HYPERPDF_COMPANY)
    set(HYPERPDF_COMPANY "Anwarul Haque (@HYPERMX7)")
endif()

string(REGEX MATCH "HyperPdfCopyright>([^<]+)<" _copyright_match "${HYPERPDF_BRAND_PROPS}")
set(HYPERPDF_COPYRIGHT "${CMAKE_MATCH_1}")
if(NOT HYPERPDF_COPYRIGHT)
    set(HYPERPDF_COPYRIGHT "© 2026 Anwarul Haque (@HYPERMX7). All rights reserved.")
endif()

message(STATUS "HyperPDF version ${HYPERPDF_VERSION}")

string(REGEX MATCH "HyperPdfProductName>([^<]+)<" _product_match "${HYPERPDF_BRAND_PROPS}")
set(HYPERPDF_PRODUCT_NAME "${CMAKE_MATCH_1}")
if(NOT HYPERPDF_PRODUCT_NAME)
    set(HYPERPDF_PRODUCT_NAME "HyperPDF")
endif()

# The file version of the native libraries, for example 1,0,0,0.
string(REPLACE "." "," HYPERPDF_FILE_VERSION "${HYPERPDF_VERSION}")
if(NOT HYPERPDF_FILE_VERSION MATCHES "^[^,]+,[^,]+,[^,]+,")
    string(APPEND HYPERPDF_FILE_VERSION ",0")
endif()

# --------------------------------------------------------------------------------------------
# Shared settings
# --------------------------------------------------------------------------------------------
function(hyperpdf_configure_target target)
    target_include_directories(${target}
        PRIVATE
            "${CMAKE_CURRENT_SOURCE_DIR}/include"
            "${CMAKE_CURRENT_SOURCE_DIR}/src/thumbnail"
            "${CMAKE_CURRENT_SOURCE_DIR}/src/preview"
    )

    target_compile_definitions(${target} PRIVATE UNICODE _UNICODE WIN32_LEAN_AND_MEAN NOMINMAX STRICT)

    if(MSVC)
        target_compile_options(${target} PRIVATE /W4 /permissive- /EHsc /MP /utf-8)
        target_compile_definitions(${target} PRIVATE _CRT_SECURE_NO_WARNINGS)
    endif()
endfunction()

# Creates the version resource of a native library.
function(hyperpdf_configure_version_resource target description internal_name original_filename)
    set(HYPERPDF_DESCRIPTION "${description}")
    set(HYPERPDF_INTERNAL_NAME "${internal_name}")
    set(HYPERPDF_ORIGINAL_FILENAME "${original_filename}")
    set(HYPERPDF_PRODUCT "${HYPERPDF_PRODUCT_NAME}")

    configure_file(
        "${CMAKE_CURRENT_SOURCE_DIR}/resources/version.rc.in"
        "${CMAKE_CURRENT_BINARY_DIR}/${target}.rc"
        @ONLY
    )

    target_sources(${target} PRIVATE "${CMAKE_CURRENT_BINARY_DIR}/${target}.rc")
endfunction()

# --------------------------------------------------------------------------------------------
# HyperPDFThumbnailProvider.dll
# --------------------------------------------------------------------------------------------
add_library(HyperPDFThumbnailProvider SHARED
    src/thumbnail/dllmain.cpp
    src/thumbnail/thumbnail_provider.cpp
    src/thumbnail/HyperPDFThumbnailProvider.def
    src/common/com_util.cpp
    src/common/pdfium_loader.cpp
)

hyperpdf_configure_target(HyperPDFThumbnailProvider)
hyperpdf_configure_version_resource(HyperPDFThumbnailProvider
    "HyperPDF Thumbnail Provider"
    "HyperPDFThumbnailProvider.dll"
    "HyperPDFThumbnailProvider.dll"
)

set_target_properties(HyperPDFThumbnailProvider PROPERTIES
    OUTPUT_NAME "HyperPDFThumbnailProvider"
    PREFIX ""
)

target_link_libraries(HyperPDFThumbnailProvider PRIVATE ole32 uuid shlwapi)

# --------------------------------------------------------------------------------------------
# HyperPDFPreviewHandler.dll
# --------------------------------------------------------------------------------------------
add_library(HyperPDFPreviewHandler SHARED
    src/preview/dllmain.cpp
    src/preview/preview_handler.cpp
    src/preview/HyperPDFPreviewHandler.def
    src/common/com_util.cpp
    src/common/pdfium_loader.cpp
)

hyperpdf_configure_target(HyperPDFPreviewHandler)
hyperpdf_configure_version_resource(HyperPDFPreviewHandler
    "HyperPDF Preview Handler"
    "HyperPDFPreviewHandler.dll"
    "HyperPDFPreviewHandler.dll"
)

set_target_properties(HyperPDFPreviewHandler PROPERTIES
    OUTPUT_NAME "HyperPDFPreviewHandler"
    PREFIX ""
)

target_link_libraries(HyperPDFPreviewHandler PRIVATE ole32 uuid shlwapi gdi32)

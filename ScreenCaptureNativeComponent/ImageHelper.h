#pragma once
#include <winrt/Windows.Storage.h>
#include <winrt/Windows.Graphics.Imaging.h>
#include <Windows.Graphics.Imaging.h>
using namespace winrt::Windows::Foundation;
using namespace winrt::Windows::Graphics::Imaging;
using namespace winrt::Windows::Graphics::DirectX;
using namespace winrt::Windows::Storage;

class ImageHelper
{
public:
	static IAsyncAction SaveSoftwareBitmapToFileAsync(ABI::Windows::Graphics::Imaging::ISoftwareBitmap* pSoftwareBitmap, StorageFile const& outputFile);
};


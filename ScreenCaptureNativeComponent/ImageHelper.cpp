#include "pch.h"
#include "ImageHelper.h"


IAsyncAction ImageHelper::SaveSoftwareBitmapToFileAsync(ABI::Windows::Graphics::Imaging::ISoftwareBitmap* pSoftwareBitmap, StorageFile const& outputFile)
{
	auto stream = co_await outputFile.OpenAsync(FileAccessMode::ReadWrite);
	auto encoder = co_await BitmapEncoder::CreateAsync(BitmapEncoder::JpegEncoderId(), stream);
	
	SoftwareBitmap bitmap{ nullptr };
	winrt::attach_abi(bitmap, pSoftwareBitmap);
	encoder.SetSoftwareBitmap(bitmap);
	encoder.IsThumbnailGenerated(true);

	try
	{
		co_await encoder.FlushAsync();
	}
	catch (...)
	{
		switch (winrt::to_hresult().value)
		{
		case WINCODEC_ERR_UNSUPPORTEDOPERATION:
			// If the encoder does not support writing a thumbnail, then try again
			// but disable thumbnail generation.
			encoder.IsThumbnailGenerated( false);
			break;
		default:
			throw;
		}
	}

	if (encoder.IsThumbnailGenerated() == false)
	{
		co_await encoder.FlushAsync();
	}
	pSoftwareBitmap->Release();
	stream.Close();
}
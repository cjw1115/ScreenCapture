#pragma once
#include "ScreenCaptureService.g.h"
#include <concurrent_queue.h>

#include <winrt/Windows.Storage.h>
#include <winrt/Windows.Graphics.Capture.h>
#include <winrt/Windows.Graphics.Imaging.h>
#include <winrt/Windows.Media.Editing.h>
#include <winrt/Windows.UI.Xaml.Controls.h>
#include <winrt/Windows.Media.MediaProperties.h>
#include <winrt/Windows.Media.Transcoding.h>
#include <winrt/Windows.UI.Core.h>
#include <windows.graphics.imaging.h>

namespace winrt::ScreenCaptureNativeComponent::implementation
{
    struct ScreenCaptureService : ScreenCaptureServiceT<ScreenCaptureService>
    {
        ScreenCaptureService() = default;

		winrt::Windows::Foundation::IAsyncAction StartCaptureAsync();
        void StopCapture();
        void VerifyDuration();
		winrt::Windows::Foundation::IAsyncAction SetupMediaComposition();
		winrt::Windows::Foundation::IAsyncAction RenderCompositionToFile(Windows::Storage::StorageFile const& file, Windows::UI::Xaml::Controls::ProgressBar const& progressBar);
		winrt::Windows::Foundation::IAsyncAction WaitForImageRenderring();
	private:
		winrt::Windows::Foundation::IAsyncOperation<winrt::Windows::Storage::StorageFolder> _setupCpatureFolder();
		void _startCaptureInternal(winrt::Windows::Graphics::Capture::GraphicsCaptureItem item);
		winrt::Windows::Foundation::IAsyncAction _renderCaptuedImagesToFiles();
	private:
		const wchar_t* CAPTURE_FOLDER = L"CaptureFolder";
		Windows::Storage::StorageFolder _captureFolder{ nullptr };

		concurrency::concurrent_queue <ABI::Windows::Graphics::Imaging::ISoftwareBitmap *> _capturedImages;
		concurrency::concurrent_queue<Windows::Foundation::TimeSpan> _captuedRelatedTimes;
		winrt::Windows::Graphics::Capture::Direct3D11CaptureFramePool _framePool{ nullptr };
		winrt::Windows::Graphics::Capture::GraphicsCaptureSession _seesion{ nullptr };
		UINT _imageCounter = 0;
		winrt::Windows::Foundation::TimeSpan _startTime;

		winrt::Windows::Media::Editing::MediaComposition _mediaComposition;
    };
}
namespace winrt::ScreenCaptureNativeComponent::factory_implementation
{
    struct ScreenCaptureService : ScreenCaptureServiceT<ScreenCaptureService, implementation::ScreenCaptureService>
    {
    };
}

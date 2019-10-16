#pragma once

#include "CaptureHelper.g.h"
#include <winrt/Windows.Graphics.Capture.h>
#include <Microsoft.Graphics.Canvas.h>
#include <winrt/Windows.Media.Editing.h>
#include <NativeGraghic.h>
namespace winrt::ScreenCaptureComponent::implementation
{
	struct CaptureHelper : CaptureHelperT<CaptureHelper>
	{
		CaptureHelper();

		void StartCaptureInternal(winrt::Windows::Foundation::TimeSpan spam, winrt::Windows::Media::Editing::MediaComposition mediaComposition, winrt::Windows::Graphics::Capture::GraphicsCaptureItem const& item);
		void Stop();
	private:
		NativeGraghic _nativeGraghic;
		winrt::Windows::Foundation::TimeSpan _span;
		winrt::Windows::Media::Editing::MediaComposition _mediaComposition = nullptr;
		winrt::Windows::Graphics::Capture::GraphicsCaptureSession _seesion = nullptr;

		winrt::Windows::Graphics::Capture::Direct3D11CaptureFramePool _framePool = nullptr;
		winrt::com_ptr< ABI::Microsoft::Graphics::Canvas::ICanvasDeviceStatics> _canvasDeviceStatics;
		winrt::com_ptr< ABI::Microsoft::Graphics::Canvas::ICanvasDevice> _canvasDevice;
		winrt::com_ptr< ABI::Windows::Graphics::DirectX::Direct3D11::IDirect3DDevice> _device;
		winrt::com_ptr< ABI::Microsoft::Graphics::Canvas::ICanvasBitmapStatics> _canvasBitmapStatics;
		winrt::com_ptr< ABI::Microsoft::Graphics::Canvas::ICanvasRenderTargetFactory> _canvasRenderTargetFactory;

		void OnFrameArrived(winrt::Windows::Graphics::Capture::Direct3D11CaptureFramePool, Windows::Foundation::IInspectable);
		void ProcessFrame(winrt::Windows::Graphics::Imaging::SoftwareBitmap softwareBitmap);
	};
}
namespace winrt::ScreenCaptureComponent::factory_implementation
{
	struct CaptureHelper : CaptureHelperT<CaptureHelper, implementation::CaptureHelper>
	{
	};
}

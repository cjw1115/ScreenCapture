#include "pch.h"
#include "CaptureHelper.h"
#include "CaptureHelper.g.cpp"

#include "NativeGraghic.h"
#include <winrt/Windows.Graphics.Imaging.h>
#include <Microsoft.Graphics.Canvas.h>

using namespace winrt::Windows::Graphics::Capture;
using namespace winrt::Windows::Graphics;
using namespace winrt::Windows::Graphics::DirectX::Direct3D11;
using namespace ABI::Windows::Graphics::DirectX;

namespace winrt::ScreenCaptureComponent::implementation
{
	CaptureHelper::CaptureHelper()
	{
	}

	void CaptureHelper::Stop()
	{
		_seesion.Close();
		_framePool.Close();
	}

	winrt::com_ptr< ABI::Microsoft::Graphics::Canvas::ICanvasResourceCreator> canvasResourceCreator;

	void CaptureHelper::StartCaptureInternal(winrt::Windows::Foundation::TimeSpan span, winrt::Windows::Media::Editing::MediaComposition mediaComposition, winrt::Windows::Graphics::Capture::GraphicsCaptureItem const& item)
	{
		_span = span;
		_mediaComposition = mediaComposition;
		_nativeGraghic.GetCanvasBitmap(_canvasBitmapStatics.put());
		_nativeGraghic.GetCanvasDevice(_device.put());
		_nativeGraghic.GetCanvasRenderTargetFactory(_canvasRenderTargetFactory.put());

		auto device = _device.as<winrt::Windows::Graphics::DirectX::Direct3D11::IDirect3DDevice>();
		canvasResourceCreator = _device.as<ABI::Microsoft::Graphics::Canvas::ICanvasResourceCreator>();

		auto size = item.Size();
		_framePool = Direct3D11CaptureFramePool::Create(device, winrt::Windows::Graphics::DirectX::DirectXPixelFormat::B8G8R8A8UIntNormalized, 2, size);
		_framePool.FrameArrived({ this,&CaptureHelper::OnFrameArrived });
		
		_seesion = _framePool.CreateCaptureSession(item);
		_seesion.StartCapture();
	}
	
	void  CaptureHelper::OnFrameArrived(winrt::Windows::Graphics::Capture::Direct3D11CaptureFramePool, Windows::Foundation::IInspectable)
	{
		auto frame = _framePool.TryGetNextFrame();
		auto softwareBitmap= winrt::Windows::Graphics::Imaging::SoftwareBitmap::CreateCopyFromSurfaceAsync(frame.Surface()).get();
		ProcessFrame(softwareBitmap);
		softwareBitmap.Close();
		frame.Close();
	}

#ifdef __cplusplus
	struct __declspec(uuid("5b0d3235-4dba-4d44-865e-8f1d0e4fd04d")) IMemoryBufferByteAccess :
		public IUnknown
	{
		// An IMemoryBuffer object is created by a client, and the buffer is provided by IBufferByteAccess::GetBuffer.
		// When IMemoryBufferReference::Close() is called, the code that is using this buffer should set "value" to nullptr,
		// effectively "forgetting" the pointer ot the buffer.
		STDMETHOD(GetBuffer)(_Outptr_result_buffer_(*capacity) BYTE** value, _Out_ UINT32* capacity) = 0;
	};
#endif

	void _getPixelBuffer(winrt::Windows::Graphics::Imaging::SoftwareBitmap softwareBitmap, UINT32* count, byte** buffer)
	{
		auto bitmapBuffer = softwareBitmap.LockBuffer(winrt::Windows::Graphics::Imaging::BitmapBufferAccessMode::Read);
		auto reference = bitmapBuffer.CreateReference();
		auto byteAccess = reference.as<IMemoryBufferByteAccess>();
		byteAccess->GetBuffer(buffer, count);
		reference.Close();
		bitmapBuffer.Close();
	}

	ABI::Windows::Foundation::Rect rect;
	

	void CaptureHelper::ProcessFrame(winrt::Windows::Graphics::Imaging::SoftwareBitmap softwareBitmap)
	{
		//auto canvasResourceCreator = 

		byte* buffer = nullptr;
		UINT32 count = 0;
		_getPixelBuffer(softwareBitmap, &count, &buffer);
		//auto bitmapBuffer = new byte[count];
		//CopyMemory(bitmapBuffer, buffer, count);

		ABI::Microsoft::Graphics::Canvas::ICanvasBitmap* canvasBitmap;
		_canvasBitmapStatics->CreateFromBytes(canvasResourceCreator.get(), count, buffer, softwareBitmap.PixelWidth(), softwareBitmap.PixelHeight(),DirectXPixelFormat_B8G8R8A8UIntNormalized, &canvasBitmap);
		
		ABI::Microsoft::Graphics::Canvas::ICanvasRenderTarget* canvasRenderTarget;
		_canvasRenderTargetFactory->CreateWithWidthAndHeightAndDpi(canvasResourceCreator.get(), (float)softwareBitmap.PixelWidth(), (float)softwareBitmap.PixelHeight(),96, &canvasRenderTarget);

		ABI::Microsoft::Graphics::Canvas::ICanvasDrawingSession* canvasDrawingSession;
		canvasRenderTarget->CreateDrawingSession(&canvasDrawingSession);
		canvasDrawingSession->Clear(ABI::Windows::UI::Color());
		
		rect.X = 0;
		rect.Y = 0;
		rect.Width = (float)softwareBitmap.PixelWidth();
		rect.Height= (float)softwareBitmap.PixelHeight();
		canvasDrawingSession->DrawImageToRect(canvasBitmap, rect);
		//delete[] bitmapBuffer;
		canvasBitmap->Release();
		canvasDrawingSession->Release();

		winrt::com_ptr< ABI::Windows::Graphics::DirectX::Direct3D11::IDirect3DSurface> surface;
		canvasRenderTarget->QueryInterface(ABI::Windows::Graphics::DirectX::Direct3D11::IID_IDirect3DSurface, (void**)surface.put());
		canvasRenderTarget->Release();
		auto winrtSruface = surface.as<winrt::Windows::Graphics::DirectX::Direct3D11::IDirect3DSurface>();
		auto clip = winrt::Windows::Media::Editing::MediaClip::CreateFromSurface(winrtSruface, _span);
		_mediaComposition.Clips().Append(clip);
	}
}

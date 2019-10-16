#include "pch.h"
#include "CaptureHelper.h"
#include "CaptureHelper.g.cpp"

#include "NativeGraghic.h"
#include <winrt/Windows.Graphics.Imaging.h>
#include <Microsoft.Graphics.Canvas.h>

using namespace winrt::Windows::Graphics::Capture;
using namespace winrt::Windows::Graphics;
using namespace winrt::Windows::Graphics::DirectX;
using namespace winrt::Windows::Graphics::DirectX::Direct3D11;

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

	void CaptureHelper::StartCaptureInternal(winrt::Windows::Foundation::TimeSpan spam, winrt::Windows::Media::Editing::MediaComposition mediaComposition, winrt::Windows::Graphics::Capture::GraphicsCaptureItem const& item)
	{
		_span = spam;
		_mediaComposition = mediaComposition;
		_nativeGraghic.GetCanvasBitmap(_canvasBitmapStatics.put());
		_nativeGraghic.GetCanvasDevice(_device.put());
		_nativeGraghic.GetCanvasRenderTargetFactory(_canvasRenderTargetFactory.put());

		auto re = _device.as<winrt::Windows::Graphics::DirectX::Direct3D11::IDirect3DDevice>();
		auto size = item.Size();
		_framePool = Direct3D11CaptureFramePool::Create(
			re, // D3D device
			DirectXPixelFormat::B8G8R8A8UIntNormalized, // Pixel format
			2, // Number of frames
			size); // Size of the buffers

		_framePool.FrameArrived({ this,&CaptureHelper::OnFrameArrived });
		//_framePool.FrameArrived += async(s, a) = >
		//{
		//	// The FrameArrived event is raised for every frame on the thread
		//	// that created the Direct3D11CaptureFramePool. This means we
		//	// don't have to do a null-check here, as we know we're the only
		//	// one dequeueing frames in our application.  

		//	// NOTE: Disposing the frame retires it and returns  
		//	// the buffer to the pool.

		//	using (var frame = _framePool.TryGetNextFrame())
		//	{
		//		var softwareBitmap = await SoftwareBitmap.CreateCopyFromSurfaceAsync(frame.Surface);
		//		ProcessFrame(softwareBitmap);
		//	}
		//};

		//_item.Closed += (s, a) = >
		//{
		//	StopCapture();
		//};

		_seesion = _framePool.CreateCaptureSession(item);
		_seesion.StartCapture();
		//_lastTime = DateTime.Now;
	}
	
	void  CaptureHelper::OnFrameArrived(winrt::Windows::Graphics::Capture::Direct3D11CaptureFramePool, Windows::Foundation::IInspectable)
	{
		auto frame = _framePool.TryGetNextFrame();
		auto softwareBitmap= winrt::Windows::Graphics::Imaging::SoftwareBitmap::CreateCopyFromSurfaceAsync(frame.Surface()).get();
		
		ProcessFrame(softwareBitmap);
	}

	/*public unsafe System.Span<byte> _getPixelBuffer(SoftwareBitmap softwareBitmap)
	{
		uint capacity;
		using (BitmapBuffer buffer = softwareBitmap.LockBuffer(BitmapBufferAccessMode.Write))
		{
			using (var reference = buffer.CreateReference())
			{
				byte* dataInBytes;

				((IMemoryBufferByteAccess)reference).GetBuffer(out dataInBytes, out capacity);
				return new Span<byte>(dataInBytes, (int)capacity);
			}
		}
	}*/

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
		//auto guid = winrt::guid(0x5B0D3235, 0x4DBA, 0x4D44, { 0x86,0x5E,0x8F,0x1D,0x0E,0x4F,0xD0,0x4D });
		auto byteAccess = reference.as<IMemoryBufferByteAccess>();
		byteAccess->GetBuffer(buffer, count);
	}

	void CaptureHelper::ProcessFrame(winrt::Windows::Graphics::Imaging::SoftwareBitmap softwareBitmap)
	{
		auto sharedDevice = _device.as<ABI::Microsoft::Graphics::Canvas::ICanvasResourceCreator>();

		byte* buffer = nullptr;
		UINT32 count = 0;
		_getPixelBuffer(softwareBitmap, &count, &buffer);
		winrt::com_ptr<ABI::Microsoft::Graphics::Canvas::ICanvasBitmap> canvasBitmap;
		_canvasBitmapStatics->CreateFromBytes(sharedDevice.get(), count, buffer, softwareBitmap.PixelWidth(), softwareBitmap.PixelHeight(),
			ABI::Windows::Graphics::DirectX::DirectXPixelFormat::DirectXPixelFormat_B8G8R8A8UIntNormalized, canvasBitmap.put());
		
		winrt::com_ptr< ABI::Microsoft::Graphics::Canvas::ICanvasRenderTarget> canvasRenderTarget;
		_canvasRenderTargetFactory->CreateWithWidthAndHeightAndDpi(sharedDevice.get(), softwareBitmap.PixelWidth(), softwareBitmap.PixelHeight(),96, canvasRenderTarget.put());

		winrt::com_ptr< ABI::Microsoft::Graphics::Canvas::ICanvasDrawingSession> canvasDrawingSession;

		canvasRenderTarget->CreateDrawingSession(canvasDrawingSession.put());
		canvasDrawingSession->Clear(ABI::Windows::UI::Color());
		ABI::Windows::Foundation::Rect r;
		r.X = 0;
		r.Y = 0;
		r.Width = softwareBitmap.PixelWidth();
		r.Height= softwareBitmap.PixelHeight();
		canvasDrawingSession->DrawImageToRect(canvasBitmap.get(), r);

		auto clip = winrt::Windows::Media::Editing::MediaClip::CreateFromSurface(canvasRenderTarget.as<winrt::Windows::Graphics::DirectX::Direct3D11::IDirect3DSurface>(), _span);
		_mediaComposition.Clips().Append(clip);
	}
}

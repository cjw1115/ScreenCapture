#pragma once
#include <Microsoft.Graphics.Canvas.h>
#include <winrt/Windows.Graphics.DirectX.Direct3D11.h>

class NativeGraghic
{
public:
	NativeGraghic() = default;
	static void GetCanvasDevice(ABI::Windows::Graphics::DirectX::Direct3D11::IDirect3DDevice** canvasDevice);
	void GetCanvasBitmap(ABI::Microsoft::Graphics::Canvas::ICanvasBitmapStatics ** canvasBitmapStatics);
	void GetCanvasRenderTargetFactory(ABI::Microsoft::Graphics::Canvas::ICanvasRenderTargetFactory** factory);
};


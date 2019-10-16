#include "pch.h"
#include "NativeGraghic.h"

#include <roapi.h>

void NativeGraghic::GetCanvasDevice(ABI::Windows::Graphics::DirectX::Direct3D11::IDirect3DDevice** device)
{
	HSTRING hstring;
	WindowsCreateString(L"Microsoft.Graphics.Canvas.CanvasDevice", sizeof(L"Microsoft.Graphics.Canvas.CanvasDevice")/sizeof(wchar_t)-1, &hstring);
	HRESULT hr = RoActivateInstance(hstring, (IInspectable**)device);
	
}

void NativeGraghic::GetCanvasBitmap(ABI::Microsoft::Graphics::Canvas::ICanvasBitmapStatics** canvasBitmapStatics)
{
	HSTRING hstring;
	WindowsCreateString(L"Microsoft.Graphics.Canvas.CanvasBitmap", sizeof(L"Microsoft.Graphics.Canvas.CanvasBitmap") / sizeof(wchar_t) - 1, &hstring);
	//HRESULT hr = RoActivateInstance(hstring, (IInspectable**)canvasBitmapStatics);
	HRESULT hr = RoGetActivationFactory(hstring, ABI::Microsoft::Graphics::Canvas::IID_ICanvasBitmapStatics, (void**)canvasBitmapStatics);
}
void NativeGraghic::GetCanvasRenderTargetFactory(ABI::Microsoft::Graphics::Canvas::ICanvasRenderTargetFactory** factory)
{
	HSTRING hstring;
	WindowsCreateString(L"Microsoft.Graphics.Canvas.CanvasRenderTarget", sizeof(L"Microsoft.Graphics.Canvas.CanvasRenderTarget") / sizeof(wchar_t) - 1, &hstring);
	//HRESULT hr = RoActivateInstance(hstring, (IInspectable**)factory);
	HRESULT hr  = RoGetActivationFactory(hstring, ABI::Microsoft::Graphics::Canvas::IID_ICanvasRenderTargetFactory, (void**)factory);
}
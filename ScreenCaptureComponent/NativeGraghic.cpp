#include "pch.h"
#include "NativeGraghic.h"

#include <string>
#include <roapi.h>

void NativeGraghic::GetCanvasDevice(ABI::Windows::Graphics::DirectX::Direct3D11::IDirect3DDevice** device)
{
	HSTRING hstring;
	std::wstring name = L"Microsoft.Graphics.Canvas.CanvasDevice";
	WindowsCreateString(name.c_str(), name.length(), &hstring);
	HRESULT hr = RoActivateInstance(hstring, (IInspectable**)device);
	assert(hr == S_OK);
	WindowsDeleteString(hstring);
}

void NativeGraghic::GetCanvasBitmap(ABI::Microsoft::Graphics::Canvas::ICanvasBitmapStatics** canvasBitmapStatics)
{
	HSTRING hstring;
	std::wstring name = L"Microsoft.Graphics.Canvas.CanvasBitmap";
	WindowsCreateString(name.c_str(), name.length(), &hstring);
	HRESULT hr = RoGetActivationFactory(hstring, ABI::Microsoft::Graphics::Canvas::IID_ICanvasBitmapStatics, (void**)canvasBitmapStatics);
	assert(hr == S_OK);
	WindowsDeleteString(hstring);
}
void NativeGraghic::GetCanvasRenderTargetFactory(ABI::Microsoft::Graphics::Canvas::ICanvasRenderTargetFactory** factory)
{
	HSTRING hstring;
	std::wstring name = L"Microsoft.Graphics.Canvas.CanvasRenderTarget";
	WindowsCreateString(name.c_str(), name.length(), &hstring);
	HRESULT hr  = RoGetActivationFactory(hstring, ABI::Microsoft::Graphics::Canvas::IID_ICanvasRenderTargetFactory, (void**)factory);
	assert(hr == S_OK);
	WindowsDeleteString(hstring);
}
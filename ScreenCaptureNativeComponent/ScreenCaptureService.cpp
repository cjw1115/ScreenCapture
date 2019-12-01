#include "pch.h"
#include "ScreenCaptureService.h"
#include "ScreenCaptureService.g.cpp"

#include <thread>
#include <stdio.h>
#include <chrono> 
#include <sstream>

#include "NativeGraghic.h"
#include "ImageHelper.h"
using namespace std::chrono_literals;

using namespace winrt;
using namespace winrt::Windows::Foundation;
using namespace winrt::Windows::Graphics::Capture;
using namespace winrt::Windows::Graphics::Imaging;
using namespace winrt::Windows::Graphics::DirectX;
using namespace winrt::Windows::Storage;
using namespace winrt::Windows::Media::Editing;
using namespace winrt::Windows::Media::MediaProperties;
using namespace winrt::Windows::UI::Xaml::Controls;
using namespace winrt::Windows::Media::Transcoding;
using namespace winrt::Windows::UI::Core;

namespace winrt::ScreenCaptureNativeComponent::implementation
{
	IAsyncOperation<StorageFolder> ScreenCaptureService::_setupCpatureFolder()
	{
		StorageFolder storageFolder{ nullptr };
		auto current = Windows::Storage::ApplicationData::Current();
		try
		{	
			storageFolder = co_await current.LocalCacheFolder().GetFolderAsync(CAPTURE_FOLDER);
			if (storageFolder != nullptr)
			{
				co_await storageFolder.DeleteAsync(StorageDeleteOption::PermanentDelete);
			}
		}
		catch (...)
		{

		}
		storageFolder = co_await current.LocalCacheFolder().CreateFolderAsync(CAPTURE_FOLDER);
		return storageFolder;
	}

    LARGE_INTEGER _last;
    LARGE_INTEGER _now;
	void ScreenCaptureService::_onFrameArraved(Direct3D11CaptureFramePool const& framePool)
	{
		auto frame = framePool.TryGetNextFrame();
        if (frame == nullptr)
        {
            return;
        }

        /*QueryPerformanceCounter(&_now);
        auto delta = _now.QuadPart - _last.QuadPart;
        if (delta <= 0.04)
        {
            frame.Close();
            std::this_thread::sleep_for(std::chrono::milliseconds((int)((0.04 - delta) * 1000)));
            return;
        }
        _last = _now;*/

		auto bitmap = SoftwareBitmap::CreateCopyFromSurfaceAsync(frame.Surface(), BitmapAlphaMode::Premultiplied).get();
		ABI::Windows::Graphics::Imaging::ISoftwareBitmap* pBitmap = nullptr;
		bitmap.as<ABI::Windows::Graphics::Imaging::ISoftwareBitmap>().copy_to(&pBitmap);
		pBitmap->AddRef();
		_capturedImages.push(pBitmap);
		_captuedRelatedTimes.push(frame.SystemRelativeTime());
		frame.Close();
	}

	void ScreenCaptureService::_startCaptureInternal(GraphicsCaptureItem item)
	{
		_capturedImages.clear();
		_captuedRelatedTimes.clear();
        std::thread([&, item]()
            {
                winrt::com_ptr<ABI::Windows::Graphics::DirectX::Direct3D11::IDirect3DDevice> canvasDevice;
                NativeGraghic::GetCanvasDevice(canvasDevice.put());
                Direct3D11::IDirect3DDevice device{ nullptr };
                winrt::copy_from_abi(device, canvasDevice.get());

                _framePool = Direct3D11CaptureFramePool::Create(device, DirectXPixelFormat::B8G8R8A8UIntNormalized, 1, item.Size());
                //_framePool.FrameArrived({ this,&ScreenCaptureService::OnFrameArraved });

                _seesion = _framePool.CreateCaptureSession(item);
                _capturing = true;
                _seesion.StartCapture();

                LARGE_INTEGER counter, frequency;
                QueryPerformanceCounter(&counter);
                QueryPerformanceFrequency(&frequency);
                auto time = counter.QuadPart / frequency.QuadPart;
                _startTime = Windows::Foundation::TimeSpan(std::chrono::seconds(time));

                QueryPerformanceCounter(&_last);
                while (_capturing)
                {
                    _onFrameArraved(_framePool);
                    std::this_thread::sleep_for(std::chrono::milliseconds(100));
                }
            }).detach();
	}

    IAsyncAction ScreenCaptureService::StartCaptureAsync()
    {
		GraphicsCapturePicker picker;
        auto item = co_await picker.PickSingleItemAsync();
		if (item != nullptr)
		{
			_captureFolder = co_await _setupCpatureFolder();
            _startCaptureInternal(item);
			std::thread([this,&item]()
				{
					_renderCaptuedImagesToFiles();
				}).detach();
		}
    }
    void ScreenCaptureService::StopCapture()
    {
        _capturing = false;
		_seesion.Close();
		_framePool.Close();
    }
    void ScreenCaptureService::VerifyDuration()
    {
        /*throw hresult_not_implemented();*/
    }
    IAsyncAction ScreenCaptureService::SetupMediaComposition(MediaComposition mediaComposition)
    {
		_mediaComposition = mediaComposition;

		auto _capturedImageFiles = co_await _captureFolder.GetFilesAsync();
		auto lastTime = _startTime;
	
		for (size_t i = 0; i < _capturedImageFiles.Size(); i++)
		{
			auto imageFile = _capturedImageFiles.GetAt(i);
			TimeSpan relatedTime;
			if (_captuedRelatedTimes.try_pop(relatedTime))
			{
				auto duration =relatedTime - lastTime;
				lastTime = relatedTime;
				auto clip = co_await MediaClip::CreateFromImageFileAsync(imageFile, duration);
				_mediaComposition.Clips().Append(clip);
			}
		}
    }
	IAsyncAction ScreenCaptureService::RenderCompositionToFile(StorageFile const& file, ProgressBar const& progressBar)
    {
		co_return;
    }

	IAsyncAction ScreenCaptureService::WaitForImageRenderring()
    {
		while (!_capturedImages.empty())
		{
			co_await 20ms;
		}
    }

	IAsyncAction ScreenCaptureService::_renderCaptuedImagesToFiles()
	{
		while (true)
		{
			bool re = false;
			do
			{
				ABI::Windows::Graphics::Imaging::ISoftwareBitmap* pBitmap = nullptr;
				re = _capturedImages.try_pop(pBitmap);
				if (re)
				{
					_imageCounter++;

					std::wstringstream ss;
					ss << L"Screenshot_";
					ss << _imageCounter;
					ss<< L".jpeg";

					//std::wstring name(L"Screenshot_" + ++_imageCounter + L".jpeg");

					auto file = co_await _captureFolder.CreateFileAsync(ss.str());
					await ImageHelper::SaveSoftwareBitmapToFileAsync(pBitmap, file);
				}
			} while (re);
			co_await 5ms;
		}
	}
}

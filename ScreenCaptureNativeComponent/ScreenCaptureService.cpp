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

	void ScreenCaptureService::_startCaptureInternal(GraphicsCaptureItem item)
	{
		_capturedImages.clear();
		_captuedRelatedTimes.clear();
		winrt::com_ptr<ABI::Windows::Graphics::DirectX::Direct3D11::IDirect3DDevice> canvasDevice;
		NativeGraghic::GetCanvasDevice(canvasDevice.put());
		Direct3D11::IDirect3DDevice device{ nullptr };
		winrt::copy_from_abi(device, canvasDevice.get());
		_framePool = Direct3D11CaptureFramePool::Create(device, DirectXPixelFormat::B8G8R8A8UIntNormalized, 2, item.Size());

		_framePool.FrameArrived([this](Direct3D11CaptureFramePool const&, IInspectable const&)->IAsyncAction
			{
				auto frame = _framePool.TryGetNextFrame();
				auto bitmap = co_await SoftwareBitmap::CreateCopyFromSurfaceAsync(frame.Surface(), BitmapAlphaMode::Premultiplied);
				ABI::Windows::Graphics::Imaging::ISoftwareBitmap* pBitmap;
				bitmap.as<ABI::Windows::Graphics::Imaging::ISoftwareBitmap>().copy_to(&pBitmap);
				pBitmap->AddRef();
				_capturedImages.push(pBitmap);

				_captuedRelatedTimes.push(frame.SystemRelativeTime());
				frame.Close();
			});
		_seesion = _framePool.CreateCaptureSession(item);
		_seesion.StartCapture();
		
		LARGE_INTEGER counter,frequency;
		QueryPerformanceCounter(&counter);
		QueryPerformanceFrequency(&frequency);
		auto time = counter.QuadPart / frequency.QuadPart;
		_startTime = Windows::Foundation::TimeSpan(std::chrono::seconds(time));
	}

    IAsyncAction ScreenCaptureService::StartCaptureAsync()
    {
		GraphicsCapturePicker picker;
		auto item = co_await picker.PickSingleItemAsync();
		if (item != nullptr)
		{
			_captureFolder = co_await _setupCpatureFolder();
			_startCaptureInternal(item);
			std::thread([this]() 
				{
					_renderCaptuedImagesToFiles();
				}).detach();
		}
    }
    void ScreenCaptureService::StopCapture()
    {
		_seesion.Close();
		_framePool.Close();
    }
    void ScreenCaptureService::VerifyDuration()
    {
        /*throw hresult_not_implemented();*/
    }
    IAsyncAction ScreenCaptureService::SetupMediaComposition()
    {
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
		if (file != nullptr)
		{
			// Call RenderToFileAsync
			auto mp4Profile = MediaEncodingProfile::CreateMp4(VideoEncodingQuality::HD1080p);
			auto saveOperation = _mediaComposition.RenderToFileAsync(file, MediaTrimmingPreference::Precise, mp4Profile);
			saveOperation.Progress([progressBar](IAsyncOperationWithProgress<TranscodeFailureReason,double> const& info, double const& progress)
				{

					progressBar.Dispatcher().RunAsync(CoreDispatcherPriority::Normal, [progressBar,progress]()
						{
							std::wstring msg = L"Progress:" + (int)progress;
							OutputDebugString(msg.c_str());
						});
				});
			saveOperation.Completed([&progressBar](IAsyncOperationWithProgress<TranscodeFailureReason, double> const& info, winrt::Windows::Foundation::AsyncStatus const& status)
				{	
					if (info.GetResults() != TranscodeFailureReason::None || status != winrt::Windows::Foundation::AsyncStatus::Completed)
					{
						throw L"Saving was unsuccessful";
					}
				});
			while (saveOperation.Status() == winrt::Windows::Foundation::AsyncStatus::Started)
			{
				std::this_thread::sleep_for(std::chrono::microseconds(20));
			}
		}
		else
		{
			throw L"User cancelled the file selection";
		}
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
#include "pch.h"
#include "Capture.h"
#include "Capture.g.cpp"
#include <string>
#include <sstream>
#include <queue>

#include <winrt/Windows.Storage.h>
#include <winrt/Windows.Storage.Streams.h>
#include <winrt/Windows.UI.Core.h>
#include <winrt/Windows.UI.Xaml.Shapes.h>

namespace winrt::AudioCaptureNativeComponent::implementation
{
	Capture::Capture(AudioCaptureNativeComponent::RoleMode role)
	{
		_roleMode = role;
		Initialize();
	}
	
	AudioCaptureNativeComponent::RoleMode Capture::Role()
	{
		return _roleMode;
	}

	void Capture::Initialize()
	{
		if (Role() == RoleMode::Loopback)
		{
			_captureHelper.InitlizeDevice(true);
		}
		else
		{
			_captureHelper.InitlizeDevice(false);
		}
	}
	event_token Capture::DataReceived(EventHandler<long long> const& handler)
	{
		return _dataReceivedEvent.add(handler);
	}

	void Capture::DataReceived(winrt::event_token const& token) noexcept
	{
		_dataReceivedEvent.remove(token);
	}

	event_token Capture::CaptureStop(EventHandler<int> const& handler)
	{
		return _captureStopEvent.add(handler);
	}

	void Capture::CaptureStop(winrt::event_token const& token) noexcept
	{
		_captureStopEvent.remove(token);
	}

	IAsyncAction Capture::Start(winrt::Windows::Storage::StorageFile file)
	{
		co_await _waveWriter.Open(file);
		_captureHelper.Start([this](WAVEFORMATEX * wavFormat)
			{
				this->Reset();
				_waveWriter.WriteHeader(wavFormat);
			},
			[this](byte * buffer, UINT bufferSize)
			{
				_currentDuration += 10;
				_waveWriter.WriteData(buffer, bufferSize);
				_dataReceivedEvent(*this, _currentDuration);
			}, [this]()
			{
				_waveWriter.Save();
				_captureStopEvent(*this, _waveWriter.Duration());
			});
	}

	void Capture::Resume()
	{
		_captureHelper.Resume();
	}

	void Capture::Pause()
	{
		_captureHelper.Pause();
	}
	void Capture::Stop()
	{
		_captureHelper.Stop();
	}
	void Capture::Reset()
	{
		_currentDuration = 0;
	}

	IAsyncAction Capture::SetPlaybackFile(winrt::Windows::Storage::StorageFile targetFile)
	{
		this->Reset();

		co_await _waveReader.Open(targetFile);

		WAVEFORMATEX format;
		DWORD pcmSize;
		_waveReader.ReadWaveformat(&format, &pcmSize);		
	}

	void Capture::SetPlaybackPosition(UINT64 position)
	{
		auto sampleSize = 480 * 2 * 4;
		_waveReader.ReadBuffer(sampleSize, [this](byte* buffer,DWORD size)
			{
			});
		
	}
}

#pragma once
#pragma once
#include "Capture.g.h"
#include "Services/CaptureHelper.h"

#include <winrt/Windows.Foundation.Collections.h>
#include <winrt/Windows.Media.Audio.h>
#include <winrt/Windows.Media.h>
#include <winrt/Windows.Media.MediaProperties.h>
#include <winrt/Windows.UI.Xaml.Controls.h>

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

#include "Utilities/WaveHeader.h"

using namespace winrt::Windows::Foundation::Collections;
using namespace winrt::Windows::Media::Audio;
using namespace winrt::Windows::Media::MediaProperties;

namespace winrt::AudioCaptureNativeComponent::implementation
{
    struct Capture : CaptureT<Capture>
    {
	public: //Property
		winrt::event_token Capture::DataReceived(EventHandler<long long> const& handler);
		void Capture::DataReceived(winrt::event_token const& token)noexcept;

		winrt::event_token Capture::CaptureStop(EventHandler<int> const& handler);
		void Capture::CaptureStop(winrt::event_token const& token)noexcept;

		AudioCaptureNativeComponent::RoleMode Role();
		void Role(AudioCaptureNativeComponent::RoleMode value);
	public: //Methods
        Capture();
        Capture(AudioCaptureNativeComponent::RoleMode role);
        void Initialize();
        IAsyncAction Start(winrt::Windows::Storage::StorageFile targetFile);
        void Resume();
        void Pause();
        void Stop();
		void Reset();

		IAsyncAction SetPlaybackFile(winrt::Windows::Storage::StorageFile targetFile);
		void SetPlaybackPosition(UINT64 position);
    private:
        event<EventHandler<long long>> _dataReceivedEvent;
        event<EventHandler<int>> _captureStopEvent;
        CaptureHelper _captureHelper;
		long long _currentDuration;

        AudioCaptureNativeComponent::RoleMode _roleMode = RoleMode::Capture;

        WaveWriter _waveWriter;
		WaveReader _waveReader;
    };
}
namespace winrt::AudioCaptureNativeComponent::factory_implementation
{
    struct Capture : CaptureT<Capture, implementation::Capture>
    {
    };
}

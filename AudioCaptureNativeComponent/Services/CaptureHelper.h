#pragma once
#include <audioclient.h>
#include <atlbase.h>
#include <mmdeviceapi.h>
#include <thread>
#include <functional>

#define REFTIMES_PER_SEC  5000000

class CaptureHelper
{
public:
	CaptureHelper();
	~CaptureHelper();
	void InitlizeDevice(bool isLoopback);
	void Start(const std::function<void(WAVEFORMATEX*)>& notifyStart, const std::function<void(byte*, UINT)>& notifyProcessing, const std::function<void()>& notifyStop);
	void Resume();
	void Pause();
	void Stop();
private:
	CComPtr<IAudioClient> audioClient;
	CComPtr<IMMDeviceEnumerator> mmDeviceEnumerator;
	CComPtr<IMMDevice> mmDevice;
	CComPtr<IAudioCaptureClient> captureClient;
	WAVEFORMATEX* wavFormat;

	std::thread captureThread;

	bool captureIndicator = false;
	bool isPause = false;
};

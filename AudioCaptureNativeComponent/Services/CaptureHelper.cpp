#include "pch.h"
#include "CaptureHelper.h"

CaptureHelper::CaptureHelper()
{
}

CaptureHelper::~CaptureHelper()
{
}

void CaptureHelper::InitlizeDevice(bool isLoopback)
{
	EDataFlow dataFlow = EDataFlow::eCapture;
	DWORD initStreamFlag = 0;
	if (isLoopback)
	{
		dataFlow = EDataFlow::eRender;
		initStreamFlag = AUDCLNT_STREAMFLAGS_LOOPBACK;
	}
	HRESULT hr = S_OK;

	if (mmDeviceEnumerator != nullptr)
	{
		mmDeviceEnumerator.Release();
	}
	if (mmDevice != nullptr)
	{
		mmDevice.Release();
	}
	if (audioClient != nullptr)
	{
		audioClient.Release();
	}
	if (captureClient != nullptr)
	{
		captureClient.Release();
	}

	hr = CoCreateInstance(__uuidof(MMDeviceEnumerator), NULL, CLSCTX_ALL, _uuidof(IMMDeviceEnumerator), (void**)& mmDeviceEnumerator);
	hr = mmDeviceEnumerator->GetDefaultAudioEndpoint(dataFlow, ERole::eConsole, &mmDevice);
	hr = mmDevice->Activate(__uuidof(IAudioClient), CLSCTX_ALL, nullptr, (void**)& audioClient);
	hr = audioClient->GetMixFormat(&wavFormat);
	hr = audioClient->Initialize(AUDCLNT_SHAREMODE::AUDCLNT_SHAREMODE_SHARED, initStreamFlag, REFTIMES_PER_SEC, 0, wavFormat, NULL);

	UINT actualBufferSize;
	audioClient->GetBufferSize(&actualBufferSize);
	hr = audioClient->GetService(__uuidof(IAudioCaptureClient), (void**)& captureClient);
}

void CaptureHelper::Start(const std::function<void(WAVEFORMATEX*)>& notifyStart, const std::function<void(byte*, UINT)>& notifyProcessing, const std::function<void()>& notifyStop)
{
	if (audioClient == nullptr)
		return;
	if (!captureIndicator)
	{
		captureIndicator = true;
		isPause = false;

		captureThread = std::thread([this, notifyStart, notifyProcessing, notifyStop]()
			{
				notifyStart(wavFormat);

				byte* buffer;
				UINT frameCount;
				DWORD flag;

				HRESULT hr = audioClient->Start();
				UINT packetLength;
				int count = 0;
				while (captureIndicator)
				{
					if (isPause)
					{
						Sleep(41);
						continue;
					}
					Sleep(41);
					hr = captureClient->GetNextPacketSize(&packetLength);
					while (packetLength != 0 && captureIndicator)
					{
						count++;
						hr = captureClient->GetBuffer(&buffer, &frameCount, &flag, NULL, NULL);
						if (flag == AUDCLNT_BUFFERFLAGS_SILENT)
						{
							//Do not record;
						}
						else
						{
							notifyProcessing((byte*)buffer, frameCount * wavFormat->nBlockAlign);
						}

						captureClient->ReleaseBuffer(frameCount);
						packetLength = 0;
						hr = captureClient->GetNextPacketSize(&packetLength);
					}
				}
				hr = audioClient->Stop();

				hr = captureClient->GetNextPacketSize(&packetLength);
				while (packetLength != 0)
				{
					hr = captureClient->GetBuffer(&buffer, &frameCount, &flag, NULL, NULL);
					if (flag == AUDCLNT_BUFFERFLAGS_SILENT)
					{
						//Do not record;
					}
					else
					{
						notifyProcessing((byte*)buffer, frameCount * wavFormat->nBlockAlign);
					}

					captureClient->ReleaseBuffer(frameCount);
					packetLength = 0;
					hr = captureClient->GetNextPacketSize(&packetLength);
				}
				hr = audioClient->Reset();
				notifyStop();
			});
		captureThread.detach();
	}

}

void CaptureHelper::Resume()
{
	if (audioClient != nullptr)
	{
		audioClient->Start();
		isPause = false;
	}
}

void CaptureHelper::Pause()
{
	if (audioClient != nullptr)
	{
		audioClient->Stop();
		isPause = true;
	}
}

void CaptureHelper::Stop()
{
	captureIndicator = false;
	isPause = false;

}
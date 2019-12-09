#include "pch.h"
#include "MediaEncoder.h"
#include "MediaEncoder.g.cpp"

// Format constants
const UINT32 VIDEO_FPS = 30;
const UINT32 VIDEO_BIT_RATE = 950000;
const GUID   VIDEO_ENCODING_FORMAT = MFVideoFormat_H264;
const GUID   VIDEO_INPUT_FORMAT = MFVideoFormat_RGB32;

template <class T> void SafeRelease(T** ppT)
{
	if (*ppT)
	{
		(*ppT)->Release();
		*ppT = NULL;
	}
}

namespace winrt::MediaEncodingNativeComponent::implementation
{
	HRESULT MediaEncoder::InitializeSinkWriter(IMFSinkWriter** ppWriter, DWORD* pStreamIndex)
	{
		*ppWriter = NULL;
		*pStreamIndex = NULL;

		IMFSinkWriter* pSinkWriter = NULL;
		IMFMediaType* pMediaTypeOut = NULL;
		IMFMediaType* pMediaTypeIn = NULL;
		DWORD           streamIndex;

		HRESULT hr = MFCreateSinkWriterFromURL(_videoPath.c_str(), NULL, NULL, &pSinkWriter);

		// Set the output media type.
		if (SUCCEEDED(hr))
		{
			hr = MFCreateMediaType(&pMediaTypeOut);
		}
		if (SUCCEEDED(hr))
		{
			hr = pMediaTypeOut->SetGUID(MF_MT_MAJOR_TYPE, MFMediaType_Video);
		}
		if (SUCCEEDED(hr))
		{
			hr = pMediaTypeOut->SetGUID(MF_MT_SUBTYPE, VIDEO_ENCODING_FORMAT);
		}
		if (SUCCEEDED(hr))
		{
			hr = pMediaTypeOut->SetUINT32(MF_MT_AVG_BITRATE, VIDEO_BIT_RATE);
		}
		if (SUCCEEDED(hr))
		{
			hr = pMediaTypeOut->SetUINT32(MF_MT_INTERLACE_MODE, MFVideoInterlace_Progressive);
		}
		if (SUCCEEDED(hr))
		{
			hr = MFSetAttributeSize(pMediaTypeOut, MF_MT_FRAME_SIZE, _videoWidth, _videoHeight);
		}
		if (SUCCEEDED(hr))
		{
			hr = MFSetAttributeRatio(pMediaTypeOut, MF_MT_FRAME_RATE, VIDEO_FPS, 1);
		}
		if (SUCCEEDED(hr))
		{
			hr = MFSetAttributeRatio(pMediaTypeOut, MF_MT_PIXEL_ASPECT_RATIO, 1, 1);
		}
		if (SUCCEEDED(hr))
		{
			hr = pSinkWriter->AddStream(pMediaTypeOut, &streamIndex);
		}

		// Set the input media type.
		if (SUCCEEDED(hr))
		{
			hr = MFCreateMediaType(&pMediaTypeIn);
		}
		if (SUCCEEDED(hr))
		{
			hr = pMediaTypeIn->SetGUID(MF_MT_MAJOR_TYPE, MFMediaType_Video);
		}
		if (SUCCEEDED(hr))
		{
			hr = pMediaTypeIn->SetGUID(MF_MT_SUBTYPE, VIDEO_INPUT_FORMAT);
		}
		if (SUCCEEDED(hr))
		{
			hr = pMediaTypeIn->SetUINT32(MF_MT_INTERLACE_MODE, MFVideoInterlace_Progressive);
		}
		if (SUCCEEDED(hr))
		{
			hr = MFSetAttributeSize(pMediaTypeIn, MF_MT_FRAME_SIZE, _videoWidth, _videoHeight);
		}
		if (SUCCEEDED(hr))
		{
			hr = MFSetAttributeRatio(pMediaTypeIn, MF_MT_FRAME_RATE, VIDEO_FPS, 1);
		}
		if (SUCCEEDED(hr))
		{
			hr = MFSetAttributeRatio(pMediaTypeIn, MF_MT_PIXEL_ASPECT_RATIO, 1, 1);
		}
		if (SUCCEEDED(hr))
		{
			hr = pSinkWriter->SetInputMediaType(streamIndex, pMediaTypeIn, NULL);
		}

		// Tell the sink writer to start accepting data.
		if (SUCCEEDED(hr))
		{
			hr = pSinkWriter->BeginWriting();
		}

		// Return the pointer to the caller.
		if (SUCCEEDED(hr))
		{
			*ppWriter = pSinkWriter;
			(*ppWriter)->AddRef();
			*pStreamIndex = streamIndex;
		}

		SafeRelease(&pSinkWriter);
		SafeRelease(&pMediaTypeOut);
		SafeRelease(&pMediaTypeIn);
		return hr;
	}

	HRESULT MediaEncoder::WriteFrame(IMFSinkWriter* pWriter, byte* videoFrameBuffer, DWORD streamIndex, const LONGLONG& rtStart, const LONGLONG& duration)
	{
		IMFSample* pSample = NULL;
		IMFMediaBuffer* pBuffer = NULL;

		const LONG cbWidth = 4 * _videoWidth;
		const DWORD cbBuffer = cbWidth * _videoHeight;

		BYTE* pData = NULL;

		// Create a new memory buffer.
		HRESULT hr = MFCreateMemoryBuffer(cbBuffer, &pBuffer);

		// Lock the buffer and copy the video frame to the buffer.
		if (SUCCEEDED(hr))
		{
			hr = pBuffer->Lock(&pData, NULL, NULL);
		}
		if (SUCCEEDED(hr))
		{
			hr = MFCopyImage(
				pData,                      // Destination buffer.
				cbWidth,                    // Destination stride.
				(BYTE*)videoFrameBuffer,    // First row in source image.
				cbWidth,                    // Source stride.
				cbWidth,                    // Image width in bytes.
				_videoHeight                // Image height in pixels.
			);
		}
		if (pBuffer)
		{
			pBuffer->Unlock();
		}

		// Set the data length of the buffer.
		if (SUCCEEDED(hr))
		{
			hr = pBuffer->SetCurrentLength(cbBuffer);
		}

		// Create a media sample and add the buffer to the sample.
		if (SUCCEEDED(hr))
		{
			hr = MFCreateSample(&pSample);
		}
		if (SUCCEEDED(hr))
		{
			hr = pSample->AddBuffer(pBuffer);
		}

		// Set the time stamp and the duration.
		if (SUCCEEDED(hr))
		{
			hr = pSample->SetSampleTime(rtStart);
		}
		if (SUCCEEDED(hr))
		{
			hr = pSample->SetSampleDuration(duration);
		}

		// Send the sample to the Sink Writer.
		if (SUCCEEDED(hr))
		{
			hr = pWriter->WriteSample(streamIndex, pSample);
		}

		SafeRelease(&pSample);
		SafeRelease(&pBuffer);
		return hr;
	}

	LONGLONG rtStart = 0;

    bool MediaEncoder::OpenVideoWriter(hstring const& videoPath, int videoWidth, int videoHeight)
    {
		_videoPath = videoPath;
		_videoWidth = videoWidth;
		_videoHeight = videoHeight;

		rtStart = 0;

		HRESULT hr = CoInitializeEx(NULL, COINIT_APARTMENTTHREADED);
		if (SUCCEEDED(hr))
		{
			hr = MFStartup(MF_VERSION);
			if (SUCCEEDED(hr))
			{
				hr = InitializeSinkWriter(&pSinkWriter, &streamIndex);
				if (SUCCEEDED(hr))
				{
					return true;
				}
			}
		}
		return false;
    }

    void MediaEncoder::WriteVideoFrame(array_view<uint8_t const> frameBuffer, INT64 duration)
    {
		// Send frames to the sink writer.
		HRESULT hr = WriteFrame(pSinkWriter, (byte*)frameBuffer.data(), streamIndex, rtStart, duration);
		rtStart += duration;
    }
    void MediaEncoder::CloseVideoWriter()
    {
		if (pSinkWriter != nullptr)
		{
			HRESULT hr = pSinkWriter->Finalize();
			SafeRelease(&pSinkWriter);
			MFShutdown();
			CoUninitialize();
		}
    }
}

#pragma once
#include "MediaEncoder.g.h"

#include <Windows.h>
#include <mfapi.h>
#include <mfidl.h>
#include <Mfreadwrite.h>
#include <mferror.h>

#pragma comment(lib, "mfreadwrite")
#pragma comment(lib, "mfplat")
#pragma comment(lib, "mfuuid")

namespace winrt::MediaEncodingNativeComponent::implementation
{
    struct MediaEncoder : MediaEncoderT<MediaEncoder>
    {
        MediaEncoder() = default;

        bool OpenVideoWriter(hstring const& videoPath, int videoWidth, int videoHeight);
		void WriteVideoFrame(array_view<uint8_t const> frameBuffer, INT64 duration);
        void CloseVideoWriter();
	private:
		HRESULT InitializeSinkWriter(IMFSinkWriter** ppWriter, DWORD* pStreamIndex);
		HRESULT WriteFrame(IMFSinkWriter* pWriter, byte* videoFrameBuffer, DWORD streamIndex, const LONGLONG& rtStart, const LONGLONG& duration);
	private:
		std::wstring _videoPath;
		UINT32 _videoWidth;
		UINT32 _videoHeight;
		IMFSinkWriter* pSinkWriter = NULL;
		DWORD streamIndex;
    };
}
namespace winrt::MediaEncodingNativeComponent::factory_implementation
{
    struct MediaEncoder : MediaEncoderT<MediaEncoder, implementation::MediaEncoder>
    {
    };
}

#include "pch.h"
#include "WaveHeader.h"

#include <vector>
#include <winrt/Windows.Security.Cryptography.h>


WaveReader::WaveReader()
{
}


WaveReader::~WaveReader()
{
}

IAsyncAction WaveReader::Open(StorageFile file)
{
	auto _ras = co_await file.OpenAsync(Windows::Storage::FileAccessMode::Read);
	auto inputStream = _ras.GetInputStreamAt(0);
	_dataReader = Windows::Storage::Streams::DataReader(inputStream);

	_dataReader.UnicodeEncoding(Windows::Storage::Streams::UnicodeEncoding::Utf8);
	_dataReader.ByteOrder(Windows::Storage::Streams::ByteOrder::LittleEndian);

	co_await _dataReader.LoadAsync(_ras.Size());
}

void WaveReader::ReadWaveformat(WAVEFORMATEX* pFormat,DWORD* pDataSize)
{
	DWORD waveHeaderSize = sizeof(WAVEHEADER) + sizeof(WAVEFORMATEX);// +pFormat->cbSize + sizeof(WaveData) + sizeof(DWORD);
	auto headerBuffer = _dataReader.ReadBuffer(waveHeaderSize);
	CopyMemory(pFormat, headerBuffer.data() + sizeof(WAVEHEADER), sizeof(WAVEFORMATEX));
	auto skipLength = pFormat->cbSize + sizeof(WaveData);
	_dataReader.ReadBuffer(skipLength);
	*pDataSize = _dataReader.ReadUInt32();
}

void WaveReader::ReadSampleBuffer(float* sampleBuffer, DWORD sampleCount, DWORD sampleSize)
{
	for (size_t i = 0; i < sampleCount; i++)
	{
		auto buffer = _dataReader.ReadBuffer(sampleSize);
		auto floatBuffer = reinterpret_cast<float*>(buffer.data());
		sampleBuffer[i] = floatBuffer[0];
	}
}

void WaveReader::ReadBuffer(DWORD sampleSize,const std::function<void(byte*, DWORD)> & func)
{
	auto buffer = _dataReader.ReadBuffer(sampleSize);
	func(buffer.data(), buffer.Length());
}
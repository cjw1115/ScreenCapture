#pragma once
#include <winrt/Windows.Foundation.h>
#include <winrt/Windows.Storage.h>
#include <winrt/Windows.Storage.Streams.h>

#include <functional>

using namespace winrt;
using namespace Windows::Foundation;
using namespace Windows::Storage;
using namespace Windows::Storage::Streams;

struct WAVEHEADER
{
    DWORD   dwRiff;                     // "RIFF"
    DWORD   dwSize;                     // Size
    DWORD   dwWave;                     // "WAVE"
    DWORD   dwFmt;                      // "fmt "
    DWORD   dwFmtSize;                  // Wave Format Size
};

const BYTE WaveHeader[] = { 'R','I','F','F',  0x00,  0x00,  0x00,  0x00, 'W','A','V','E','f','m','t',' ', 0x00, 0x00, 0x00, 0x00 };
const BYTE WaveData[] = { 'd', 'a', 't', 'a' };

class WaveWriter
{
    DWORD _totalHeaderSize = 0;
	DWORD _totalDataSize= 0;
    IRandomAccessStream _ras{ nullptr };
    WAVEFORMATEX* _waveFormat;
public:
    
    WaveWriter();
    ~WaveWriter();

    IAsyncAction Open(StorageFile file);
    void Save();
    void WriteHeader(WAVEFORMATEX* pFormat);
    void WriteData(byte* buffer, DWORD bufferLength);

    int Duration();
};

class WaveReader
{
public:
	WaveReader();
	~WaveReader();

	IAsyncAction Open(StorageFile file);
	void ReadWaveformat(WAVEFORMATEX* pFormat, DWORD* pDataSize);
	void ReadSampleBuffer(float* sampleBuffer, DWORD sampleCount, DWORD sampleSize);
	void ReadBuffer(DWORD sampleSize, const std::function<void(byte*, DWORD)>& func);
private:
	Windows::Storage::Streams::DataReader _dataReader{ nullptr };
};
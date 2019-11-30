#include "pch.h"
#include "WaveHeader.h"

#include <vector>
#include <winrt/Windows.Security.Cryptography.h>

WaveWriter::WaveWriter()
{
}

WaveWriter::~WaveWriter()
{
}

IAsyncAction WaveWriter::Open(StorageFile file)
{
    _ras = co_await file.OpenAsync(Windows::Storage::FileAccessMode::ReadWrite);
}

void WaveWriter::Save()
{
    _ras.Seek(sizeof(DWORD));
    
    byte buffer[4];
    memset(buffer, 0, sizeof(byte) * 4);
    byte* pBuffer = buffer;
    *((DWORD*)pBuffer) = _totalHeaderSize - (2 * sizeof(DWORD)) + _totalDataSize; //0 means Data is empty;

    auto bufferView = winrt::array_view<byte const>((uint8_t*)pBuffer, (uint8_t*)(pBuffer + sizeof(DWORD)));
    auto winrtBuffer = winrt::Windows::Security::Cryptography::CryptographicBuffer::CreateFromByteArray(bufferView);
    _ras.WriteAsync(winrtBuffer).get();

    _ras.Seek(_totalHeaderSize - sizeof(DWORD));

    memset(buffer, 0, sizeof(byte) * 4);
    *((DWORD*)pBuffer) = _totalDataSize;
    bufferView = winrt::array_view<byte const>((uint8_t*)buffer, (uint8_t*)(buffer + sizeof(DWORD)));
    winrtBuffer = winrt::Windows::Security::Cryptography::CryptographicBuffer::CreateFromByteArray(bufferView);
    _ras.WriteAsync(winrtBuffer).get();

    _ras.FlushAsync().get();
    _ras.Close();
}

void WaveWriter::WriteHeader(WAVEFORMATEX* pFormat)
{
    _waveFormat = pFormat;

    DWORD waveHeaderSize = sizeof(WAVEHEADER) + sizeof(WAVEFORMATEX) + pFormat->cbSize + sizeof(WaveData) + sizeof(DWORD);
    _totalHeaderSize = waveHeaderSize;

    byte* headerBuffer = new (std::nothrow) byte[waveHeaderSize];
    memset(headerBuffer, 0, waveHeaderSize);

    byte* waveFilePointer = headerBuffer;
    WAVEHEADER* waveHeader = reinterpret_cast<WAVEHEADER*>(headerBuffer);
    CopyMemory(waveFilePointer, WaveHeader, sizeof(WaveHeader));
    waveFilePointer += sizeof(WaveHeader);

    waveHeader->dwSize = waveHeaderSize - (2 * sizeof(DWORD)) + 0; //0 means Data is empty;
    waveHeader->dwFmtSize = sizeof(WAVEFORMATEX) + pFormat->cbSize;
    //need to copy the extra data to wave file.
    CopyMemory(waveFilePointer, pFormat, waveHeader->dwFmtSize);
    waveFilePointer += waveHeader->dwFmtSize;

    CopyMemory(waveFilePointer, WaveData, sizeof(WaveData));
    waveFilePointer += sizeof(WaveData);

    waveFilePointer += sizeof(DWORD);

    auto headerBufferView = winrt::array_view<byte const>((uint8_t*)headerBuffer, (uint8_t*)(headerBuffer + waveHeaderSize));
    auto winrtBuffer = winrt::Windows::Security::Cryptography::CryptographicBuffer::CreateFromByteArray(headerBufferView);
    _ras.WriteAsync(winrtBuffer).get();
    delete[] headerBuffer;
}

void WaveWriter::WriteData(byte* buffer, DWORD bufferLength)
{
    auto bufferView = winrt::array_view<byte const>((uint8_t*)buffer, (uint8_t*)(buffer + bufferLength));
    auto winrtBuffer = winrt::Windows::Security::Cryptography::CryptographicBuffer::CreateFromByteArray(bufferView);
    _ras.WriteAsync(winrtBuffer).get();

    _totalDataSize += bufferLength;
}

int WaveWriter::Duration()
{
    auto toatalBytePerSec = _waveFormat->nSamplesPerSec * _waveFormat->nChannels * (_waveFormat->wBitsPerSample / 8);
    return _totalDataSize * 1000 / toatalBytePerSec;
}
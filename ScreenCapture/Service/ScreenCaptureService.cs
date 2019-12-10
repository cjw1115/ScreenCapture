using MediaEncodingNativeComponent;
using Microsoft.Graphics.Canvas;
using ScreenCapture.Helper;
using ScreenCapture.Model;
using ScreenCaptureNativeComponent;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.Imaging;
using Windows.Media.Editing;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml.Controls;

namespace ScreenCapture.Service
{
    public class ScreenCaptureService
    {
        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceCounter(out long lpPerformanceCount);

        private readonly string CAPTURE_FOLDER = "CaptureFolder";
        private Direct3D11CaptureFramePool _framePool;
        private GraphicsCaptureSession _seesion;
        private StorageFolder _captureFolder;
        private string _captureVideoFilePath;
        private TimeSpan _startTime;

        private uint _imageCounter = 0;
        private bool _capturing = false;

        private MediaEncoder _mediaEncoder = new MediaEncoder();

        private async Task<StorageFolder> _setupCpatureFolder()
        {
            StorageFolder storageFolder = null;
            try
            {
                storageFolder = await Windows.Storage.ApplicationData.Current.LocalCacheFolder.GetFolderAsync(CAPTURE_FOLDER);
                if (storageFolder != null)
                {
                    await storageFolder.DeleteAsync(StorageDeleteOption.PermanentDelete);
                }
            }
            catch (FileNotFoundException e)
            {
            }
            catch
            {
                throw;
            }
            storageFolder = await Windows.Storage.ApplicationData.Current.LocalCacheFolder.CreateFolderAsync(CAPTURE_FOLDER);
            return storageFolder;
        }

        public async Task StartCaptureAsync()
        {
            var picker = new GraphicsCapturePicker();
            GraphicsCaptureItem item = await picker.PickSingleItemAsync();
            if (item != null)
            {
                _captureFolder = await _setupCpatureFolder();
                _startCaptureInternal(item);
            }
        }

        public void StopCapture()
        {
            _capturing = false;
        }

        private void _startCaptureInternal(GraphicsCaptureItem item)
        {
            _captureVideoFilePath = System.IO.Path.Combine(_captureFolder.Path, "ouput.wmv");

            _mediaEncoder.OpenVideoWriter(_captureVideoFilePath, item.Size.Width, item.Size.Height);

            _framePool = Direct3D11CaptureFramePool.Create(CanvasDevice.GetSharedDevice(), DirectXPixelFormat.B8G8R8A8UIntNormalized, 1, item.Size);
            _seesion = _framePool.CreateCaptureSession(item);
            _seesion.StartCapture();
            _capturing = true;

            QueryPerformanceCounter(out long counter);
            _startTime = TimeSpan.FromTicks(counter);
            Task.Run(_onFrameArraved);
        }

        [ComImport]
        [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        unsafe interface IMemoryBufferByteAccess
        {
            void GetBuffer(out byte* buffer, out uint capacity);
        }

        private async void _onFrameArraved()
        {
            TimeSpan _lastTimeStamp = _startTime;
            while (_capturing)
            {
                QueryPerformanceCounter(out long start);
                using (var frame = _framePool.TryGetNextFrame())
                {
                    if (frame == null)
                        continue;
                    using (var bitmap = await SoftwareBitmap.CreateCopyFromSurfaceAsync(frame.Surface, BitmapAlphaMode.Premultiplied))
                    {
                        _processBitmap(bitmap, frame.SystemRelativeTime - _lastTimeStamp);
                        _lastTimeStamp = frame.SystemRelativeTime;
                    }
                }
                QueryPerformanceCounter(out long end);
                var spendTime = TimeSpan.FromTicks(end - start);
                if (spendTime < TimeSpan.FromMilliseconds(41))
                {
                    await Task.Delay((TimeSpan.FromMilliseconds(41) - spendTime).Milliseconds);
                }
            }
            _seesion.Dispose();
            _framePool.Dispose();
            _mediaEncoder.CloseVideoWriter();
        }

        private unsafe void _processBitmap(SoftwareBitmap bitmap,TimeSpan duration)
        {
            using (var buffer = bitmap.LockBuffer(BitmapBufferAccessMode.Read))
            {
                var reference = (IMemoryBufferByteAccess)buffer.CreateReference();
                
                reference.GetBuffer(out byte* nativeBuffer, out uint capacity);
                byte[] frameBuffer = new byte[capacity];
                for (int i = 0; i < bitmap.PixelHeight; i++)
                {
                    for (int j = 0; j < bitmap.PixelWidth; j++)
                    {
                        var indexManaged = (i * bitmap.PixelWidth + j) * 4;
                        var indexNative = ((bitmap.PixelHeight - 1 - i) * bitmap.PixelWidth + j) * 4;
                        frameBuffer[indexManaged + 0] = nativeBuffer[indexNative + 0];
                        frameBuffer[indexManaged + 1] = nativeBuffer[indexNative + 1];
                        frameBuffer[indexManaged + 2] = nativeBuffer[indexNative + 2];
                        frameBuffer[indexManaged + 3] = nativeBuffer[indexNative + 3];
                    }
                }
                _mediaEncoder.WriteVideoFrame(frameBuffer, duration.Ticks);
            }
        }

        public async Task SetupMediaComposition(MediaComposition mediaComposition)
        {
            var captureVideoFile = await StorageFile.GetFileFromPathAsync(_captureVideoFilePath);
            var clip = await MediaClip.CreateFromFileAsync(captureVideoFile);
            mediaComposition.Clips.Add(clip);
        }
    }
}

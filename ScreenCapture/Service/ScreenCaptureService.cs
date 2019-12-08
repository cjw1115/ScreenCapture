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
    public class ScreenCaptureService:IScreenCaptureService
    {
        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceCounter(out long lpPerformanceCount);

        private readonly string CAPTURE_FOLDER = "CaptureFolder";
        private MediaComposition _mediaComposition;
        private Direct3D11CaptureFramePool _framePool;
        private GraphicsCaptureSession _seesion;
        private StorageFolder _captureFolder;
        private string _captureVideoFilePath;
        private TimeSpan _startTime;

        private ConcurrentQueue<CapturedVideoFrame> _capturedImages;
        private ConcurrentQueue<TimeSpan> _captuedRelatedTimes;
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

        private async Task StartCaptureAsync()
        {
            var picker = new GraphicsCapturePicker();
            GraphicsCaptureItem item = await picker.PickSingleItemAsync();
            if (item != null)
            {
                _captureFolder = await _setupCpatureFolder();
                _startCaptureInternal(item);
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                Task.Run(() =>
                {
                    _renderCaptuedImagesToFiles();
                });
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }
        }

        private void StopCapture()
        {
            _capturing = false;
            _seesion.Dispose();
            _framePool.Dispose();
            _mediaEncoder.CloseVideoWriter();
        }

        private void _startCaptureInternal(GraphicsCaptureItem item)
        {
            _capturedImages = new ConcurrentQueue<CapturedVideoFrame>();
            _captuedRelatedTimes = new ConcurrentQueue<TimeSpan>();

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
            int index = 1;
            TimeSpan _lastTimeStamp = _startTime;
            while (_capturing)
            {
                QueryPerformanceCounter(out long start);
                using (var frame = _framePool.TryGetNextFrame())
                {
                    if (frame == null)
                        continue;
                    using(var bitmap = await SoftwareBitmap.CreateCopyFromSurfaceAsync(frame.Surface, BitmapAlphaMode.Premultiplied))
                    {
                        _processBitmap(bitmap, frame.SystemRelativeTime - _lastTimeStamp);
                        _lastTimeStamp = frame.SystemRelativeTime;
                    }
                    //_capturedImages.Enqueue(new CapturedVideoFrame(index++, bitmap));
                    //_captuedRelatedTimes.Enqueue(frame.SystemRelativeTime);
                }
                QueryPerformanceCounter(out long end);
                var spendTime = TimeSpan.FromTicks(end - start);
                if (spendTime < TimeSpan.FromMilliseconds(41))
                {
                    await Task.Delay((TimeSpan.FromMilliseconds(41) - spendTime).Milliseconds);
                }
            }
        }

        private unsafe void _processBitmap(SoftwareBitmap bitmap,TimeSpan duration)
        {
            using (var buffer = bitmap.LockBuffer(BitmapBufferAccessMode.Read))
            {
                var reference = (IMemoryBufferByteAccess)buffer.CreateReference();

                reference.GetBuffer(out byte* nativeBuffer, out uint capacity);
                byte[] frameBuffer = new byte[capacity];
                Marshal.Copy((IntPtr)nativeBuffer, frameBuffer, 0, frameBuffer.Length);
                _mediaEncoder.WriteVideoFrame(frameBuffer, duration.Ticks);
            }
        }

        private async Task<bool> VerifyDuration()
        {
            var durations = _captuedRelatedTimes.ToList();
            var last = durations[0];
            for (int i = 1; i < durations.Count - 1; i++)
            {
                if (!(durations[i] - last > TimeSpan.FromSeconds(0)))
                {
                    await new MessageDialog("Duration error").ShowAsync();
                    return false;
                }
                else
                {
                    last = durations[i];
                }
            }
            return true;
        }

        private async Task SetupMediaComposition(MediaComposition mediaComposition)
        {
            _mediaComposition = mediaComposition;
            var captureVideoFile = await StorageFile.GetFileFromPathAsync(_captureVideoFilePath);
            var clip = await MediaClip.CreateFromFileAsync(captureVideoFile);
            mediaComposition.Clips.Add(clip);
            //var _capturedImageFiles = await _captureFolder.GetFilesAsync();
            //var lastTime = _startTime;
            //foreach (var imageFile in _capturedImageFiles)
            //{
            //    if (_captuedRelatedTimes.TryDequeue(out TimeSpan relatedTime))
            //    {
            //        var duration = relatedTime - lastTime;
            //        lastTime = relatedTime;
            //        MediaClip clip = await MediaClip.CreateFromImageFileAsync(imageFile, duration);
            //        _mediaComposition.Clips.Add(clip);
            //    }
            //}
        }

        private async Task RenderCompositionToFile(Windows.Storage.StorageFile file,Windows.UI.Xaml.Controls.ProgressBar progressBar)
        {
            throw new NotImplementedException();
        }

        private async Task _renderCaptuedImagesToFiles()
        {
            while (true)
            {
                bool re = false;
                do
                {
                    re = _capturedImages.TryDequeue(out var videoFrame);
                    if (re)
                    {
                        Task.Run(async () =>
                        {
                            var file = await _captureFolder.CreateFileAsync($"Screenshot_{videoFrame.Index}.jpeg");
                            await ImageHelper.SaveSoftwareBitmapToFileAsync(videoFrame.Bitmap, file);
                            videoFrame.Bitmap.Dispose();
                        });
                    }
                }
                while (re);
                await Task.Delay(10);
            }
        }

        private async Task WaitForImageRenderring()
        {
            while (_capturedImages.Count > 0)
            {
                await Task.Delay(20);
            }
            return;
        }

        #region Interface proxy
        IAsyncAction IScreenCaptureService.StartCaptureAsync()
        {
            return StartCaptureAsync().AsAsyncAction();
        }

        void IScreenCaptureService.VerifyDuration()
        {
        }

        IAsyncAction IScreenCaptureService.SetupMediaComposition(MediaComposition mediaComposition)
        {
            return SetupMediaComposition(mediaComposition).AsAsyncAction();
        }

        IAsyncAction IScreenCaptureService.RenderCompositionToFile(StorageFile file, ProgressBar progressBar)
        {
            return RenderCompositionToFile(file, progressBar).AsAsyncAction();
        }

        IAsyncAction IScreenCaptureService.WaitForImageRenderring()
        {
            return WaitForImageRenderring().AsAsyncAction();
        }

        void IScreenCaptureService.StopCapture()
        {
            StopCapture();
        }
        #endregion
    }
}

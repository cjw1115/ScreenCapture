using Microsoft.Graphics.Canvas;
using ScreenCapture.Helper;
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
using Windows.Media.MediaProperties;
using Windows.Media.Transcoding;
using Windows.Storage;
using Windows.UI.Popups;

namespace ScreenCapture.Service
{
    public class ScreenCaptureService
    {
        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceCounter(out long lpPerformanceCount);

        private readonly string CAPTURE_FOLDER = "CaptureFolder";
        private MediaComposition _mediaComposition;
        private Direct3D11CaptureFramePool _framePool;
        private GraphicsCaptureSession _seesion;
        private StorageFolder _captureFolder;
        private TimeSpan _startTime;

        private ConcurrentQueue<SoftwareBitmap> _capturedImages;
        private ConcurrentQueue<TimeSpan> _captuedRelatedTimes;
        private uint _imageCounter = 0;


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
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                Task.Run(() =>
                {
                    _renderCaptuedImagesToFiles();
                });
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }
        }

        public void StopCapture()
        {
            _seesion.Dispose();
            _framePool.Dispose();
        }

        private void _startCaptureInternal(GraphicsCaptureItem item)
        {
            _capturedImages = new ConcurrentQueue<SoftwareBitmap>();
            _captuedRelatedTimes = new ConcurrentQueue<TimeSpan>();

            _framePool = Direct3D11CaptureFramePool.Create(CanvasDevice.GetSharedDevice(), DirectXPixelFormat.B8G8R8A8UIntNormalized, 3, item.Size);
            _framePool.FrameArrived += (o, e) =>
            {
                using (var frame = _framePool.TryGetNextFrame())
                {
                    var bitmap = SoftwareBitmap.CreateCopyFromSurfaceAsync(frame.Surface, BitmapAlphaMode.Premultiplied).AsTask().Result;

                    _capturedImages.Enqueue(bitmap);
                    _captuedRelatedTimes.Enqueue(frame.SystemRelativeTime);
                }
            };
            _seesion = _framePool.CreateCaptureSession(item);
            _seesion.StartCapture();
            QueryPerformanceCounter(out long counter);
            _startTime = TimeSpan.FromTicks(counter);
        }

        public async Task<bool> VerifyDuration()
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

        public async Task SetupMediaComposition()
        {
            var _capturedImageFiles = await _captureFolder.GetFilesAsync();
            var fileNames = _capturedImageFiles.Select(m => m.Name).ToList();
            _mediaComposition = new MediaComposition();
            var lastTime = _startTime;
            foreach (var imageFile in _capturedImageFiles)
            {
                if (_captuedRelatedTimes.TryDequeue(out TimeSpan relatedTime))
                {
                    var duration = relatedTime - lastTime;
                    lastTime = relatedTime;
                    MediaClip clip = await MediaClip.CreateFromImageFileAsync(imageFile, duration);
                    _mediaComposition.Clips.Add(clip);
                }
            }
        }

        public async Task RenderCompositionToFile(Windows.Storage.StorageFile file,Windows.UI.Xaml.Controls.ProgressBar progressBar)
        {
            if (file != null)
            {
                // Call RenderToFileAsync
                var mp4Profile = MediaEncodingProfile.CreateMp4(VideoEncodingQuality.HD720p);
                var saveOperation = _mediaComposition.RenderToFileAsync(file, MediaTrimmingPreference.Precise, mp4Profile);
                saveOperation.Progress = new AsyncOperationProgressHandler<TranscodeFailureReason, double>((info, progress) =>
                {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    progressBar.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        progressBar.Value = progress;
                    });
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                });
                saveOperation.Completed = new AsyncOperationWithProgressCompletedHandler<TranscodeFailureReason, double>((info, status) =>
                {
                    var results = info.GetResults();
                    if (results != TranscodeFailureReason.None || status != AsyncStatus.Completed)
                    {
                        throw new Exception("Saving was unsuccessful");
                    }
                });
                while(saveOperation.Status ==  AsyncStatus.Started)
                {
                    await Task.Delay(20);
                }
            }
            else
            {
                throw new Exception("User cancelled the file selection");
            }
        }

        private async Task _renderCaptuedImagesToFiles()
        {
            while (true)
            {
                bool re = false;
                do
                {
                    re = _capturedImages.TryDequeue(out SoftwareBitmap bitmap);
                    if (re)
                    {
                        var file = await _captureFolder.CreateFileAsync($"Screenshot_{++_imageCounter}.jpeg");
                        await ImageHelper.SaveSoftwareBitmapToFileAsync(bitmap, file);
                        bitmap.Dispose();
                    }
                }
                while (re);
                await Task.Delay(10);
            }
        }

        public async Task WaitForImageRenderring()
        {
            while (_capturedImages.Count > 0)
            {
                await Task.Delay(20);
            }
            return;
        }
    }
}

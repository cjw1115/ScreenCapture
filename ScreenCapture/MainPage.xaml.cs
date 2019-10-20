using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.Imaging;
using Windows.Media.Editing;
using Windows.Media.Transcoding;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System.Collections.Concurrent;
using Microsoft.Graphics.Canvas;
using System.Runtime.InteropServices;
using Windows.Media.MediaProperties;
using Windows.UI;
using Windows.UI.Xaml.Media;

namespace ScreenCapture
{
    public sealed partial class MainPage : Page
    {
        
        public MainPage()
        {
            InitializeComponent();
        }

        private readonly string CAPTURE_FOLDER = "CaptureFolder";

        private MediaComposition _mediaComposition;
        private Direct3D11CaptureFramePool _framePool;
        private GraphicsCaptureSession _seesion;
        private StorageFolder _captureFolder;
        private async Task<StorageFolder> _setupCpatureFolder()
        {
            StorageFolder storageFolder = null;

            storageFolder = await Windows.Storage.ApplicationData.Current.LocalCacheFolder.GetFolderAsync(CAPTURE_FOLDER);
            if (storageFolder != null)
            {
                await storageFolder.DeleteAsync(StorageDeleteOption.PermanentDelete);
            }
            storageFolder = await Windows.Storage.ApplicationData.Current.LocalCacheFolder.CreateFolderAsync(CAPTURE_FOLDER);

            return storageFolder;
        }

        private List<StorageFile> _capturedImageFiles;
        private ConcurrentQueue<SoftwareBitmap> _capturedImages;
        private ConcurrentQueue<TimeSpan> _captuedRelatedTimes;
        private readonly object _lockObejct = new object();
        private uint _imageCounter = 0;
        private ConcurrentQueue<SoftwareBitmap> _iamgeQueues;

        private uint filter = 0;

        private TimeSpan _startTime;

        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceCounter(out long lpPerformanceCount);

        private async Task _startCaptureAsync()
        {
            var picker = new GraphicsCapturePicker();
            GraphicsCaptureItem item = await picker.PickSingleItemAsync();

            if (item != null)
            {
                _captureFolder = await _setupCpatureFolder();
                _startCaptureInternal(item);

                Task.Run(() =>
                {
                    _renderCaptuedImagesToFiles();
                });
            }
        }

        private void _startCaptureInternal(GraphicsCaptureItem item)
        {
            _capturedImageFiles = new List<StorageFile>();
            _capturedImages = new ConcurrentQueue<SoftwareBitmap>();
            _iamgeQueues = new ConcurrentQueue<SoftwareBitmap>();
            _captuedRelatedTimes = new ConcurrentQueue<TimeSpan>();

            _framePool = Direct3D11CaptureFramePool.Create(CanvasDevice.GetSharedDevice(), DirectXPixelFormat.B8G8R8A8UIntNormalized, 2, item.Size);
            _framePool.FrameArrived += async (o, e) =>
            {

                using (var frame = _framePool.TryGetNextFrame())
                {
                    if (filter++ % 2 == 0)
                    {
                        var softwareBitmap = await Windows.Graphics.Imaging.SoftwareBitmap.CreateCopyFromSurfaceAsync(frame.Surface, Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied);
                        _capturedImages.Enqueue(softwareBitmap);
                        _captuedRelatedTimes.Enqueue(frame.SystemRelativeTime);
                    }
                }
            };
            _seesion = _framePool.CreateCaptureSession(item);
            _seesion.StartCapture();
            QueryPerformanceCounter(out long counter);
            _startTime = TimeSpan.FromTicks(counter);
        }

        private async Task _setupMediaComposition()
        {
            try
            {
                var _capturedImageFiles = await _captureFolder.GetFilesAsync();
                _mediaComposition = new MediaComposition();
                int i = 0;
                var lastTime = _startTime;
                foreach (var imageFile in _capturedImageFiles)
                {
                    if(_captuedRelatedTimes.TryDequeue(out TimeSpan relatedTime))
                    {
                        var duration = relatedTime - lastTime;
                        lastTime = relatedTime;
                        MediaClip clip = await MediaClip.CreateFromImageFileAsync(imageFile, duration);
                        _mediaComposition.Clips.Add(clip);
                    }
                    
                }
            }
            catch
            {

            }
            
        }


        private async Task _renderCompositionToFile(StorageFile file)
        {
            if (file != null)
            {
                // Call RenderToFileAsync
                var mp4Profile = MediaEncodingProfile.CreateMp4(VideoEncodingQuality.HD1080p);
                var saveOperation = _mediaComposition.RenderToFileAsync(file, MediaTrimmingPreference.Precise, mp4Profile);

                saveOperation.Progress = new AsyncOperationProgressHandler<TranscodeFailureReason, double>((info, progress) =>
                {
                    progressBar.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => 
                    {
                        progressBar.Value = progress;
                    });
                });
                saveOperation.Completed = new AsyncOperationWithProgressCompletedHandler<TranscodeFailureReason, double>((info, status) =>
                {
                    try
                    {
                        var results = info.GetResults();
                        if (results != TranscodeFailureReason.None || status != AsyncStatus.Completed)
                        {
                            System.Diagnostics.Debug.WriteLine(("Saving was unsuccessful"));
                        }
                        else
                        {
                            progressBar.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                            {
                                progressBar.Foreground = new SolidColorBrush(Colors.Blue);
                            });
                            System.Diagnostics.Debug.WriteLine("Trimmed clip saved to file");
                        }
                    }
                    finally
                    {
                    }
                });
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("User cancelled the file selection");
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
                await Task.Delay(20);
            }
        }

        private async void _btnStartClickAsync(object sender, RoutedEventArgs e)
        {
            await _startCaptureAsync();
        }

        private void _btnStopClick(object sender, RoutedEventArgs e)
        {
            _seesion.Dispose();
            _framePool.Dispose();
        }

        private async void _btnSaveClick(object sender, RoutedEventArgs e)
        {
            try
            {
                progressBar.Foreground = new SolidColorBrush(Colors.Green);

                var picker = new Windows.Storage.Pickers.FileSavePicker();
                picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.VideosLibrary;
                picker.FileTypeChoices.Add("MP4 files", new List<string>() { ".mp4" });
                picker.SuggestedFileName = "RenderedComposition.mp4";

                Windows.Storage.StorageFile file = await picker.PickSaveFileAsync();

                while (_capturedImages.Count > 0)
                {
                    await Task.Delay(20);
                }
                await _setupMediaComposition();
                await _renderCompositionToFile(file);

                
            }
#pragma warning disable CS0168 // Variable is declared but never used
            catch (Exception)
#pragma warning restore CS0168 // Variable is declared but never used
            {
                progressBar.Foreground = new SolidColorBrush(Colors.Red);
            }

        }
    }
}
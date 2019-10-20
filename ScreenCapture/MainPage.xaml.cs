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

namespace ScreenCapture
{
    public sealed partial class MainPage : Page
    {
        
        public MainPage()
        {
            InitializeComponent();
        }

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
                while(_captuedImages.Count>0)
                {
                    await Task.Delay(20);
                }
                await _setupMediaComposition();
                await _renderCompositionToFile();
            }
#pragma warning disable CS0168 // Variable is declared but never used
            catch (Exception)
#pragma warning restore CS0168 // Variable is declared but never used
            {
            }

        }

        private async Task _renderCaptuedImagesToFiles()
        {
            while(true)
            {
                bool re = false;
                do
                {
                    re = _captuedImages.TryDequeue(out SoftwareBitmap bitmap);
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

        private MediaComposition _mediaComposition;
        private async Task _setupMediaComposition()
        {
            var _captureImageFiles =await _captureFolder.GetFilesAsync();
            _mediaComposition = new MediaComposition();
            foreach (var imageFile in _captureImageFiles)
            {
                MediaClip clip = await MediaClip.CreateFromImageFileAsync(imageFile,TimeSpan.FromMilliseconds(20));
                _mediaComposition.Clips.Add(clip);
            }
        }
        private async Task _renderCompositionToFile()
        {
            var picker = new Windows.Storage.Pickers.FileSavePicker();
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.VideosLibrary;
            picker.FileTypeChoices.Add("MP4 files", new List<string>() { ".mp4" });
            picker.SuggestedFileName = "RenderedComposition.mp4";

            Windows.Storage.StorageFile file = await picker.PickSaveFileAsync();
            if (file != null)
            {
                // Call RenderToFileAsync
                var saveOperation = _mediaComposition.RenderToFileAsync(file, MediaTrimmingPreference.Precise);

                saveOperation.Progress = new AsyncOperationProgressHandler<TranscodeFailureReason, double>((info, progress) =>
                {
                    System.Diagnostics.Debug.WriteLine(string.Format("Saving file... Progress: {0:F0}%", progress));
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


        Direct3D11CaptureFramePool _framePool;
        GraphicsCaptureSession _seesion;

        private readonly string CAPTURE_FOLDER = "CaptureFolder";
        private Windows.Storage.StorageFolder _captureFolder = null;
        async Task<StorageFolder> _setupCpatureFolder()
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

        private List<StorageFile> _captureImageFiles;
        private ConcurrentQueue<SoftwareBitmap> _captuedImages;
        private readonly object _lockObejct = new object();
        private uint _imageCounter = 0;
        private ConcurrentQueue<SoftwareBitmap> _iamgeQueues;
        private void _startCaptureInternal(GraphicsCaptureItem item)
        {
            _captureImageFiles = new List<StorageFile>();
            _captuedImages = new ConcurrentQueue<SoftwareBitmap>();
            _iamgeQueues = new ConcurrentQueue<SoftwareBitmap>();

            _framePool = Direct3D11CaptureFramePool.Create(CanvasDevice.GetSharedDevice(), DirectXPixelFormat.B8G8R8A8UIntNormalized, 2, item.Size);
            _framePool.FrameArrived += async (o, e) => 
            {
                using (var frame = _framePool.TryGetNextFrame())
                {
                    var softwareBitmap = await Windows.Graphics.Imaging.SoftwareBitmap.CreateCopyFromSurfaceAsync(frame.Surface, Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied);
                    _captuedImages.Enqueue(softwareBitmap);
                }
            };
            _seesion = _framePool.CreateCaptureSession(item);
            _seesion.StartCapture();
        }


    }
}
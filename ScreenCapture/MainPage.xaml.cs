using Microsoft.Graphics.Canvas;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics;
using Windows.Graphics.Capture;
using Windows.Media.Editing;
using Windows.Media.Transcoding;
using Windows.UI.Composition;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace ScreenCapture
{


    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        // Non-API related members.
        private ScreenCaptureComponent.CaptureHelper _captureHelper = new ScreenCaptureComponent.CaptureHelper();
        private MediaComposition _mediaComposition = new MediaComposition();
        public MainPage()
        {
            InitializeComponent();
        }

        public async Task StartCaptureAsync()
        {
            // The GraphicsCapturePicker follows the same pattern the
            // file pickers do.
            var picker = new GraphicsCapturePicker();
            GraphicsCaptureItem item = await picker.PickSingleItemAsync();

            // The item may be null if the user dismissed the
            // control without making a selection or hit Cancel.
            if (item != null)
            {
                StartCaptureInternal(item);
                //_captureHelper.StartCaptureInternal(TimeSpan.FromMilliseconds(20), _mediaComposition, item);
            }
        }

        private async void Button_ClickAsync(object sender, RoutedEventArgs e)
        {
            await StartCaptureAsync();
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await RenderCompositionToFile(_mediaComposition);
            }
#pragma warning disable CS0168 // Variable is declared but never used
            catch (Exception)
#pragma warning restore CS0168 // Variable is declared but never used
            {

            }
            
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            _seesion.Dispose();
            _framePool.Dispose();

            //_captureHelper.Stop();
        }

        private async Task RenderCompositionToFile(MediaComposition composition)
        {
            var picker = new Windows.Storage.Pickers.FileSavePicker();
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.VideosLibrary;
            picker.FileTypeChoices.Add("MP4 files", new List<string>() { ".mp4" });
            picker.SuggestedFileName = "RenderedComposition.mp4";

            Windows.Storage.StorageFile file = await picker.PickSaveFileAsync();
            if (file != null)
            {
                // Call RenderToFileAsync
                var saveOperation = composition.RenderToFileAsync(file, MediaTrimmingPreference.Precise);

                saveOperation.Progress = new AsyncOperationProgressHandler<TranscodeFailureReason, double>(async (info, progress) =>
                {
                    await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, new DispatchedHandler(() =>
                    {
                        System.Diagnostics.Debug.WriteLine(string.Format("Saving file... Progress: {0:F0}%", progress));
                    }));
                });
                saveOperation.Completed = new AsyncOperationWithProgressCompletedHandler<TranscodeFailureReason, double>(async (info, status) =>
                {
                    await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, new DispatchedHandler(() =>
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
                            // Update UI whether the operation succeeded or not
                        }

                    }));
                });
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("User cancelled the file selection");
            }
        }


        Direct3D11CaptureFramePool _framePool;
        GraphicsCaptureSession _seesion;
        CanvasDevice CanvasDevice = new CanvasDevice();
        void StartCaptureInternal(GraphicsCaptureItem item)
        {  
            _framePool = Direct3D11CaptureFramePool.Create(CanvasDevice, Windows.Graphics.DirectX.DirectXPixelFormat.B8G8R8A8UIntNormalized, 2, item.Size);
            _framePool.FrameArrived += async (o, e) => 
            {
                using (var frame = _framePool.TryGetNextFrame())
                {
                    var softwareBitmap = await Windows.Graphics.Imaging.SoftwareBitmap.CreateCopyFromSurfaceAsync(frame.Surface);
                    ProcessFrame(softwareBitmap);
                }
            };
            _seesion = _framePool.CreateCaptureSession(item);
            _seesion.StartCapture();
        }

        private void ProcessFrame(Windows.Graphics.Imaging.SoftwareBitmap softwareBitmap)
        {
            CanvasRenderTarget rendertarget;
            var pixelBuffer = _getPixelBuffer(softwareBitmap);
            using (CanvasBitmap canvas = CanvasBitmap.CreateFromBytes(CanvasDevice.GetSharedDevice(true), pixelBuffer.ToArray(), softwareBitmap.PixelWidth, softwareBitmap.PixelHeight, Windows.Graphics.DirectX.DirectXPixelFormat.B8G8R8A8UIntNormalized))
            {
                rendertarget = new CanvasRenderTarget(CanvasDevice.GetSharedDevice(true), canvas.SizeInPixels.Width, canvas.SizeInPixels.Height, 96);
                using (CanvasDrawingSession ds = rendertarget.CreateDrawingSession())
                {
                    ds.Clear(Windows.UI.Colors.Black);
                    ds.DrawImage(canvas);
                }
            }
            MediaClip clip = MediaClip.CreateFromSurface(rendertarget, TimeSpan.FromMilliseconds(20));
            _mediaComposition.Clips.Add(clip);
        }

        public unsafe System.Span<byte> _getPixelBuffer(Windows.Graphics.Imaging.SoftwareBitmap softwareBitmap)
        {
            uint capacity;
            using (Windows.Graphics.Imaging.BitmapBuffer buffer = softwareBitmap.LockBuffer(Windows.Graphics.Imaging.BitmapBufferAccessMode.Write))
            {
                using (var reference = buffer.CreateReference())
                {
                    byte* dataInBytes;

                    ((IMemoryBufferByteAccess)reference).GetBuffer(out dataInBytes, out capacity);
                    return new Span<byte>(dataInBytes, (int)capacity);
                }
            }
        }
        [ComImport]
        [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        unsafe interface IMemoryBufferByteAccess
        {
            void GetBuffer(out byte* buffer, out uint capacity);
        }
    }
}
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Composition;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Media.Editing;
using Windows.Media.Transcoding;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media.Imaging;

namespace ScreenCapture
{
    

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        // Capture API objects.
        private SizeInt32 _lastSize;
        private GraphicsCaptureItem _item;
        private Direct3D11CaptureFramePool _framePool;
        private GraphicsCaptureSession _session;

        // Non-API related members.
        private CanvasDevice _canvasDevice;
        private CompositionGraphicsDevice _compositionGraphicsDevice;
        private Compositor _compositor;
        private CompositionDrawingSurface _surface;
        private CanvasBitmap _currentFrame;
        private string _screenshotFilename = "test.png";

        public MainPage()
        {
            InitializeComponent();
            Setup();
        }

        private void Setup()
        {
            _canvasDevice = new CanvasDevice();

            //_compositionGraphicsDevice = CanvasComposition.CreateCompositionGraphicsDevice(
            //    Window.Current.Compositor,
            //    _canvasDevice);

            //_compositor = Window.Current.Compositor;

            //_surface = _compositionGraphicsDevice.CreateDrawingSurface(
            //    new Size(400, 400),
            //    DirectXPixelFormat.B8G8R8A8UIntNormalized,
            //    DirectXAlphaMode.Premultiplied);    // This is the only value that currently works with
            //                                        // the composition APIs.

            //var visual = _compositor.CreateSpriteVisual();
            //visual.RelativeSizeAdjustment = Vector2.One;
            //var brush = _compositor.CreateSurfaceBrush(_surface);
            //brush.HorizontalAlignmentRatio = 0.5f;
            //brush.VerticalAlignmentRatio = 0.5f;
            //brush.Stretch = CompositionStretch.Uniform;
            //visual.Brush = brush;
            //ElementCompositionPreview.SetElementChildVisual(this, visual);
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
            }
        }
        ScreenCaptureComponent.CaptureHelper captureHelper = new ScreenCaptureComponent.CaptureHelper();
        private void StartCaptureInternal(GraphicsCaptureItem item)
        {
           
            captureHelper.StartCaptureInternal(TimeSpan.FromMilliseconds(10),mediaComposition,item);
        }

        

        Windows.Media.Editing.MediaComposition mediaComposition = new Windows.Media.Editing.MediaComposition();
        DateTime _lastTime;
        //private void ProcessFrame(SoftwareBitmap softwareBitmap)
        //{
        //    CanvasRenderTarget rendertarget;
        //    var sharedDevice = CanvasDevice.GetSharedDevice();
        //    var pixelBuffer = _getPixelBuffer(softwareBitmap);
        //    using (CanvasBitmap canvas = CanvasBitmap.CreateFromBytes(sharedDevice, pixelBuffer.ToArray(),softwareBitmap.PixelWidth,softwareBitmap.PixelHeight,DirectXPixelFormat.B8G8R8A8UIntNormalized))
        //    {
        //        rendertarget = new CanvasRenderTarget(CanvasDevice.GetSharedDevice(), canvas.SizeInPixels.Width, canvas.SizeInPixels.Height, 96);
        //        using (CanvasDrawingSession ds = rendertarget.CreateDrawingSession())
        //        {
        //            ds.Clear(Colors.Black);
        //            ds.DrawImage(canvas);
        //        }
        //    }
        //    var now = DateTime.Now;
        //    var span = now - _lastTime;
        //    _lastTime = now;
        //    MediaClip clip = MediaClip.CreateFromSurface(rendertarget, span);
        //    mediaComposition.Clips.Add(clip);
        //}

        //public  System.Span<byte> _getPixelBuffer(SoftwareBitmap softwareBitmap)
        //{
        //    uint capacity;
        //    using (BitmapBuffer buffer = softwareBitmap.LockBuffer(BitmapBufferAccessMode.Write))
        //    {
        //        using (var reference = buffer.CreateReference())
        //        {
        //            byte* dataInBytes;
                    
        //            ((IMemoryBufferByteAccess)reference).GetBuffer(out dataInBytes, out capacity);
        //            return new Span<byte>(dataInBytes, (int)capacity);
        //        }
        //    }
        //}
        //private void ProcessFrame(Direct3D11CaptureFrame frame)
        //{
        //    // Resize and device-lost leverage the same function on the
        //    // Direct3D11CaptureFramePool. Refactoring it this way avoids
        //    // throwing in the catch block below (device creation could always
        //    // fail) along with ensuring that resize completes successfully and
        //    // isn’t vulnerable to device-lost.
        //    bool needsReset = false;
        //    bool recreateDevice = false;

        //    if ((frame.ContentSize.Width != _lastSize.Width) ||
        //        (frame.ContentSize.Height != _lastSize.Height))
        //    {
        //        needsReset = true;
        //        _lastSize = frame.ContentSize;
        //    }

        //    try
        //    {
        //        // Take the D3D11 surface and draw it into a  
        //        // Composition surface.

        //        // Convert our D3D11 surface into a Win2D object.
        //        CanvasBitmap canvasBitmap = CanvasBitmap.CreateFromDirect3D11Surface(
        //            _canvasDevice,
        //            frame.Surface);

        //        _currentFrame = canvasBitmap;

        //        // Helper that handles the drawing for us.
        //        FillSurfaceWithBitmap(canvasBitmap);
        //    }

        //    // This is the device-lost convention for Win2D.
        //    catch (Exception e) when (_canvasDevice.IsDeviceLost(e.HResult))
        //    {
        //        // We lost our graphics device. Recreate it and reset
        //        // our Direct3D11CaptureFramePool.  
        //        needsReset = true;
        //        recreateDevice = true;
        //    }

        //    if (needsReset)
        //    {
        //        ResetFramePool(frame.ContentSize, recreateDevice);
        //    }
        //}

        private void FillSurfaceWithBitmap(CanvasBitmap canvasBitmap)
        {
            CanvasComposition.Resize(_surface, canvasBitmap.Size);

            using (var session = CanvasComposition.CreateDrawingSession(_surface))
            {
                session.Clear(Colors.Transparent);
                session.DrawImage(canvasBitmap);
            }
        }

        private void ResetFramePool(SizeInt32 size, bool recreateDevice)
        {
            do
            {
                try
                {
                    if (recreateDevice)
                    {
                        _canvasDevice = new CanvasDevice();
                    }

                    _framePool.Recreate(
                        _canvasDevice,
                        DirectXPixelFormat.B8G8R8A8UIntNormalized,
                        2,
                        size);
                }
                // This is the device-lost convention for Win2D.
                catch (Exception e) when (_canvasDevice.IsDeviceLost(e.HResult))
                {
                    _canvasDevice = null;
                    recreateDevice = true;
                }
            } while (_canvasDevice == null);
        }

        private async void Button_ClickAsync(object sender, RoutedEventArgs e)
        {
            
            await StartCaptureAsync();
        }

        private async void ScreenshotButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            await SaveImageAsync(_screenshotFilename, _currentFrame);
        }

        private async Task SaveImageAsync(string filename, CanvasBitmap frame)
        {
            StorageFolder pictureFolder = KnownFolders.SavedPictures;

            StorageFile file = await pictureFolder.CreateFileAsync(
                filename,
                CreationCollisionOption.ReplaceExisting);

            using (var fileStream = await file.OpenAsync(FileAccessMode.ReadWrite))
            {
                await frame.SaveAsync(fileStream, CanvasBitmapFileFormat.Png, 1f);
            }
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            StorageFolder VideosLibrary = KnownFolders.VideosLibrary;

            StorageFile file = await VideosLibrary.CreateFileAsync(
                "test",
                CreationCollisionOption.ReplaceExisting);
            await RenderCompositionToFile(mediaComposition);
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            captureHelper.Stop();
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
    }
}
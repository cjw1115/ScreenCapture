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
using System.Linq;
using System.IO;
using Windows.UI.Popups;
using ScreenCapture.Service;

namespace ScreenCapture
{
    public sealed partial class MainPage : Page
    {
        //private ScreenCaptureService _screenCapture = new ScreenCaptureService();
        private ScreenCaptureNativeComponent.ScreenCaptureService _screenCapture = new ScreenCaptureNativeComponent.ScreenCaptureService();
        public MainPage()
        {
            InitializeComponent();
        }

        private async void _btnStartClickAsync(object sender, RoutedEventArgs e)
        {
            await _screenCapture.StartCaptureAsync();
        }

        private void _btnStopClick(object sender, RoutedEventArgs e)
        {
            _screenCapture.StopCapture();
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

                await _screenCapture.WaitForImageRenderring();

                //if (!await _screenCapture.VerifyDuration())
                //{
                //    return;
                //}
                await _screenCapture.SetupMediaComposition();
                await _screenCapture.RenderCompositionToFile(file,progressBar);
            }
            catch (Exception ex)
            {
                progressBar.Foreground = new SolidColorBrush(Colors.Red);
                await new MessageDialog(ex.Message).ShowAsync();
            }

        }
    }
}
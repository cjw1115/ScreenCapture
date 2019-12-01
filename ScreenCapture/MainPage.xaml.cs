﻿using System;
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

using Native = ScreenCaptureNativeComponent;
using Managed = ScreenCapture.Service;
using ScreenCapture.Helper;
using ScreenCapture.Services;

namespace ScreenCapture
{
    public sealed partial class MainPage : Page
    {
        private MediaComposition _mediaComposition = new MediaComposition();
        private UINotificationService _notificationService = new UINotificationService();

        private bool _isNativeMode = false;
        private ScreenCaptureNativeComponent.IScreenCaptureService _screenCapture = new Managed.ScreenCaptureService();


        private readonly string CAPTURE_AUIDO_FILE = "loopback.wav";
        private StorageFile _audioFile;
        private AudioCaptureNativeComponent.Capture _audioCapture = new AudioCaptureNativeComponent.Capture(AudioCaptureNativeComponent.RoleMode.Loopback);

        public MainPage()
        {
            InitializeComponent();
            this.Loaded += MainPage_Loaded;
        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            var (re, msg) = await PermissionHelper.RequestMicrophonePermission();
            if(!re)
            {
                await _notificationService.Notify(msg);
            }
        }

        private async void _btnStartClickAsync(object sender, RoutedEventArgs e)
        {
            _audioFile = await Windows.Storage.ApplicationData.Current.LocalCacheFolder.CreateFileAsync(CAPTURE_AUIDO_FILE, CreationCollisionOption.ReplaceExisting);
            await _screenCapture.StartCaptureAsync();
            await _audioCapture.Start(_audioFile);
        }

        private void _btnStopClick(object sender, RoutedEventArgs e)
        {
            _screenCapture.StopCapture();
            _audioCapture.Stop();
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
                await _screenCapture.SetupMediaComposition(_mediaComposition);
                _mediaComposition.BackgroundAudioTracks.Add(await BackgroundAudioTrack.CreateFromFileAsync(_audioFile));
                await _screenCapture.RenderCompositionToFile(file,progressBar);
            }
            catch (Exception ex)
            {
                progressBar.Foreground = new SolidColorBrush(Colors.Red);
                await new MessageDialog(ex.Message).ShowAsync();
            }
        }

        
        private void cbMode_Checked(object sender, RoutedEventArgs e)
        {
            _isNativeMode = true;
            _screenCapture = new Native.ScreenCaptureService();
        }

        private void cbMode_Unchecked(object sender, RoutedEventArgs e)
        {
            _isNativeMode = false;
            _screenCapture = new Managed.ScreenCaptureService();
        }
    }
}
using CQ.Common.Helpers;
using ScreenCapture.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Media.Editing;
using Windows.Media.MediaProperties;
using Windows.Media.Transcoding;
using Windows.Storage;

namespace ScreenCapture.ViewModel
{
    public class MainViewModel: BindableBase
    {
        private MediaComposition _mediaComposition = new MediaComposition();
        private UINotificationService _notificationService = DependencyManager.Instance.ResolveType<UINotificationService>();

        private bool _isNativeMode = false;
        private ScreenCaptureNativeComponent.IScreenCaptureService _screenCapture = DependencyManager.Instance.ResolveType<ScreenCaptureService>();


        private readonly string CAPTURE_AUIDO_LOOPBACK_FILE = "loopback.wav";
        private StorageFile _audioLoopbackFile;
        private AudioCaptureNativeComponent.Capture _loopbackCapture = new AudioCaptureNativeComponent.Capture(AudioCaptureNativeComponent.RoleMode.Loopback);
        private readonly string CAPTURE_AUIDO_VOICE_FILE = "voice.wav";
        private StorageFile _audioVoiceFile;
        private AudioCaptureNativeComponent.Capture _voiceCapture = new AudioCaptureNativeComponent.Capture(AudioCaptureNativeComponent.RoleMode.Capture);

        public ProgressViewModel ProgressVM { get; } = new ProgressViewModel { Status = ProgressStatus.Progressing, Progress = 0 };

        public MainViewModel()
        {
        }

        public async void StartCaptureAsync()
        {
            _audioLoopbackFile = await Windows.Storage.ApplicationData.Current.LocalCacheFolder.CreateFileAsync(CAPTURE_AUIDO_LOOPBACK_FILE, CreationCollisionOption.ReplaceExisting);
            _audioVoiceFile = await Windows.Storage.ApplicationData.Current.LocalCacheFolder.CreateFileAsync(CAPTURE_AUIDO_VOICE_FILE, CreationCollisionOption.ReplaceExisting);
            //For syncing the video frame and background audio frame.
            //Need start the scrren capture service at first,then start the loopback service, the voice service should be last one.
            //Start scrren capture service will take almost 1s. but start loopback service takes littile silliseconds. 
            await _screenCapture.StartCaptureAsync();
            await _loopbackCapture.Start(_audioLoopbackFile);
            await _voiceCapture.Start(_audioVoiceFile);
        }

        public void StopCapture()
        {
            _screenCapture.StopCapture();
            _loopbackCapture.Stop();
            _voiceCapture.Stop();
        }

        public async void SaveToFile()
        {
            try
            {
                ProgressVM.Progress = 0;
                ProgressVM.Status = ProgressStatus.Progressing;

                var picker = new Windows.Storage.Pickers.FileSavePicker();
                picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.VideosLibrary;
                picker.FileTypeChoices.Add("MP4 files", new List<string>() { ".mp4" });
                picker.SuggestedFileName = "RenderedComposition.mp4";

                Windows.Storage.StorageFile videoFile = await picker.PickSaveFileAsync();

                await _screenCapture.WaitForImageRenderring();
                await _screenCapture.SetupMediaComposition(_mediaComposition);
                _mediaComposition.BackgroundAudioTracks.Add(await BackgroundAudioTrack.CreateFromFileAsync(_audioVoiceFile));
                _mediaComposition.BackgroundAudioTracks.Add(await BackgroundAudioTrack.CreateFromFileAsync(_audioLoopbackFile));

                RenderCompositionToFile(videoFile);
            }
            catch (Exception ex)
            {
                ProgressVM.Status = ProgressStatus.Failed;
                await _notificationService.Notify(ex.Message);
            }
        }

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        private void RenderCompositionToFile(Windows.Storage.StorageFile file)
        {
            var mp4Profile = MediaEncodingProfile.CreateMp4(VideoEncodingQuality.HD1080p);
            var saveOperation = _mediaComposition.RenderToFileAsync(file, MediaTrimmingPreference.Fast, mp4Profile);
            saveOperation.Progress = new AsyncOperationProgressHandler<TranscodeFailureReason, double>((info, progress) =>
            {
                Helper.UIUtility.RunAsync(() =>
                {
                    ProgressVM.Progress = progress;
                });
            });
            saveOperation.Completed = new AsyncOperationWithProgressCompletedHandler<TranscodeFailureReason, double>((info, status) =>
            {
                var results = info.GetResults();
                if (results != TranscodeFailureReason.None || status != AsyncStatus.Completed)
                {
                    Helper.UIUtility.RunAsync(() =>
                    {
                        ProgressVM.Status = ProgressStatus.Failed;
                    });
                }
                else
                {
                    Helper.UIUtility.RunAsync(() =>
                    {
                        ProgressVM.Status = ProgressStatus.Success;
                    });
                }
            });
        }
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
    }
}

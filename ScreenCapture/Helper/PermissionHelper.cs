using System;
using System.Threading.Tasks;
using Windows.Media.Capture;

namespace ScreenCapture.Helper
{
    public class PermissionHelper
    {
        private static int NoCaptureDevicesHResult = -1072845856;

        public async static Task<Tuple<bool,string>> RequestMicrophonePermission()
        {
            MediaCapture capture = null;
            try
            {
                // Request access to the audio capture device.
                MediaCaptureInitializationSettings settings = new MediaCaptureInitializationSettings();
                settings.StreamingCaptureMode = StreamingCaptureMode.Audio;
                settings.MediaCategory = MediaCategory.Speech;
                capture = new MediaCapture();
                
                await capture.InitializeAsync(settings);
            }
            catch (TypeLoadException)
            {
                return Tuple.Create(false, UIUtility.GetUIString("MEDIA_COMPONENT_UNAVALIABLE"));
            }
            catch (UnauthorizedAccessException)
            {
                // Thrown when permission to use the audio capture device is denied.
                // If this occurs, show an error or disable recognition functionality.
                return Tuple.Create(false, UIUtility.GetUIString("UNAUTHORIZED_MSG"));
            }
            catch (Exception exception)
            {
                // Thrown when an audio capture device is not present.
                if (exception.HResult == NoCaptureDevicesHResult)
                {
                    return Tuple.Create(false, UIUtility.GetUIString("CAPTURE_DEVICE_UNAVALIABLE")); ;
                }
                else
                {
                    throw;
                }
            }
            finally
            {
                capture?.Dispose();
            }
            return Tuple.Create(true,string.Empty);
        }
    }
}

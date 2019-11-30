using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.UI.Core;

namespace ScreenCapture.Helper
{
    public class UIUtility
    {
        private static CoreDispatcher _dispatcher;
        public static void SetDispatcher(CoreDispatcher coreDispatcher)
        {
            _dispatcher = coreDispatcher;
        }

        public static async Task RunAsync(Action action)
        {
            await _dispatcher?.RunAsync(CoreDispatcherPriority.Normal, () => { action.Invoke(); });
        }

        public static void SetCurrentViewMinSize(Size preferredSize)
        {
            Windows.UI.ViewManagement.ApplicationView.PreferredLaunchWindowingMode = Windows.UI.ViewManagement.ApplicationViewWindowingMode.PreferredLaunchViewSize;
            Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().SetPreferredMinSize(preferredSize);
        }

        private static ResourceLoader _resourceLoader = null;
        public static string GetUIString(string name)
        {
            if (_resourceLoader == null)
            {
                _resourceLoader = new Windows.ApplicationModel.Resources.ResourceLoader();
            }
            return _resourceLoader.GetString(name);
        }
    }
}

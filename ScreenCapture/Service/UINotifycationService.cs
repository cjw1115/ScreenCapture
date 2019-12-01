using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Popups;

namespace ScreenCapture.Service
{
    public class UINotificationService
    {
        public UINotificationService()
        {
        }

        public async Task Notify(string message)
        {
            await new MessageDialog(message).ShowAsync();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace ScreenCapture.Converter
{
    public class ProgressForegroundConverter:IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var status = (ViewModel.ProgressStatus)value;
            switch (status)
            {
                case ViewModel.ProgressStatus.Progressing:
                    return new SolidColorBrush(Colors.Blue);
                case ViewModel.ProgressStatus.Success:
                    return new SolidColorBrush(Colors.Green);
                case ViewModel.ProgressStatus.Failed:
                    return new SolidColorBrush(Colors.Red);
                default:
                    return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;

namespace ScreenCapture.Model
{
    public class CapturedVideoFrame
    {
        public int Index { get; private set; }
        public SoftwareBitmap Bitmap { get; private set; }

        public CapturedVideoFrame(int index, SoftwareBitmap bitmap)
        {
            Index = index;
            Bitmap = bitmap;
        }
    }
}

using CQ.Common.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenCapture.ViewModel
{
    public enum ProgressStatus
    {
        Progressing,
        Success,
        Failed
    }

    public class ProgressViewModel:BindableBase
    {
        
        private double _progress;
        public double Progress
        {
            get { return _progress; }
            set { SetProperty(ref _progress, value); }
        }

        private ProgressStatus _status;
        public ProgressStatus Status
        {
            get { return _status; }
            set { SetProperty(ref _status, value); }
        }
    }
}

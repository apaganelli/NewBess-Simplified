using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Microsoft.Kinect;

namespace NewBess
{
    public class ApplicationViewModel: ObservableObject
    {
        private ICommand _changePageCommand;

        private IPageViewModel _previousPageViewModel;
        private IPageViewModel _currentPageViewModel;
        private List<IPageViewModel> _pageViewModels;

        #region Global variables

        public int NumFramesTest_DTW = 30;                          // Num of frames for DTW checking during test.
        public int TestTime = 20;                                   // sets the time in seconds of each test
        public int CalibrationTime = 3;                             // sets the time in seconds of calibration process
        public int DelayTime = 0;

        public double JointAxisPrecision = 0.07;                    // sets the precision in meters of the joint precision
        public double AnglePrecision = 15;                          // sets the angle precision in degrees of trunk position (head cal, spine_base, head test).

        public int participantId = 0;                               // Selected participant id.
        public string participantName = "";                         // Selected participant Name.
        public string condition = "A";                              // Test experimental condition A, B, C, D. 

        public bool EnableWBB = true;                               // Configurable flag to enable or not WBB control.
        public bool SaveJoints = true;

        public static int MaxFramesZeroCalibration = 150;
        public static int MaxFramesReference = 30;                     // Number of frames - Pose calibration. Based on 30 frames per second.
#endregion
        /// <summary>
        /// The constructor activate the application default page and selects it as the active page.
        /// </summary>
        public ApplicationViewModel()
        {
        }

        /// <summary>
        /// Interface command to execute the change of pages.
        /// </summary>
        public ICommand ChangePageCommand
        {
            get
            {
                return _changePageCommand;
            }

            set
            {
                if (_changePageCommand == null)
                {
                    _changePageCommand = new RelayCommand(
                        p => ChangeViewModel((IPageViewModel)p),
                        p => p is IPageViewModel);
                }
             }
        }

        /// <summary>
        /// Gets/sets the current view model page.
        /// </summary>
        public IPageViewModel CurrentPageViewModel
        {
            get
            {
                return _currentPageViewModel;
            }
            set
            {
                if (_currentPageViewModel is PoseCalibrationViewModel)
                {
                    _currentPageViewModel = value;
                    OnPropertyChanged("CurrentPageViewModel");
                } else if (_currentPageViewModel != value)
                {
                    _currentPageViewModel = value;
                    OnPropertyChanged("CurrentPageViewModel");
                }
            }
        }

        public IPageViewModel PreviousPageViewModel
        {
            get { return _previousPageViewModel; }
            set
            {
                if(_previousPageViewModel != value)
                {
                    _previousPageViewModel = value;
                    OnPropertyChanged("PreviousPageViewModel");
                }
            }
        }

        /// <summary>
        /// Gets the list of all view model pages that had been instantiated.
        /// </summary>
        public List<IPageViewModel> PageViewModels
        {
            get
            {
                if (_pageViewModels == null)
                    _pageViewModels = new List<IPageViewModel>();

                return _pageViewModels;
            }
        }

        /// <summary>
        /// Changes the active selected view model. If it is a new view model that had not been activated before,
        /// adds it to the list of view model pages.
        /// </summary>
        /// <param name="viewModel"></param>
        public void ChangeViewModel(IPageViewModel viewModel)
        {
            if (!PageViewModels.Contains(viewModel))
                PageViewModels.Add(viewModel);

            PreviousPageViewModel = CurrentPageViewModel;
            CurrentPageViewModel = PageViewModels.FirstOrDefault(vm => vm == viewModel);
        }
    }
}

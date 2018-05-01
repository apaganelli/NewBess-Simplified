using System;
using System.Timers;
using System.Windows.Media;

namespace NewBess
{
    class PoseCalibrationViewModel : CalibrationTest
    {

#region Constructors
        public PoseCalibrationViewModel(ApplicationViewModel app, string name)
        {
            _app = app;
            Name = name;            // Module name: DS = double stance; SS = single stance; TS = tandem stance
            LoadCalibrationViewModel();
            StatusText = "Ready";
        }
#endregion

        override protected void infoUpdateTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            // Start timer of recording double stance joint positions for reference.
            if (_startPoseCalibration && _kinectBV.Delay < 0)
            {
                // PoseCalibrationCounter in mmsec - it is updated within kinect body view, frame received event.

                int CalibrationElapsedSeconds = (DateTime.Now - StartCalibrationTime).Seconds;
                if (CalibrationElapsedSeconds >= _app.CalibrationTime)
                {
                    PoseCalibrationDone = true;
                    _startPoseCalibration = false;
                    _kinectBV.IsCalibrating = false;
                    StatusText = "Finished timer for recording stance pose";
                } else
                {
                    StatusText = "Start timer for recording stance pose " + CalibrationElapsedSeconds; 
                }
            }

            // Executing test?
            if(_kinectBV.IsTesting && _kinectBV.Delay < 0)
            {
                int seconds = (DateTime.Now - StartTestTime).Seconds;
                this.TestTime = "Test time in seconds: " + seconds.ToString("F0");

                // Finished?
                if(seconds >= _app.TestTime)
                {
                    _kinectBV.IsTesting = false;
                    _kinectBV.TestStopWatch.Stop();
                    FinishedTest = true;
                    StatusText = "Test finished with " + TotalErrors + " errors.";
                    LogTestLines.Add("Finished:" + TotalErrors);
                }
            }

            // Work with WBB?
            if (_app.EnableWBB)
            {
                UpdateStatusWii(e);
            }            
        }

        /// <summary>
        /// Start Pose Calibration storing key joints positions and kinect video clip.
        /// </summary>
        override protected void StartPoseCalibration()
        {
            PoseCalibrationDone = false;
            LogJoints3DCal.Clear();
            LogCalLines.Clear();
            ResetMessages();

            LogJoints3DCal.Add("#Start Joints3D Calibration");
            LogCalLines.Add("#Start calibration");

            if (_app.EnableWBB)
            {
                LogCalLines.Add("CoP:0:" + CalculatedCoPX.ToString("N5") + ":" + CalculatedCoPY.ToString("N5"));
            }

            _startPoseCalibration = true;                       // flag that signals timer to count calibration seconds.
            _kinectBV.Delay = _app.DelayTime * 30;              // Retard start of the calibration.
            _kinectBV.IsCalibrating = true;                     // flag to kinect body view
        }

        /// <summary>
        /// Action to start test analyzing CoP and CoM position and posture.
        /// </summary>
        override protected void StartTest()
        {
            // Be sure to only stores the current series of the test.
            LogJoints3D.Clear();
            LogTestLines.Clear();
            LogTestLines.Add("#Start test");

            if (_app.SaveJoints) LogJoints2D.Clear();

            if (_app.EnableWBB)
            {
                LogTestLines.Add("CoP:0:" + CalculatedCoPX.ToString("N5") + ":" + CalculatedCoPY.ToString("N5"));
            }

            TotalErrors = 0;
            LeftFoot = LeftHand = RightFoot = RightHand = "";

            LogJoints3D.Add("#Start Joints3D Test");
            _kinectBV.Delay = _app.DelayTime * 30;
            _kinectBV.IsTesting = true;
            FinishedTest = false;
        }

        public void ResetMessages()
        {
            // Clear any residual message on screen about joint status.
            LeftFoot = "";
            RightFoot = "";
            LeftHand = "";
            RightHand = "";
            TrunkSway = "";
            StatusText = "";
        }

        #region Properties
        // Status of segments.
        string _leftFoot;
        string _rightFoot;
        string _leftHand;
        string _rightHand;
        string _trunkSway;

        public string TrunkSway
        {
            get { return _trunkSway; }
            set
            {
                if (_trunkSway != value)
                {
                    _trunkSway = value;
                    OnPropertyChanged("TrunkSway");
                }
            }
        }

        public string LeftHand {
            get { return _leftHand; }
            set
            {
                if (_leftHand != value)
                {
                    _leftHand = value;
                    OnPropertyChanged("LeftHand");
                }
            }
        }

        public string RightHand
        {
            get { return _rightHand; }
            set
            {
                if (_rightHand != value)
                {
                    _rightHand = value;
                    OnPropertyChanged("RightHand");
                }
            }
        }

        public string LeftFoot
        {
            get { return _leftFoot; }
            set
            {
                if (_leftFoot != value)
                {
                    _leftFoot = value;
                    OnPropertyChanged("LeftFoot");
                }
            }
        }

        public string RightFoot
        {
            get { return _rightFoot; }
            set
            {
                if (_rightFoot != value)
                {
                    _rightFoot = value;
                    OnPropertyChanged("RightFoot");
                }
            }
        }

        public int TotalErrors { get; set; }

        public ImageSource ImageSource
        {
            get { return _kinectBV.ImageSource; }
        }
        #endregion
    }
}

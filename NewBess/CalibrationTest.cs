using System;
using System.Collections.Generic;
using System.Globalization;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WiimoteLib;


namespace NewBess
{
    public abstract class CalibrationTest : ObservableObject, IPageViewModel
    {
        protected Wiimote wiiDevice = new Wiimote();

        protected ApplicationViewModel _app;
        protected KinectBodyView _kinectBV;

        protected string _logFilename;

        /// <summary>
        /// Store the logs (text) of the system
        /// </summary>
        public List<string> LogCalLines;                    // Stores calibration process logs
        public List<string> LogTestLines;                   // Stores test process logs
        public List<string> LogJoints2D;                    // Stores mapped joint positions processed to be displayed on the screen
        public List<string> LogCalZeroLines;                // Stores weight calibration process logs
        public List<string> LogJoints3D;                    // Stores raw joint positions in camera space during tests
        public List<string> LogJoints3DCal;                 // Stores raw joint positions in camera space during calibration
 
        public DateTime StartCalibrationTime;

        #region WBB variable attributes
        protected float _kgWeight;
        protected float _kgTopLeft;
        protected float _kgTopRight;
        protected float _kgBottomLeft;
        protected float _kgBottomRight;

        protected float _owWeight;
        protected float _owTopLeft;
        protected float _owTopRight;
        protected float _owBottomLeft;
        protected float _owBottomRight;

        protected float _CoGX;
        protected float _CoGY;

        protected float _calculatedCoPX;
        protected float _calculatedCoPY;

        protected float _zeroCalWeight;
        protected string _statusText;
        protected string _testTime;
        #endregion

        protected bool _canConnectWBB = true;
        protected bool _startPoseCalibration = false;
        protected bool _poseCalibrationDone = false;
        protected bool _zeroCalibrationDone = false;
        protected bool _finishedTest = false;
        private bool _savedata = false;

        protected bool _doZeroCalibration = false;
        protected int i = 0;
        protected float zeroWeight = 0;

        public DateTime StartTestTime;

        private DrawingGroup drawingGroup;                      // for drawing chart of CoP position
        private DrawingImage imageSource;                       // chart of CoP position internal object

        private ImageSource picture;                            // show picture of the position.

        #region ICommnad attributes
        protected ICommand _connectWBBCommand;
        protected ICommand _zeroCalCommand;
        protected ICommand _cancelCommand;
        protected ICommand _startPoseCalibrationCommand;
        protected ICommand _startTestCommand;
        protected ICommand _saveCommand;
        #endregion

        /// <summary>
        /// Timer to read WBB info. Interval in ms, enabled if the event should be triggered.
        /// </summary>
        public System.Timers.Timer infoUpdateTimer = new System.Timers.Timer() { Interval = 30, Enabled = false };

        /// <summary>
        /// Base class constructor.
        /// </summary>
        public CalibrationTest()
        {
            LogCalLines = new List<string>();
            LogTestLines = new List<string>();
            LogCalZeroLines = new List<string>();
            LogJoints3D = new List<string>();
            LogJoints3DCal = new List<string>();
            drawingGroup = new DrawingGroup();
            imageSource = new DrawingImage(drawingGroup);
        }

        public void LoadCalibrationViewModel()
        {
            // Setup a timer which controls the rate at which updates are processed.
            infoUpdateTimer.Elapsed += new ElapsedEventHandler(infoUpdateTimer_Elapsed);

            _kinectBV = new KinectBodyView(_app, Name);

            // If WBB is not enabled, connect button is not available, then we need to start timer events here for timer controls work properly.
            if (!_app.EnableWBB)
            {
                infoUpdateTimer.Enabled = true;
            }

            if (_app.SaveJoints && LogJoints2D == null)
            {
                LogJoints2D = new List<string>();
            }

            _canConnectWBB = true;
            ZeroCalibrationDone = true;
        }

        public void ResetLogs()
        {
            LogJoints3D.Clear();
            LogJoints3DCal.Clear();
            LogCalLines.Clear();
            LogTestLines.Clear();    

            if(LogJoints2D != null)
            {
                LogJoints2D.Clear();
            }
        }

        #region Commands
        public ICommand ConnectWBBCommand
        {
            get
            {
                if (_connectWBBCommand == null)
                {
                    _connectWBBCommand = new RelayCommand(param => ConnectWiiBalanceBoard(), param => _app.EnableWBB && _canConnectWBB);
                }
                return _connectWBBCommand;
            }
        }


        public ICommand ZeroCalCommand
        {
            get
            {
                if (_zeroCalCommand == null)
                {
                    _zeroCalCommand = new RelayCommand(param => ZeroCalibration(), param => _app.EnableWBB);
                }
                return _zeroCalCommand;
            }
        }


        public ICommand StartPoseCalibrationCommand
        {
            get
            {
                if (_startPoseCalibrationCommand == null)
                {
                    _startPoseCalibrationCommand = new RelayCommand(param => StartPoseCalibration(), param => ZeroCalibrationDone || !_app.EnableWBB);
                }

                return _startPoseCalibrationCommand;
            }
        }

        public ICommand StartTestCommand
        {
            get
            {
                if (_startTestCommand == null)
                {
                    _startTestCommand = new RelayCommand(param => StartTest(), param => PoseCalibrationDone);
                }

                return _startTestCommand;
            }
        }

        public ICommand SaveCommand
        {
            get
            {
                if (_saveCommand == null)
                {
                    _saveCommand = new RelayCommand(param => Save(), param => FinishedTest);
                }

                return _saveCommand;
            }
        }

        public ICommand CancelCommand
        {
            get
            {
                if (_cancelCommand == null)
                {
                    _cancelCommand = new RelayCommand(param => Cancel());
                }

                return _cancelCommand;
            }
        }
        #endregion

        private void ConnectWiiBalanceBoard()
        {
            try
            {
                // Find all connected Wii devices.
                var deviceCollection = new WiimoteCollection();
                deviceCollection.FindAllWiimotes();

                for (int i = 0; i < deviceCollection.Count; i++)
                {
                    wiiDevice = deviceCollection[i];

                    // Setup update handlers.
                    wiiDevice.WiimoteChanged += wiiDevice_WiimoteChanged;
                    wiiDevice.WiimoteExtensionChanged += wiiDevice_WiimoteExtensionChanged;

                    // Connect and send a request to verify it worked.
                    wiiDevice.Connect();
                    wiiDevice.SetReportType(InputReport.IRAccel, false); // FALSE = DEVICE ONLY SENDS UPDATES WHEN VALUES CHANGE!
                    wiiDevice.SetLEDs(true, false, false, false);

                    ZeroCalibration();

                    // Enable processing of updates.
                    infoUpdateTimer.Enabled = true;

                    // Prevent connect being pressed more than once.
                    _canConnectWBB = false;
                    break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Sets flag to calibabre zero weight load on the WBB.
        /// </summary>
        private void ZeroCalibration()
        {
            i = 0;
            LogCalZeroLines.Clear();
            LogCalZeroLines.Add("#Start ZeroCalibration");
            zeroWeight = 0;
            zcBL = zcBR = zcTL = zcTR = 0;                  // Gets zero cal mean values for each load cell.
            _doZeroCalibration = true;
        }

        /// <summary>
        /// Cancel button pressed.
        /// </summary>
        private void Cancel()
        {
            if(Name == "TS" && !SavedData)
            {
                Save();
            }

            LogCalLines.Clear();
            LogCalZeroLines.Clear();
            if(LogJoints2D != null) LogJoints2D.Clear();
            LogJoints3D.Clear();

            // infoUpdateTimer.Enabled = false;
            // wiiDevice.Disconnect();
            // _canConnectWBB = true;

            Name = "DS";
            _app.ChangeViewModel(_app.PageViewModels.Find(r => r.Name == "TestsViewModel"));
        }

        protected abstract void StartPoseCalibration();
        protected abstract void StartTest();

        // Save test results in xML file.
        /// <summary>
        /// Save all calibration and test information in a file named:  Test_ID-Participant_Name-yyyy-MM-dd-HH-mm-ss.txt
        /// Test_ID = DS / double stance, SS / single stance, TS / tandem stance 
        /// </summary>
        public void Save()
        {
            _logFilename = Name + "-" + _app.participantName + "-" + _app.condition + "-" + DateTime.Now.ToString("yyyy-MM-dd-HH_mm_ss") + ".txt";
            System.IO.File.WriteAllLines("Data\\" + _logFilename, LogCalZeroLines);
            System.IO.File.AppendAllLines("Data\\" + _logFilename, LogCalLines);
            System.IO.File.AppendAllLines("Data\\" + _logFilename, LogTestLines);
            System.IO.File.AppendAllLines("Data\\" + _logFilename, LogJoints3DCal);
            System.IO.File.AppendAllLines("Data\\" + _logFilename, LogJoints3D);

            if (_app.SaveJoints)
            {
                System.IO.File.WriteAllLines("Data\\Joints_" + _logFilename, LogJoints2D);
            }

            SavedData = true;
            StatusText = "Information saved to: Data\\" + _logFilename;
        }
         
        // Calibration variables.
        float calW_BL0, calW_BR0, calW_TL0, calW_TR0;
        float calW_BL17, calW_BR17, calW_TL17, calW_TR17, calW_BL34, calW_BR34, calW_TL34, calW_TR34;

        float zcBL, zcBR, zcTL, zcTR;
        float KgCalculatedCoPx, KgCalculatedCoPy;
        int updateShowImage = 0;                                        // Control delays for updating image with CoP.

        protected void UpdateStatusWii(ElapsedEventArgs e)
        {
            // Get raw values from WBB load sensors
            float rvBL = wiiDevice.WiimoteState.BalanceBoardState.SensorValuesRaw.BottomLeft;
            float rvBR = wiiDevice.WiimoteState.BalanceBoardState.SensorValuesRaw.BottomRight;
            float rvTL = wiiDevice.WiimoteState.BalanceBoardState.SensorValuesRaw.TopLeft;
            float rvTR = wiiDevice.WiimoteState.BalanceBoardState.SensorValuesRaw.TopRight;

            OWBottomLeft = rvBL;
            OWBottomRight = rvBR;
            OWTopLeft = rvTL;
            OWTopRight = rvTR;

            // Get the current sensor values in KG.
            KgTotalWeight = wiiDevice.WiimoteState.BalanceBoardState.WeightKg;
            KgTopLeft = wiiDevice.WiimoteState.BalanceBoardState.SensorValuesKg.TopLeft;
            KgTopRight = wiiDevice.WiimoteState.BalanceBoardState.SensorValuesKg.TopRight;
            KgBottomLeft = wiiDevice.WiimoteState.BalanceBoardState.SensorValuesKg.BottomLeft;
            KgBottomRight = wiiDevice.WiimoteState.BalanceBoardState.SensorValuesKg.BottomRight;


            // Correct reads discounting unloaded values of each cell.
            rvBL -= zcBL;
            rvBR -= zcBR;
            rvTL -= zcTL;
            rvTR -= zcTR;

            if(rvBL < calW_BL17)
            {
                rvBL = (17 * rvBL) / (calW_BL17 - calW_BL0);
            } else 
            {
                rvBL = (34 * rvBL) / (calW_BL34 - calW_BL0); 
            }

            if (rvBR < calW_BR17)
            {
                rvBR = (17 * rvBR) / (calW_BR17 - calW_BR0);
            }
            else
            {
                rvBR = (34 * rvBR) / (calW_BR34 - calW_BR0);
            }

            if (rvTL < calW_TL17)
            {
                rvTL = (17 * rvTL) / (calW_TL17 - calW_TL0);
            }
            else
            {
                rvTL = (34 * rvTL) / (calW_TL34 - calW_TL0);
            }

            if (rvTR < calW_TR17)
            {
                rvTR = (17 * rvTR) / (calW_TR17 - calW_TR0);
            }
            else
            {
                rvTR = (34 * rvTR) / (calW_TR34 - calW_TR0);
            }

            OWTotalWeight = rvBL + rvBR + rvTR + rvTL;

            // Calculate CoP using formula
            // CoPx = (L/2) * ((TR + BR) - (TL + BL)) / TR + BR + TL + BL
            // CoPy = (W/2) * ((TL + TR) - (BL + BR)) / TR + BR + TL + BL
            // W = 238 mm and L = 433mm
            // Bartlett HL, Ting LH, Bingham JT. Accuracy of force and center of pressure measures of the Wii Balance Board. Gait Posture. 2014; 39:224–8.
            CalculatedCoPX = (433/2) * (((rvTR + rvBR) - (rvTL + rvBL)) / (rvBL + rvBR + rvTL + rvTR));
            CalculatedCoPY = (238/2) * (((rvTR + rvTL) - (rvBR + rvBL)) / (rvBL + rvBR + rvTL + rvTR));

            KgCalculatedCoPx = (433 / 2) * (((KgTopRight + KgBottomRight) - (KgTopLeft + KgBottomLeft)) / (KgBottomLeft + KgBottomRight + KgTopLeft + KgTopRight));
            KgCalculatedCoPy = (238 / 2) * (((KgTopRight + KgTopLeft) - (KgBottomRight + KgBottomLeft)) / (KgBottomLeft + KgBottomRight + KgTopLeft + KgTopRight));

            // Do not refresh image every tick.
            if (updateShowImage == 0)
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                { ShowWBBImage(); }));

                updateShowImage = 4;
            } else
            {
                updateShowImage--;
            }

            // Calibrate zero weight when the board is unloaded.
            if (_doZeroCalibration && i < ApplicationViewModel.MaxFramesZeroCalibration)
            {
                i++;                
                LogCalZeroLines.Add("RL-0Cal:" + OWBottomLeft.ToString("F0") + ":" + OWBottomRight.ToString("F0") + ":" + OWTopLeft.ToString("F0") + ":" + OWTopRight.ToString("F0"));
                zeroWeight += KgTotalWeight;
                zcBL += OWBottomLeft;
                zcBR += OWBottomRight;
                zcTL += OWTopLeft;
                zcTR += OWTopRight;
                StatusText = "Calibrating Zero Weight.";
            }
            else
            {
                if (_doZeroCalibration)
                {
                    ZeroCalibrationDone = true;
                    zeroWeight = zeroWeight / ApplicationViewModel.MaxFramesZeroCalibration;
                    zcBL = zcBL / ApplicationViewModel.MaxFramesZeroCalibration;
                    zcBR = zcBR / ApplicationViewModel.MaxFramesZeroCalibration;
                    zcTL = zcTL / ApplicationViewModel.MaxFramesZeroCalibration;
                    zcTR = zcTR / ApplicationViewModel.MaxFramesZeroCalibration;

                    // Get fabricant calibration values for 0kg, 17kg and 34kg over each sensor.
                    calW_BL0 = wiiDevice.WiimoteState.BalanceBoardState.CalibrationInfo.Kg0.BottomLeft;
                    calW_BR0 = wiiDevice.WiimoteState.BalanceBoardState.CalibrationInfo.Kg0.BottomRight;
                    calW_TL0 = wiiDevice.WiimoteState.BalanceBoardState.CalibrationInfo.Kg0.TopLeft;
                    calW_TR0 = wiiDevice.WiimoteState.BalanceBoardState.CalibrationInfo.Kg0.TopRight;

                    calW_BL17 = wiiDevice.WiimoteState.BalanceBoardState.CalibrationInfo.Kg17.BottomLeft;
                    calW_BR17 = wiiDevice.WiimoteState.BalanceBoardState.CalibrationInfo.Kg17.BottomRight;
                    calW_TL17 = wiiDevice.WiimoteState.BalanceBoardState.CalibrationInfo.Kg17.TopLeft;
                    calW_TR17 = wiiDevice.WiimoteState.BalanceBoardState.CalibrationInfo.Kg17.TopRight;

                    calW_BL34 = wiiDevice.WiimoteState.BalanceBoardState.CalibrationInfo.Kg34.BottomLeft;
                    calW_BR34 = wiiDevice.WiimoteState.BalanceBoardState.CalibrationInfo.Kg34.BottomRight;
                    calW_TL34 = wiiDevice.WiimoteState.BalanceBoardState.CalibrationInfo.Kg34.TopLeft;
                    calW_TR34 = wiiDevice.WiimoteState.BalanceBoardState.CalibrationInfo.Kg34.TopRight;

                    string txt = "CAL0_17_34:" + calW_BL0.ToString("F0") + ":" + calW_BR0.ToString("F0") + ":" + calW_TL0.ToString("F0") + ":" + calW_TR0.ToString("F0") + ":";
                    txt += calW_BL17.ToString("F0") + ":" + calW_BR17.ToString("F0") + ":" + calW_TL17.ToString("F0") + ":" + calW_TR17.ToString("F0") + ":";
                    txt += calW_BL34.ToString("F0") + ":" + calW_BR34.ToString("F0") + ":" + calW_TL34.ToString("F0") + ":" + calW_TR34.ToString("F0");
                    LogCalZeroLines.Add(txt);

                    StatusText = "Calibrate Zero Weight FINISHED.";
                }

                _doZeroCalibration = false;
            }

            if (_startPoseCalibration && _kinectBV.Delay < 0)
            {
                LogCalLines.Add("RL:" + OWBottomLeft.ToString("F0") + ":" + OWBottomRight.ToString("F0") + ":" + OWTopLeft.ToString("F0") + ":" + OWTopRight.ToString("F0"));
                LogCalLines.Add("CoP:" + (e.SignalTime - StartCalibrationTime).TotalMilliseconds.ToString("F0") + ":" + CalculatedCoPX.ToString("N3") + ":" + CalculatedCoPY.ToString("N3"));                
            }
            else if (_kinectBV.IsTesting && _kinectBV.Delay < 0)
            {
                string time = (e.SignalTime - StartTestTime).TotalMilliseconds.ToString("F0");
                LogTestLines.Add("CoP:" + time + ":" + CalculatedCoPX.ToString("N5") + ":" + CalculatedCoPY.ToString("N5"));
                LogTestLines.Add("RL:" + OWBottomLeft.ToString("F0") + ":" + OWBottomRight.ToString("F0") + ":" + OWTopLeft.ToString("F0") + ":" + OWTopRight.ToString("F0"));
                LogTestLines.Add("CoP-Kg:" + time + ":" + KgCalculatedCoPx.ToString("N5") + ":" + KgCalculatedCoPy.ToString("N5"));
                LogTestLines.Add("RL-Kg:" + KgBottomLeft.ToString("N2") + ":" + KgBottomRight.ToString("N2") + ":" + KgTopLeft.ToString("N2") + ":" + KgTopRight.ToString("N2"));                
            }

            // Discount read value when board is unloaded.
            ZeroCalWeight = KgTotalWeight - zeroWeight;

            CoGX = wiiDevice.WiimoteState.BalanceBoardState.CenterOfGravity.X;
            CoGY = wiiDevice.WiimoteState.BalanceBoardState.CenterOfGravity.Y;
        }


        Pen pen1 = new Pen(Brushes.White, 1);
        Pen penCentralVert = new Pen(Brushes.Wheat, 3);

        /// <summary>
        /// Show CoP movement on the balance board.
        /// </summary>
        private void ShowWBBImage()
        {
            using (DrawingContext dc = drawingGroup.Open())
            {
                dc.DrawRectangle(Brushes.Black, null, new System.Windows.Rect(0.0, 0.0, 440, 260));

                double x = 0;
                double y = 0;

                // Right is positive. Then, it goes beyond the center.
                if (CalculatedCoPX >= 0)
                {
                    x = 220 + CalculatedCoPX;
                }
                else if (CalculatedCoPX < 0)
                {
                    x = 220 - Math.Abs(CalculatedCoPX);
                }

                // Top is positive, then has the lowest values in Y
                if (CalculatedCoPY >= 0)
                {
                    y = 130 - CalculatedCoPY;
                }
                else if (CalculatedCoPY < 0) //  if negative, it is below the center.
                {
                    y = 130 + Math.Abs(CalculatedCoPY);
                }

                // Do not let it go out of bounds.
                if (x > 430) { x = 430; }
                if (y > 240) { y = 240; }

                dc.DrawEllipse(Brushes.Yellow, null, new System.Windows.Point(x, y), 5, 5);
                System.Windows.Point textLocation = new System.Windows.Point(1, 1);

                int j = -21;

                // Draw verticaltal lines
                for (int i = 5; i < 440; i += 20)
                {
                    dc.DrawText(new FormattedText(j.ToString("F0"), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight,
                                new Typeface("Georgia"), 8, Brushes.White),
                                new System.Windows.Point(i, 1));
                    // dc.DrawLine(pen1, new System.Windows.Point(i, 12), new System.Windows.Point(i, 248));
                    j += 2;
                }

                // Draw vertical central line
                dc.DrawLine(penCentralVert, new System.Windows.Point(219, 12), new System.Windows.Point(219, 248));

                // Draw horizontal lines
                j = 12;
                for (int i= 9; i<260; i +=20)
                {                    
                    dc.DrawText(new FormattedText(j.ToString("F0"), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight,
                                new Typeface("Georgia"), 8, Brushes.White),
                                new System.Windows.Point(430, i));
                    // dc.DrawLine(pen1, new System.Windows.Point(5, i+2), new System.Windows.Point(425, i));
                    j -= 2;
                }

                // Draw horizontal central line
                dc.DrawLine(penCentralVert, new System.Windows.Point(5, 129), new System.Windows.Point(425, 129));
            }

            this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, 440, 260));
        }

        protected abstract void infoUpdateTimer_Elapsed(object sender, ElapsedEventArgs e);

        private void wiiDevice_WiimoteChanged(object sender, WiimoteChangedEventArgs e)
        {
            // Called every time there is a sensor update, values available using e.WiimoteState.
            // Use this for tracking and filtering rapid accelerometer and gyroscope sensor data.
            // The balance board values are basic, so can be accessed directly only when needed.
        }

        private void wiiDevice_WiimoteExtensionChanged(object sender, WiimoteExtensionChangedEventArgs e)
        {
            // This is not needed for balance boards.
        }

        public KinectBodyView GetKinectBodyView()
        {
            return _kinectBV;
        }

        #region Properties
        private string _pageName;

        public ImageSource WBB_Image
        {
            get { return imageSource; }
        }

        public bool ZeroCalibrationDone
        {
            get { return _zeroCalibrationDone; }
            set
            {
                if (value != _zeroCalibrationDone)
                {
                    _zeroCalibrationDone = value;
                    OnPropertyChanged("ZeroCalibrationDone");
                }
            }
        }

        public bool PoseCalibrationDone
        {
            get { return _poseCalibrationDone; }
            set
            {
                if(_poseCalibrationDone != value)
                {
                    _poseCalibrationDone = value;
                    OnPropertyChanged("PoseCalibrationDone");
                }
            }
        }

        public bool FinishedTest
        {
            get { return _finishedTest; }
            set
            {
                if(value != _finishedTest)
                {
                    _finishedTest = value;
                    OnPropertyChanged("FinishedTest");
                }
            }
        }

        // Name of the test being executed
        public string Name
        {
            get { return _pageName; }
            set
            {
                if (_pageName != value)
                {
                    _pageName = value;
                    PageTitle = value;

                    if(_pageName == "DS")
                    {
                        ButtonVisibility = true;
                        Picture = new BitmapImage(new Uri("C:\\Users\\anton\\source\\repos\\NewBess\\NewBess\\bin\\x64\\Debug\\DoubleStance-BESS.jpg"));
                    }
                    else
                    {
                        if(_pageName == "SS")
                        {
                            Picture = new BitmapImage(new Uri("C:\\Users\\anton\\source\\repos\\NewBess\\NewBess\\bin\\x64\\Debug\\SingleStance-BESS.jpg"));
                        } else
                        {
                            Picture = new BitmapImage(new Uri("C:\\Users\\anton\\source\\repos\\NewBess\\NewBESS\\bin\\x64\\Debug\\TandemStance-BESS.jpg"));
                        }

                        ButtonVisibility = false;
                    }

                    // Change also the name of module in the kinect sensor.
                    if(_kinectBV != null)
                    {
                        _kinectBV.ModuleName = value;
                    }

                }
            }
        }

        string _pageTitle;
        public string PageTitle
        {
            get { return _pageTitle; }
            set
            {
                if(_pageTitle != value)
                {
                    if(value == "DS")
                    {
                        _pageTitle = "Double Leg Stance Balance Test";
                    } else if(value == "SS")
                    {
                        _pageTitle = "Single Leg Stance Balance Test";
                    } else
                    {
                        _pageTitle = "Tandem Leg Stance Balance Test";
                    }
                    OnPropertyChanged("PageTitle");
                }
            }
        }

        bool _buttonVisibility = true;
        public bool ButtonVisibility
        {
            get { return _buttonVisibility; } 
            set
            {
                if(_buttonVisibility != value)
                {
                    _buttonVisibility = value;
                    OnPropertyChanged("ButtonVisibility");
                }
            }
        }

        public int ParticipantID
        {
            get { return _app.participantId; }
        }

        public string ExperimentalCondition
        {
            get { return _app.condition; }
            set
            {
                if(_app.condition != value)
                {
                    _app.condition = value;
                    OnPropertyChanged("ExperimentalCondition");
                }
            }
        }

        public float CalculatedCoPX
        {
            get { return _calculatedCoPX; }
            set
            {
                if (_calculatedCoPX != value)
                {
                    _calculatedCoPX = value;
                    OnPropertyChanged("CalculatedCoPX");
                }
            }
        }

        public float CalculatedCoPY
        {
            get { return _calculatedCoPY; }
            set
            {
                if (_calculatedCoPY != value)
                {
                    _calculatedCoPY = value;
                    OnPropertyChanged("CalculatedCoPY");
                }
            }
        }

        public float KgTopLeft
        {
            get { return _kgTopLeft; }
            set
            {
                if (_kgTopLeft != value)
                {
                    _kgTopLeft = value;
                    OnPropertyChanged("KgTopLeft");
                }
            }
        }

        public float KgTopRight
        {
            get { return _kgTopRight; }
            set
            {
                if (_kgTopRight != value)
                {
                    _kgTopRight = value;
                    OnPropertyChanged("KgTopRight");
                }
            }
        }

        public float KgBottomLeft
        {
            get { return _kgBottomLeft; }
            set
            {
                if (_kgBottomLeft != value)
                {
                    _kgBottomLeft = value;
                    OnPropertyChanged("KgBottomLeft");
                }
            }
        }

        public float KgBottomRight
        {
            get { return _kgBottomRight; }
            set
            {
                if (_kgBottomRight != value)
                {
                    _kgBottomRight = value;
                    OnPropertyChanged("KgBottomRight");
                }
            }
        }

        public float KgTotalWeight
        {
            get { return _kgWeight; }
            set
            {
                if (_kgWeight != value)
                {
                    _kgWeight = value;
                    OnPropertyChanged("KgTotalWeight");
                }
            }
        }

        //
        // Raw Signal.
        //
        public float OWTopLeft
        {
            get { return _owTopLeft; }
            set
            {
                if (_owTopLeft != value)
                {
                    _owTopLeft = value;
                    OnPropertyChanged("OWTopLeft");
                }
            }
        }

        public float OWTopRight
        {
            get { return _owTopRight; }
            set
            {
                if (_owTopRight != value)
                {
                    _owTopRight = value;
                    OnPropertyChanged("OWTopRight");
                }
            }
        }

        public float OWBottomLeft
        {
            get { return _owBottomLeft; }
            set
            {
                if (_owBottomLeft != value)
                {
                    _owBottomLeft = value;
                    OnPropertyChanged("OWBottomLeft");
                }
            }
        }

        public float OWBottomRight
        {
            get { return _owBottomRight; }
            set
            {
                if (_owBottomRight != value)
                {
                    _owBottomRight = value;
                    OnPropertyChanged("OWBottomRight");
                }
            }
        }

        public float OWTotalWeight
        {
            get { return _owWeight; }
            set
            {
                if (_owWeight != value)
                {
                    _owWeight = value;
                    OnPropertyChanged("OWTotalWeight");
                }
            }
        }

        public float ZeroCalWeight
        {
            get { return _zeroCalWeight; }
            set
            {
                if (_zeroCalWeight != value)
                {
                    _zeroCalWeight = value;
                    OnPropertyChanged("zeroCalWeight");
                }
            }
        }

        public float CoGX
        {
            get { return _CoGX; }
            set
            {
                if (_CoGX != value)
                {
                    _CoGX = value;
                    OnPropertyChanged("CoGX");
                }
            }
        }

        public float CoGY
        {
            get { return _CoGY; }
            set
            {
                if (_CoGY != value)
                {
                    _CoGY = value;
                    OnPropertyChanged("CoGY");
                }
            }
        }

        public string StatusText
        {
            get { return _statusText; }
            set
            {
                if (_statusText != value)
                {
                    _statusText = value;
                    OnPropertyChanged("Statustext");
                }
            }
        }

        public string TestTime
        {
            get { return _testTime; }
            set
            {
                if (_testTime != value)
                {
                    _testTime = value;
                    OnPropertyChanged("TestTime");
                }
            }
        }

        public bool SavedData
        {
            get { return _savedata; }
            set
            {
                if(_savedata != value)
                {
                    _savedata = value;
                }
            }
        }

        /// <summary>
        /// Show picture of the test position.
        /// </summary>
        public ImageSource Picture
        {
            get { return picture; }
            set
            {
                if(picture != value)
                {
                    picture = value;
                    OnPropertyChanged("Picture");
                }
            }

        }
        #endregion
    }
}

using System;
using System.Windows.Input;
using Microsoft.Win32;
using System.Collections.Generic;
using Microsoft.Kinect;
using System.Windows;
using System.Windows.Media;
using System.Timers;

namespace NewBess
{
    class ReviewViewModel : ObservableObject, IPageViewModel
    {
        ResultsViewModel _resultsViewModel = null;

        List<DataLog> LogCoP;
        List<DataLog> LogCoM;

        bool _finishedPlay = false;

        private string _pageName;
        public string Name
        {
            get { return _pageName; }
            set
            {
                if (value != _pageName)
                {
                    _pageName = value; 
                }
            }

        }

        private string _cpcmFilename;
        private string _jointsFilename;

        public string CPCM_Filename {
            get { return _cpcmFilename; }
            set
            {
                if(_cpcmFilename != value)
                {
                    _cpcmFilename = value;
                    OnPropertyChanged("CPCM_Filename");
                }
            }
        }

        private string[] LogJoints;
        private string[] LogCPCM;

        class Frame
        {
            public Dictionary<JointType, Point> jointPoints { get; set; }
            public float CoM_X { get; set; }
            public float CoM_Y { get; set; }

            public Frame()
            {
                jointPoints = new Dictionary<JointType, Point>();
            }

        }

        List<Frame> bodyJoints = new List<Frame>();

        public string  Joints_Filename {
            get { return _jointsFilename; }
            set
            {
                if(_jointsFilename != value)
                {
                    _jointsFilename = value;
                    OnPropertyChanged("Joints_Filename");
                }
            }
        }

        public ICommand getCPCM_File;
        public ICommand getJoints_File;
        public ICommand playCommand;
        public ICommand _resultsCommand;

        private ApplicationViewModel _app;

        private List<Tuple<JointType, JointType>> bones;
        private DrawingGroup drawingGroup;
        private DrawingImage imageSource;

        private System.Timers.Timer infoUpdateTimer = new System.Timers.Timer() { Interval = 20, Enabled = false };

        public ReviewViewModel(ApplicationViewModel app)
        {
            _app = app;
            Name = "ReviewVM";

            LogCoM = new List<DataLog>();
            LogCoP = new List<DataLog>();

            _finishedPlay = false;

            infoUpdateTimer.Elapsed += new ElapsedEventHandler(infoUpdateTimer_Elapsed);
        }

        #region Commands
        public ICommand GetCPCM_File
        {
            get
            {
                if (getCPCM_File == null)
                {
                    getCPCM_File = new RelayCommand(param => Get_FileFunction("CPCM"), param => true);
                }
                return getCPCM_File;
            }
        }

        public ICommand GetJoints_File
        {
            get
            {
                if (getJoints_File == null)
                {
                    getJoints_File = new RelayCommand(param => Get_FileFunction("Joints"), param => true);
                }
                return getJoints_File;
            }
        }

        public ICommand Play
        {
            get
            {
                if (playCommand == null)
                {
                    playCommand = new RelayCommand(param => PlayFunction(), 
                        param => !string.IsNullOrEmpty(CPCM_Filename) && !string.IsNullOrEmpty(Joints_Filename));
                }
                return playCommand;
            }
        }

        public ICommand ResultsCommand
        {
            get
            {
                if (_resultsCommand == null)
                {
                    _resultsCommand = new RelayCommand(param => Results(), param => _finishedPlay);
                }

                return _resultsCommand;
            }
        }

        private void Get_FileFunction(string filename)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.DefaultExt = ".txt";
            bool? result = dlg.ShowDialog();

            if(result == true)
            {
                if (filename == "Joints")
                {
                    Joints_Filename = dlg.FileName;
                } else
                {
                    CPCM_Filename = dlg.FileName;
                }
            }
        }
        
        private void PlayFunction()
        {
            CoGX = 0.0f;
            CoGZ = 0.0f;
            CalculatedCoPX = 0.0f;
            CalculatedCoPY = 0.0f;

            StatusText = "Playing ....";

            CreateBones();
            LoadJointsPosition();
            // Missing load CoM and CoP data in CPCM file, need it to filled up LogcoM and LogCoP lists and see results.
            LoadFullLog();
            _finishedPlay = true;
        }

        /// <summary>
        /// Show charts and analysed data from collected data.
        /// </summary>
        private void Results()
        {
            if (_resultsViewModel == null)
            {
                _resultsViewModel = new ResultsViewModel(_app, LogCoP, LogCoM, Name);
            } else
            {
                _resultsViewModel.LoadResultViewModel(LogCoP, LogCoM, Name);
            }

            _app.ChangeViewModel(_resultsViewModel);
        }
        #endregion Commands

        int counter;

        private void LoadJointsPosition()
        {
            LogJoints = System.IO.File.ReadAllLines(Joints_Filename);
            counter = 0;

            // Create the drawing group we'll use for drawing
            drawingGroup = new DrawingGroup();

            // Create an image source that we can use in our image control
            ImageSource = new DrawingImage(drawingGroup);

            infoUpdateTimer.Enabled = true;
        }

        private void LoadFullLog()
        {
            LogCPCM = System.IO.File.ReadAllLines(CPCM_Filename);
            string[] tokens;
            bool validLine = false;

            double time;
            float x, y, z;

            LogCoM.Clear();
            LogCoP.Clear();

            foreach (string line in LogCPCM)
            {
                // read lines until find the start of the test.
                if(!validLine && line.Contains("#Start test"))
                {
                    validLine = true;
                    continue;
                } else
                {
                    // if found line from where test starts.
                    if(validLine)
                    {
                        tokens = line.Split(':');

                        // if not end of log.
                        if (!line.Contains("Finished"))
                        {
                            if (tokens[0] == "CoP-Kg")
                            {
                                time = double.Parse(tokens[1]);
                                x = float.Parse(tokens[2]);
                                y = float.Parse(tokens[3]);
                                DataLog record = new DataLog(time, x, y, 0f);
                                LogCoP.Add(record);
                            }
                            else if (tokens[0] == "CoM")
                            {
                                time = double.Parse(tokens[1]);
                                x = float.Parse(tokens[2]);
                                y = float.Parse(tokens[3]);
                                z = float.Parse(tokens[4]);
                                DataLog record = new DataLog(time, x, y, z);
                                LogCoM.Add(record);
                            }
                        } else                                            // End of log has total number of errors.
                        {
                            TestResult = int.Parse(tokens[1]);
                        }
                    }
                }
            }
        }

        private void infoUpdateTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            string[] tokens;
            Point CoM, CoP;
            CoM = new Point();
            CoP = new Point();

            Dictionary<JointType, Point> readPoints;

            if (counter < LogJoints.Length)
            {
                string line = LogJoints[counter++];

                tokens = line.Split(':');

                readPoints = new Dictionary<JointType, Point>();

                for (int i = 0; i <= 75; i += 3)
                {
                    int index;
                    float x, y;

                    if (i < 75)
                    {
                        index = int.Parse(tokens[i]);
                        x = float.Parse(tokens[i + 1]);
                        y = float.Parse(tokens[i + 2]);

                        readPoints[(JointType)index] = new Point(x, y);
                    }
                    else
                    {
                        x = float.Parse(tokens[i + 1]);
                        CoM.X = x;

                        y = float.Parse(tokens[i + 3]);
                        CoM.Y = y;

                        CoGX = float.Parse(tokens[i + 4]);
                        CoGZ = float.Parse(tokens[i + 6]);


                        CoP.X = float.Parse(tokens[i + 8]);
                        CoP.Y = float.Parse(tokens[i + 10]);
                    }
                }

                CalculatedCoPX = (float) CoP.X;
                CalculatedCoPY = (float) CoP.Y;

                if (counter % 3 == 0)
                {
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        StatusText = "Playing...  " + ((counter * 33) / 1000).ToString("F0") + " (s)";
                    }
                    ));
                }

                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    ShowImage(readPoints, CoM); 
                }));
            } else
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    StatusText = "Finished.";
                }
                    ));
                infoUpdateTimer.Enabled = false;
            }
        }

        private void ShowImage(Dictionary<JointType, Point> readPoints, Point CoM)
        {
            Pen drawingPen = new Pen(Brushes.Red, 6);

            using (DrawingContext dc = drawingGroup.Open())
            {
                // Draw a transparent background to set the render size
                dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, 512, 424));

                foreach (var bone in bones)
                {
                    dc.DrawLine(drawingPen, readPoints[bone.Item1], readPoints[bone.Item2]);
                }

                for (int i = 0; i < 25; i++)
                {
                    dc.DrawEllipse(Brushes.OrangeRed, null, readPoints[(JointType)i], 3, 3);
                }

                dc.DrawEllipse(Brushes.White, null, CoM, 5, 5);

                drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, 512, 424));
            }
        }

        private void CreateBones()
        {
            this.bones = new List<Tuple<JointType, JointType>>();
            // Torso
            this.bones.Add(new Tuple<JointType, JointType>(JointType.Head, JointType.Neck));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.Neck, JointType.SpineShoulder));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.SpineMid));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineMid, JointType.SpineBase));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipLeft));

            // Right Arm
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderRight, JointType.ElbowRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowRight, JointType.WristRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.HandRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HandRight, JointType.HandTipRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.ThumbRight));

            // Left Arm
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderLeft, JointType.ElbowLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowLeft, JointType.WristLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.HandLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HandLeft, JointType.HandTipLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.ThumbLeft));

            // Right Leg
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HipRight, JointType.KneeRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeRight, JointType.AnkleRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleRight, JointType.FootRight));
            // Left Leg
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HipLeft, JointType.KneeLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeLeft, JointType.AnkleLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleLeft, JointType.FootLeft));
        }

        private float _CoGx;
        public float CoGX
        {
            get { return _CoGx; }
            set
            {
                if (_CoGx != value)
                {
                    _CoGx = value;
                    OnPropertyChanged("CoGX");
                }
            }
        }

        private float _CoGz;
        public float CoGZ
        {
            get { return _CoGz; }
            set
            {
                if (_CoGz != value)
                {
                    _CoGz = value;
                    OnPropertyChanged("CoGZ");
                }
            }
        }

        private float _calculatedCoPX;

        public float CalculatedCoPX
        {
            get { return _calculatedCoPX; }
            set
            {
                if(_calculatedCoPX != value)
                {
                    _calculatedCoPX = value;
                    OnPropertyChanged("CalculatedCoPX");
                }
            }
        }

        private float _calculatedCoPY;

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

        private int _testResult;
        public int TestResult
        {
            get { return _testResult; } 
            set
            {
                if(_testResult != value)
                {
                    _testResult = value;
                    OnPropertyChanged("TestResult");
                }
            }
        }


        string _statusText = "";
        public string StatusText
        {
            get { return _statusText; }
            set
            {
                if(_statusText != value)
                {
                    _statusText = value;
                    OnPropertyChanged("StatusText");
                }
            }
        }


        public ImageSource ImageSource
        {
            get { return this.imageSource; }
            set
            {
                if(imageSource != value)
                {
                    imageSource = (DrawingImage) value;
                    OnPropertyChanged("ImageSource");
                }
            }
        }
    }
}

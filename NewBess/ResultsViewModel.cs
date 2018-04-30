using LiveCharts;
using LiveCharts.Defaults;
using System;
using System.Collections.Generic;
using System.Windows.Input;

// Another chart library to check out is docs.oxyplot.org, seems to be simpler and slighter.


namespace NewBess
{
    class ResultsViewModel : ObservableObject, IPageViewModel
    {
        ApplicationViewModel _app;
        string _prevPageName = "";

        List<DataLog> _listCoP;
        List<DataLog> _listCoM;

        private ICommand _backCommand;
        private ICommand _filterCommand;

        // Center of pressure
        public ChartValues<ScatterPoint> CoP { get; set; }                       // Statocinesiogram (A-P vs M-L) space
        public ChartValues<ObservablePoint> CoP_X { get; set; }                     // Stabilogram serie M-L over time
        public ChartValues<ObservablePoint> CoP_Y { get; set; }                     // Stabilogram serie A-P over time

        // Center of gravity
        public ChartValues<ObservablePoint> CoG { get; set; }                       // Statocinesiogram (A-P vs M-L) space
        public ChartValues<ObservablePoint> CoG_X { get; set; }                     // Stabilogram serie M-L over time
        public ChartValues<ObservablePoint> CoG_Z { get; set; }                     // Stabilogram serie A-P over time


        #region Properties
        private string _pageName;
        public string Name
        {
            get { return _pageName; }
            set
            {
                if (_pageName != value)
                    _pageName = value;
            }
        }

        /// <summary>
        /// Filter butterworth cutoff frequency - 0 = unfiltered.
        /// </summary>
        private double _cutoff;
        public double CutOff
        {
            get { return _cutoff; }
            set
            {
                if (_cutoff != value)
                {
                    _cutoff = value;
                    OnPropertyChanged("CutOff");
                }
            }
        }

        private double _totalPathLength = 0;
        public double TotalPathLength
        {
            get { return _totalPathLength; }
            set
            {
                if (_totalPathLength != value)
                {
                    _totalPathLength = value;
                    OnPropertyChanged("TotalPathLength");
                }
            }
        }

        private double _totalPathLengthCoG = 0;
        public double TotalPathLengthCoG
        {
            get { return _totalPathLengthCoG; }
            set
            {
                if (_totalPathLengthCoG != value)
                {
                    _totalPathLengthCoG = value;
                    OnPropertyChanged("TotalPathLengthCoG");
                }
            }
        }

        private double _peakVelocity;
        public double PeakVelocity
        {
            get { return _peakVelocity; }
            set
            {
                if (_peakVelocity != value)
                {
                    _peakVelocity = value;
                    OnPropertyChanged("PeakVelocity");
                }
            }
        }

        private double _avgVelocity;
        public double AvgVelocity
        {
            get { return _avgVelocity; }
            set
            {
                if (_avgVelocity != value)
                {
                    _avgVelocity = value;
                    OnPropertyChanged("AvgVelocity");
                }
            }
        }

        private double _avgVelocityCoG;
        public double AvgVelocityCoG
        {
            get { return _avgVelocityCoG; }
            set
            {
                if (_avgVelocityCoG != value)
                {
                    _avgVelocityCoG = value;
                    OnPropertyChanged("AvgVelocityCoG");
                }
            }
        }

        private double _peakVelocityCoG;
        public double PeakVelocityCoG
        {
            get { return _peakVelocityCoG; }
            set
            {
                if (_peakVelocityCoG != value)
                {
                    _peakVelocityCoG = value;
                    OnPropertyChanged("PeakVelocityCoG");
                }
            }
        }

        private float _minCoP_X, _maxCoP_X, _minCoP_Y, _maxCoP_Y;
        public float MinCoP_X
        {
            get { return _minCoP_X; }
            set
            {
                if (_minCoP_X != value)
                {
                    _minCoP_X = value;
                    OnPropertyChanged("MinCoP_X");
                }
            }
        }

        public float MaxCoP_X
        {
            get { return _maxCoP_X; }
            set
            {
                if (_maxCoP_X != value)
                {
                    _maxCoP_X = value;
                    OnPropertyChanged("MaxCoP_X");
                }
            }
        }

        public float MinCoP_Y
        {
            get { return _minCoP_Y; }
            set
            {
                if (_minCoP_Y != value)
                {
                    _minCoP_Y = value;
                    OnPropertyChanged("MinCoP_Y");
                }
            }
        }

        public float MaxCoP_Y
        {
            get { return _maxCoP_Y; }
            set
            {
                if (_maxCoP_Y != value)
                {
                    _maxCoP_Y = value;
                    OnPropertyChanged("MaxCoP_Y");
                }
            }
        }

        private float _minCoG_X, _maxCoG_X, _minCoG_Z, _maxCoG_Z;
        public float MinCoG_X
        {
            get { return _minCoG_X; }
            set
            {
                if (_minCoG_X != value)
                {
                    _minCoG_X = value;
                    OnPropertyChanged("MinCoG_X");
                }
            }
        }

        public float MaxCoG_X
        {
            get { return _maxCoG_X; }
            set
            {
                if (_maxCoG_X != value)
                {
                    _maxCoG_X = value;
                    OnPropertyChanged("MaxCoG_X");
                }
            }
        }

        public float MinCoG_Z
        {
            get { return _minCoG_Z; }
            set
            {
                if (_minCoG_Z != value)
                {
                    _minCoG_Z = value;
                    OnPropertyChanged("MinCoG_Z");
                }
            }
        }

        public float MaxCoG_Z
        {
            get { return _maxCoG_Z; }
            set
            {
                if (_maxCoG_Z != value)
                {
                    _maxCoG_Z = value;
                    OnPropertyChanged("MaxCoG_Z");
                }
            }
        }

        private float _ampCoP_X, _ampCoP_Y, _ampCoG_X, _ampCoG_Z;
        public float AmplitudeCoP_X
        {
            get
            {
                return _ampCoP_X;
            }
            set {
                if (_ampCoP_X != value)
                {
                    _ampCoP_X = value;
                    OnPropertyChanged("AmplitudeCoP_X");
                }
            }
        }

        public float AmplitudeCoP_Y
        {
            get
            {
                return _ampCoP_Y;
            }

            set
            {
                if (_ampCoP_Y != value)
                {
                    _ampCoP_Y = value;
                    OnPropertyChanged("AmplitudeCoP_Y");
                }
            }
        }

        public float AmplitudeCoG_X
        {
            get
            {
                return _ampCoG_X;
            }
            set
            {
                if (_ampCoG_X != value)
                {
                    _ampCoG_X = value;
                    OnPropertyChanged("AmplitudeCoG_X");
                }
            }
        }

        public float AmplitudeCoG_Z
        {
            get { return _ampCoG_Z; }
            set
            {
                if (_ampCoG_Z != value)
                {
                    _ampCoG_Z = value;
                    OnPropertyChanged("AmplitudeCoG_Z");
                }
            }
        }

        private double _rmsCoPX, _rmsCoPY, _rmsCoMX, _rmsCoMZ;
        public double RMSCoP_X
        {
            get { return _rmsCoPX; }
            set
            {
                if (_rmsCoPX != value)
                {
                    _rmsCoPX = value;
                    OnPropertyChanged("RMSCoP_X");
                }
            }
        }

        public double RMSCoP_Y
        {
            get { return _rmsCoPY; }
            set
            {
                if (_rmsCoPY != value)
                {
                    _rmsCoPY = value;
                    OnPropertyChanged("RMSCoP_Y");
                }
            }
        }


        private double _stddevCoPX, _stddevCoPY;
        public double StdDevCoPX
        {
            get { return _stddevCoPX; }
            set
            {
                if (_stddevCoPX != value)
                {
                    _stddevCoPX = value;
                    OnPropertyChanged("StdDevCoPX");
                }
            }
        }

        public double StdDevCoPY
        {
            get { return _stddevCoPY; }
            set
            {
                if (_stddevCoPY != value)
                {
                    _stddevCoPY = value;
                    OnPropertyChanged("StdDevCoPY");
                }
            }
        }



        public double RMSCoM_X
        {
            get { return _rmsCoMX; }
            set
            {
                if (_rmsCoMX != value)
                {
                    _rmsCoMX = value;
                    OnPropertyChanged("RMSCoM_X");
                }
            }
        }

        public double RMSCoM_Z
        {
            get { return _rmsCoMZ; }
            set
            {
                if (_rmsCoMZ != value)
                {
                    _rmsCoMZ = value;
                    OnPropertyChanged("RMSCoM_Z");
                }
            }
        }

        private double _meanCoPX, _meanCoPY;
        public double MeanCoPX
        {
            get { return _meanCoPX; }
            set
            {
                if(_meanCoPX != value)
                {
                    _meanCoPX = value;
                    OnPropertyChanged("MeanCoPX");
                }
            }
        }

        public double MeanCoPY
        {
            get { return _meanCoPY; }
            set
            {
                if(_meanCoPY != value)
                {
                    _meanCoPY = value;
                    OnPropertyChanged("MeanCoPY");
                }
            }
        }
        #endregion

#region Commands
        public ICommand BackCommand
        {
            get
            {
                if (_backCommand == null)
                {
                    _backCommand = new RelayCommand(param => GoBack());
                }

                return _backCommand;
            }
        }

        public ICommand FilterCommand
        {
            get
            {
                if (_filterCommand == null)
                {
                    _filterCommand = new RelayCommand(param => Filter());
                }

                return _filterCommand;
            }
        }
#endregion

        // Return to calling page.
        private void GoBack()
        {
            _app.ChangeViewModel(_app.PageViewModels.Find(r => r.Name == _prevPageName));
        }

        /// <summary>
        /// Apply butterworth filter and refill _listCoP / _listCoM.
        /// </summary>
        private void Filter()
        {
            double[] cop_x, cop_y;

            cop_x = new double[_listCoP.Count];
            cop_y = new double[_listCoP.Count];

            int i = 0;

            foreach(DataLog data in _listCoP)
            {
                cop_x[i] = data.CX;
                cop_y[i] = data.CY;
                i++;
            }

            cop_x = Filters.Butterworth(cop_x, 0.033, CutOff);
            cop_y = Filters.Butterworth(cop_y, 0.033, CutOff);

            for(i=0; i < cop_x.Length; i++)
            {
                _listCoP[i].CX = (float) cop_x[i];
                _listCoP[i].CY = (float) cop_y[i];
            }

            double[] com_x, com_z;
            com_x = new double[_listCoM.Count];
            com_z = new double[_listCoM.Count];

            i = 0;

            foreach(DataLog data in _listCoM)
            {
                com_x[i] = data.CX;
                com_z[i] = data.CZ;
                i++;
            }

            com_x = Filters.Butterworth(com_x, 0.033, CutOff);
            com_z = Filters.Butterworth(com_z, 0.033, CutOff);

            for(i=0; i < com_x.Length; i++)
            {
                _listCoM[i].CX = (float) com_x[i];
                _listCoM[i].CZ = (float) com_z[i];
            }

            MinCoP_X = float.MaxValue;
            MaxCoP_X = float.MinValue;

            MinCoP_Y = float.MaxValue;
            MaxCoP_Y = float.MinValue;

            MinCoG_X = float.MaxValue;
            MaxCoG_X = float.MinValue;

            MinCoG_Z = float.MaxValue;
            MaxCoG_Z = float.MinValue;

            ShowCharts();
        }


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="app">Pointer to main application</param>
        /// <param name="ListCoP">List of datalog records - center of pressure</param>
        /// <param name="ListCoM">List of datalog records - center of mass</param>
        /// <param name="pageName">Name of the calling usercontrol/page</param>
        public ResultsViewModel(ApplicationViewModel app, List<DataLog> ListCoP, List<DataLog> ListCoM, string pageName)
        {
            _app = app;
            Name = "ResultViewModel";
            CoP = new ChartValues<ScatterPoint>();
            CoP_X = new ChartValues<ObservablePoint>();
            CoP_Y = new ChartValues<ObservablePoint>();

            CoG = new ChartValues<ObservablePoint>();
            CoG_X = new ChartValues<ObservablePoint>();
            CoG_Z = new ChartValues<ObservablePoint>();

            LoadResultViewModel(ListCoP, ListCoM, pageName);
        }

        public void LoadResultViewModel(List<DataLog> ListCoP, List<DataLog> ListCoM, string pageName)
        {
            _prevPageName = pageName;

            MinCoP_X = float.MaxValue;
            MaxCoP_X = float.MinValue;

            MinCoP_Y = float.MaxValue;
            MaxCoP_Y = float.MinValue;

            MinCoG_X = float.MaxValue;
            MaxCoG_X = float.MinValue;

            MinCoG_Z = float.MaxValue;
            MaxCoG_Z = float.MinValue;

            _listCoM = ListCoM;
            _listCoP = ListCoP;

            ShowCharts();
        }

        /// <summary>
        ///  Show CoP collected information.
        ///  Called from UserControl load event.
        /// </summary>
        public void ShowCharts()
        {
            TotalPathLength = 0;
            PeakVelocity = float.MinValue;

            double xAnt = _listCoP[0].CX;
            double yAnt = _listCoP[0].CY;
            double timeAnt = _listCoP[0].elapsedTime;
            double initialTime = timeAnt;
            double distance = 0;
            double vel = 0;

            CoP.Clear();
            CoP_X.Clear();
            CoP_Y.Clear();

            MeanCoPX = 0;
            MeanCoPY = 0;

            RMSCoP_X = 0;
            RMSCoP_Y = 0;

            // Find mean and RMS
            foreach (DataLog data in _listCoP)
            {
                MeanCoPX += data.CX;
                MeanCoPY += data.CY;
                RMSCoP_X += (data.CX * data.CX);
                RMSCoP_Y += (data.CY * data.CY);
            }

            MeanCoPX = MeanCoPX / _listCoP.Count;
            MeanCoPY = MeanCoPY / _listCoP.Count;               // Now we can find standard deviation of the serie.
            RMSCoP_X = Math.Sqrt(RMSCoP_X / _listCoP.Count);
            RMSCoP_Y = Math.Sqrt(RMSCoP_Y / _listCoP.Count);

            StdDevCoPX = 0;
            StdDevCoPY = 0;

            foreach (DataLog data in _listCoP)
            {
                StdDevCoPX += Math.Pow(data.CX - MeanCoPX, 2);
                StdDevCoPY += Math.Pow(data.CY - MeanCoPY, 2);
            }

            StdDevCoPX = Math.Sqrt(StdDevCoPX / (_listCoP.Count - 1));
            StdDevCoPY = Math.Sqrt(StdDevCoPY / (_listCoP.Count - 1));

            double x, y;

            foreach (DataLog data in _listCoP)
            {
                x = data.CX;
                y = data.CY;

                while ((x > (MeanCoPX + (StdDevCoPX * 5))) || (x < (MeanCoPX - (StdDevCoPX * 5))))
                {
                    x = (x + MeanCoPX) / 2;
                }

                data.CX = (float) x;

                while( (y > (MeanCoPY + (StdDevCoPY * 5))) || (y < (MeanCoPY - (StdDevCoPY * 5)))) {
                    y = (y + MeanCoPY) / 2;
                }

                data.CY = (float) y;

                if (data.CX < MinCoP_X) MinCoP_X = data.CX;
                if (data.CX > MaxCoP_X) MaxCoP_X = data.CX;

                if (data.CY < MinCoP_Y) MinCoP_Y = data.CY;
                if (data.CY > MaxCoP_Y) MaxCoP_Y = data.CY;

                // Calculate total path length (frame by frame vector position) in (mm)
                distance = Math.Sqrt(Math.Pow((double)data.CX - xAnt, 2) + Math.Pow((double)data.CY - yAnt, 2));
                TotalPathLength += distance;
          
                // Calculate instante velocity and check peak velocity.
                vel = (distance / 1000) / ((data.elapsedTime - timeAnt) / 1000);

                if(vel > PeakVelocity && !double.IsInfinity(vel))
                {
                    PeakVelocity = vel;
                }

                xAnt = (double) data.CX;
                yAnt = (double) data.CY;
                timeAnt = data.elapsedTime;

                CoP.Add(new ScatterPoint(data.CX - MeanCoPX , data.CY - MeanCoPY, 0.1 ));
                CoP_X.Add(new ObservablePoint(data.elapsedTime, data.CX - MeanCoPX));
                CoP_Y.Add(new ObservablePoint(data.elapsedTime, data.CY - MeanCoPY));
            }

            //  Path Length is in centimeters and Time is in milliseconds. Velocity in m/s
            AvgVelocity = (TotalPathLength / 1000) / ((timeAnt - initialTime) / 1000);

            ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // Deals with Center of Mass data.
            ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            TotalPathLengthCoG = 0;
            PeakVelocityCoG = 0;
            xAnt = _listCoM[0].CX;
            double zAnt = _listCoM[0].CZ;
            initialTime = _listCoM[0].elapsedTime;
            timeAnt = initialTime;

            CoG.Clear();
            CoG_X.Clear();
            CoG_Z.Clear();

            double meanCoGX = 0;
            double meanCoGZ = 0;

            RMSCoM_X = 0;
            RMSCoM_Z = 0;
            
            // finds Min and Max values of Center of Mass.
            foreach (DataLog data in _listCoM)
            {
                if (data.CX < MinCoG_X) MinCoG_X = data.CX;
                if (data.CX > MaxCoG_X) MaxCoG_X = data.CX;

                if (data.CZ < MinCoG_Z) MinCoG_Z = data.CZ;
                if (data.CZ > MaxCoG_Z) MaxCoG_Z = data.CZ;

                meanCoGX += data.CX;
                meanCoGZ += data.CZ;

                RMSCoM_X = (data.CX * data.CX);
                RMSCoM_Z = (data.CZ * data.CZ);
            }

            MinCoG_X = MinCoG_X * 1000;
            MaxCoG_X = MaxCoG_X * 1000;

            MinCoG_Z = MinCoG_Z * 1000;
            MaxCoG_Z = MaxCoG_Z * 1000;

            meanCoGX = (meanCoGX / _listCoM.Count);
            meanCoGZ = (meanCoGZ / _listCoM.Count);

            RMSCoM_X = Math.Sqrt(RMSCoM_X / _listCoM.Count);
            RMSCoM_Z = Math.Sqrt(RMSCoM_Z / _listCoM.Count);

            foreach (DataLog data in _listCoM)
            {
                // Calculate total path length (frame by frame vector position) in (mm)
                distance = Math.Sqrt(Math.Pow((double)data.CX - xAnt, 2) + Math.Pow((double)(data.CZ - zAnt), 2));
                TotalPathLengthCoG += distance;

                // Calculate instante velocity and check peak velocity.
                vel = distance / ((data.elapsedTime - timeAnt) / 1000);

                if (vel > PeakVelocityCoG)
                {
                    PeakVelocityCoG = vel;
                }

                xAnt = (double)data.CX;
                zAnt = (double)data.CZ;
                timeAnt = data.elapsedTime;

                // Kinect provides spatial data in meters, converting to mm
                CoG.Add(new ObservablePoint((data.CX - meanCoGX) * 1000, (data.CZ - meanCoGZ) * 1000));
                CoG_X.Add(new ObservablePoint(data.elapsedTime, (data.CX - meanCoGX) * 1000));
                CoG_Z.Add(new ObservablePoint(data.elapsedTime, (data.CZ - meanCoGZ) * 1000));
            }

            // Kinect provides position data in meters.
            AvgVelocityCoG = TotalPathLengthCoG / ((timeAnt - initialTime) / 1000);

            TotalPathLengthCoG = TotalPathLengthCoG * 1000;                                    // transform it to mm.
            meanCoGX = meanCoGX * 1000;
            meanCoGZ = meanCoGZ * 1000;
            RMSCoM_X = RMSCoM_X * 1000;
            RMSCoM_Z = RMSCoM_Z * 1000;

            AmplitudeCoP_X = Math.Abs(MaxCoP_X - MinCoP_X);
            AmplitudeCoP_Y = Math.Abs(MaxCoP_Y - MinCoP_Y);

            AmplitudeCoG_X = Math.Abs(MaxCoG_X - MinCoG_X);
            AmplitudeCoG_Z = Math.Abs(MaxCoG_Z - MinCoG_Z);
        }        
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewBess
{
    class ConfigurationViewModel : ObservableObject, IPageViewModel
    {
        ApplicationViewModel _app;

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

        public ConfigurationViewModel(ApplicationViewModel app)
        {
            _app = app;
            Name = "ConfVM";
        }

#region Properties
        /// <summary>
        /// General flag to either use or not Wii Balance Board features.
        /// </summary>
        public bool UseWBB
        {
            get { return _app.EnableWBB;  }
            set
            {
                if(_app.EnableWBB != value)
                {
                    _app.EnableWBB = value;
                    OnPropertyChanged("EnableWBB");
                }
            }
        }


        public bool SaveJoints
        {
            get { return _app.SaveJoints; }
            set
            {
                if (_app.SaveJoints != value)
                {
                    _app.SaveJoints = value;
                    OnPropertyChanged("SaveJoints");
                }
            }
        }


        public int CalibrationTime
        {
            get { return _app.CalibrationTime; }
            set
            {
                if(_app.CalibrationTime != value)
                {
                    _app.CalibrationTime = value;
                    OnPropertyChanged("CalibrationTime");
                }
            }
        }


        public int DelayTime
        {
            get { return _app.DelayTime; }
            set
            {
                if (_app.DelayTime != value)
                {
                    _app.DelayTime = value;
                    OnPropertyChanged("DelayTime");
                }
            }

        }

        public int TestTime
        {
            get { return _app.TestTime; }
            set
            {
                if (_app.TestTime != value)
                {
                    _app.TestTime = value;
                    OnPropertyChanged("TestTime");
                }
            }
        }

        public double JointPrecision
        {
            get { return _app.JointAxisPrecision; }
            set
            {
                if (_app.JointAxisPrecision != value)
                {
                    _app.JointAxisPrecision = value;
                    OnPropertyChanged("JointAxisPrecision");
                }
            }
        }

        public double AnglePrecision
        {
            get { return _app.AnglePrecision; }
            set
            {
                if (_app.AnglePrecision != value)
                {
                    _app.AnglePrecision = value;
                    OnPropertyChanged("AnglePrecision");
                }
            }
        }
        #endregion Properties

    }
}

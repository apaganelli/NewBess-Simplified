using System;
using System.Windows.Input;

namespace NewBess
{
    class TestsViewModel : ObservableObject, IPageViewModel
    {
        ApplicationViewModel _app;

        ICommand _continueCommand;

        PoseCalibrationViewModel _doubleCalibrationVM;

        string _statusTxt = "";

        public TestsViewModel(ApplicationViewModel app)
        {
            _app = app;
            Name = "TestsViewModel";
            ParticipantID = _app.participantId;
        }

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

        public ICommand ContinueCommand
        {
            get
            {
                if (_continueCommand == null)
                {
                    _continueCommand = new RelayCommand(param => Continue());
                }

                return _continueCommand;
            }
        }

        /// <summary>
        /// Always start doing the double leg stance test.
        /// </summary>
        private void Continue()
        {
            if(_app.participantName == "")
            {
                StatusText = "Participant not selected.";
                return;
            }

           _app.PreviousPageViewModel =_app.CurrentPageViewModel;

           _doubleCalibrationVM = (PoseCalibrationViewModel)_app.PageViewModels.Find(r => (r.Name == "DS"));

            if (_doubleCalibrationVM == null)
            {
                _doubleCalibrationVM = new PoseCalibrationViewModel(_app, "DS");
            } else
            {
                _doubleCalibrationVM.Name = "DS";
            }

            _app.ChangeViewModel(_doubleCalibrationVM);
        }

        int id;
        public int ParticipantID
        {
            get { return id; }
            set
            {
                if(id != value)
                {
                    id = value;
                    OnPropertyChanged("ParticipantID");
                }
            }
        }

        public int AppParticipantID
        {
            get { return _app.participantId; }
        }

        public string Group
        {
            get { return _app.condition; }
            set
            {
                if(_app.condition != value)
                {
                    _app.condition = value;
                    OnPropertyChanged("Group");
                }
            }
        }

        public string StatusText
        {
            get { return _statusTxt; }

            set
            {
                if(_statusTxt != value)
                {
                    _statusTxt = value;
                    OnPropertyChanged("StatusText");
                }
            }
        }

    }
}

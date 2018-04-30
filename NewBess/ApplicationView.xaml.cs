using System.Windows;
using System.Windows.Controls;

namespace NewBess   
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class ApplicationView : Window
    {

        private ConfigurationViewModel _configurationViewModel = null;
        private ParticipantsViewModel _participantsViewModel = null;
        private TestsViewModel _testsViewModel = null;
        private ReviewViewModel _reviewViewModel = null;

        public ApplicationView()
        {
            InitializeComponent();
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            if (e.Source is TabControl)
            {
                ApplicationViewModel app = (ApplicationViewModel)DataContext;

                if (TabItemConfiguration.IsSelected)
                {
                    if(_configurationViewModel == null)
                    {
                        _configurationViewModel = new ConfigurationViewModel(app);
                    }

                    app.ChangeViewModel(_configurationViewModel);

                } else if(TabItemParticipants.IsSelected)
                {
                    if(_participantsViewModel == null)
                    {
                        _participantsViewModel = new ParticipantsViewModel(app);
                    }

                    app.ChangeViewModel(_participantsViewModel);

                } else if(TabItemTests.IsSelected)
                {
                    if (_testsViewModel == null)
                    {
                        _testsViewModel = new TestsViewModel(app);
                        app.ChangeViewModel(_testsViewModel);
                    } else {

                        PoseCalibrationViewModel dcVM = (PoseCalibrationViewModel) app.PageViewModels.Find(r => r.Name == "DS" || r.Name == "SS" || r.Name == "TS");

                        if (dcVM != null)
                        {
                            app.ChangeViewModel(dcVM);
                        } else
                        {
                            app.ChangeViewModel(_testsViewModel);
                        }
                    }                    
                } else if(TabItemReview.IsSelected)
                {
                    if(_reviewViewModel == null)
                    {
                        _reviewViewModel = new ReviewViewModel(app);
                    }

                    app.ChangeViewModel(_reviewViewModel);
                }

            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            ApplicationViewModel app = (ApplicationViewModel)DataContext;

            PoseCalibrationViewModel dcVM = (PoseCalibrationViewModel) app.PageViewModels.Find(r => r.Name == "DS" || r.Name == "SS" || r.Name == "TS");

            if(dcVM != null)
            {
                dcVM.infoUpdateTimer.Enabled = false;
            }

            System.Windows.Application.Current.Shutdown();
        }
    }
}

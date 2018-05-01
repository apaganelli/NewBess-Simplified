using System.Windows;
using System.Windows.Controls;

namespace NewBess
{
    /// <summary>
    /// Interaction logic for BessTestView.xaml
    /// </summary>
    public partial class BessTestView : UserControl
    {
        public BessTestView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            PoseCalibrationViewModel dc = (PoseCalibrationViewModel)this.DataContext;
            dc.ResetMessages();
            dc.StatusText = "";
        }

        private void btNext_Click(object sender, RoutedEventArgs e) 
        {
            PoseCalibrationViewModel dc = (PoseCalibrationViewModel)this.DataContext;

            // If for any reason data was not saved. Save it, before going to the next position.
            if(! dc.SavedData && dc.FinishedTest)
            {
                dc.Save();
            } else
            {
                dc.ResetMessages();
                dc.StatusText = "";
            }

            // Clean up log space. It has already been saved.
            dc.ResetLogs();
            dc.SavedData = false;

            if(dc.Name == "DS")
            {
                dc.Name = "SS";
                dc.PoseCalibrationDone = false;

            } else if(dc.Name == "SS")
            {
                dc.PoseCalibrationDone = false;
                dc.Name = "TS";

                // In this case, this is the last position. There is no next.
                ((Button)sender).Visibility = Visibility.Hidden;
            }

            this.Refresh();
        }
    }
}

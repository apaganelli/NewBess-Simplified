using System.Windows;
using System.Windows.Controls;

namespace NewBess
{
    /// <summary>
    /// Interaction logic for DoubleView.xaml
    /// </summary>
    public partial class TestsView : UserControl
    {
        public TestsView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is TestsViewModel)
            {
                TestsViewModel data = (TestsViewModel)DataContext;

                if (data.StatusText != "")
                {
                    data.StatusText = "";
                }

                data.ParticipantID = data.AppParticipantID;
            }
        }
    }
}

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Xml;

namespace NewBess
{
    /// <summary>
    /// Interaction logic for ParticipantsView.xaml
    /// </summary>
    public partial class ParticipantsView : UserControl
    {
        XmlDataProvider dataProvider;

        public ParticipantsView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Gets the participant id. It is used for operations onto participant profile (delete, edit, new).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lbId_TextChanged(object sender, TextChangedEventArgs e)
        {
            ParticipantsViewModel participant = (ParticipantsViewModel)this.DataContext;
            TextBox txt = (TextBox)sender;

            if(txt.Text != "")
            {
                participant.SessionId = Int32.Parse(txt.Text);                
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            var resource = this.FindResource("Participants");

            if (resource != null)
            {
                string xmlFilename = @"C:\Users\anton\source\repos\NewBess\NewBess\bin\x64\Debug\Participants.xml";

                // System.Configuration.ConfigurationManager.AppSettings["xmlSessionsFile"];
                dataProvider = new XmlDataProvider();
                dataProvider.Source = new Uri(@xmlFilename);
                (resource as XmlDataProvider).Source = dataProvider.Source;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ParticipantsViewModel participant = (ParticipantsViewModel)this.DataContext;
            XmlDataProvider resource = this.FindResource("Participants") as XmlDataProvider;

            if(resource != null)
            {
                System.Collections.ICollection participants = (System.Collections.ICollection) resource.Data;

                foreach(XmlNode xn in participants)
                {
                    if (Int32.Parse(xn.Attributes["Id"].Value) == participant.SessionId)
                    {
                        participant.ParticipantName = xn.Attributes["Name"].Value;
                        break;
                    }
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FacebookAnalyzer
{
    /// <summary>
    /// Interaction logic for StartScreen.xaml
    /// </summary>
    public partial class StartScreen : UserControl
    {
        public StartScreen()
        {
            InitializeComponent();

            for(int i=0;i<AppData.data.recentLogs.Count;i++)
            {
                string rcLog = AppData.data.recentLogs[i];
                Button recentLogButton = new Button();
                recentLogButton.Content = rcLog;
                recentLogButton.DataContext = rcLog;
                recentLogButton.Height = 30;
                recentLogButton.Margin = new Thickness(5);
                recentLogButton.Background = new SolidColorBrush(Colors.White);
                recentLogButton.Click += RecentLogButton_Click;
                logsPanel.Children.Add(recentLogButton);
            }
        }

        private void RecentLogButton_Click(object sender, RoutedEventArgs e)
        {
            string location = (string)(sender as Button).DataContext;
            OpenLog(location);
        }

        private void addLog_Click(object sender, RoutedEventArgs e)
        {
            //prompt
            using (var fbd = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult result = fbd.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    OpenLog(fbd.SelectedPath);
                    return;
                }

                if (result != System.Windows.Forms.DialogResult.OK)
                    return;
            }

            System.Windows.Forms.MessageBox.Show("Invalid log location");
        }

        private void OpenLog(string location)
        {
            MainWindow.current.pushControl(new LoadingLogViewer(location));
        }
    }
}

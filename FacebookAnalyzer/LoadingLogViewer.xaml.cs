using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
    /// Interaction logic for LoadingLogViewer.xaml
    /// </summary>
    public partial class LoadingLogViewer : UserControl
    {
        string location;

        Thread loadThread;
        System.Timers.Timer updateTicker;

        FaceLog log;

        bool finished = false;

        public LoadingLogViewer(string location)
        {
            this.location = location;
            InitializeComponent();


            log = FaceLog.LoadLogAssynchronous(location);

            updateTicker = new System.Timers.Timer();
            updateTicker.Interval = 50;
            updateTicker.AutoReset = true;
            updateTicker.Elapsed += UpdateTicker_Elapsed;
            updateTicker.Start();
        }

        private void UpdateTicker_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(UpdateStatus));
        }

        public void UpdateStatus()
        {
            //Check graphics
            progress.Minimum = 0;
            progress.Maximum = log.threadFileCount;
            progress.Value = log.threadsProcessed;

            //If over, fire up engines
            if(log.loadFinished)
            {
                updateTicker.Stop();
                Dispatcher.BeginInvoke(new Action(OpenLog));
            }
        }

        public void OpenLog()
        {
            MainWindow.current.popControl();
            MainWindow.current.pushControl(new LogViewer(log));
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
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
    /// Interaction logic for LogViewer.xaml
    /// </summary>
    public partial class LogViewer : UserControl
    {
        FaceLog log;

        TimelineDataController dataController;
        TimelineViewer timeViewer;

        public LogViewer(FaceLog log)
        {
            this.log = log;
            InitializeComponent();

            selfLabel.Content = log.owner.name;

            SolidColorBrush threadColor = new SolidColorBrush(Colors.LightCyan);
            for(int i=0;i<log.threads.Count;i++)
            {
                ChatThread thread = log.threads[i];
                if (thread.messages.Count >= 100 || (thread.people.Count == 3 && thread.messages.Count >= 50) )
                {
                    Button threadBut = new Button();

                    string names = "";
                    for (int k = 0; k < thread.people.Count; k++)
                        if (thread.people[k] != log.owner)
                            names += thread.people[k] + "\n";
                    names = names.Substring(0, names.Length - 1);

                    threadBut.Content = names;
                    threadBut.DataContext = i;
                    threadBut.Background = threadColor;
                    threadBut.Click += ThreadBut_Click;

                    threadBut.Margin = new Thickness(1);

                    peoplePanel.Children.Add(threadBut);
                }
            }

            dataController = new TimelineDataController(log.allMessages, log.beginScope, log.endScope, log.owner);

            timeViewer = new TimelineViewer(log.allMessages, log.beginScope, log.endScope, log.owner, dataController);
            timelineControl.Content = timeViewer;

            Timer updateTicker = new Timer();
            updateTicker.Interval = 50;
            updateTicker.AutoReset = true;
            updateTicker.Elapsed += UpdateTicker_Elapsed; ;
            updateTicker.Start();
        }

        private void UpdateTicker_Elapsed(object sender, ElapsedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(UpdateGraphics));
        }

        private void UpdateGraphics()
        {
            //Owner Volumetric
            ownerCharCountLabel.Content = dataController.ownerCharCount;
            ownerWordCountLabel.Content = dataController.ownerWordCount;
            ownerMessageCountLabel.Content = dataController.ownerMessageCount;

            //Other Volumetric
            otherCharCountLabel.Content = dataController.otherCharCount;
            otherWordCountLabel.Content = dataController.otherWordCount;
            otherMessageCountLabel.Content = dataController.otherMessageCount;

            //Owner Form
            ownerAvgWordSizeLabel.Content = ("" + Math.Round(dataController.ownerCharCount / (double)dataController.ownerWordCount,5));
            ownerAvgMessageSizeLabel.Content = ("" + Math.Round(dataController.ownerCharCount / (double)dataController.ownerMessageCount, 5));
            ownerCommaCountLabel.Content = dataController.ownerCommaCount;

            //Other Form
            otherAvgWordSizeLabel.Content = ("" + Math.Round(dataController.otherCharCount / (double)dataController.otherWordCount, 5));
            otherAvgMessageSizeLabel.Content = ("" + Math.Round(dataController.otherCharCount / (double)dataController.otherMessageCount, 5));
            otherCommaCountLabel.Content = dataController.otherCommaCount;

            //Owner Style
            ownerQuestionCountLabel.Content = dataController.ownerQuestionCount;
            ownerExclamationCountLabel.Content = dataController.ownerCommaCount;
            ownerLinkCountLabel.Content = dataController.ownerLinkCount;

            //Other Style
            otherQuestionCountLabel.Content = dataController.otherQuestionCount;
            otherExclamationCountLabel.Content = dataController.otherCommaCount;
            otherLinkCountLabel.Content = dataController.otherLinkCount;
        }

        private void ThreadBut_Click(object sender, RoutedEventArgs e)
        {
            ChatThread threadToView = log.threads[(int)(sender as Button).DataContext];
            MainWindow.current.pushControl(new ThreadViewer(log, threadToView));
        }
    }
}

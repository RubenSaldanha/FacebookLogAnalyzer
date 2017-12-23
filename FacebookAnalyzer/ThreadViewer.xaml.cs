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
    /// Interaction logic for ThreadViewer.xaml
    /// </summary>
    public partial class ThreadViewer : UserControl
    {
        FaceLog log;
        ChatThread chat;
        TimelineViewer timeViewer;

        public ThreadViewer(FaceLog log, ChatThread chat)
        {
            this.log = log;
            this.chat = chat;
            InitializeComponent();

            for(int i=0;i<chat.messages.Count;i++)
            {
                if (i >= 100)
                    break;

                Message msg = chat.messages[i];

                Label labelMsg = new Label();

                labelMsg.Content = msg.author.ToString() + " : " + msg.text;

                chatPanel.Children.Add(labelMsg);
            }

            TimelineDataController dataController = new TimelineDataController(chat.messages, log.beginScope, log.endScope, log.owner);
            timeViewer = new TimelineViewer(chat.messages, log.beginScope, log.endScope, log.owner, dataController);
            timelineControl.Content = timeViewer;
        }

        private void chatExporterButton_Click(object sender, RoutedEventArgs e)
        {
            using (StreamWriter file =
            new StreamWriter("chatExport.txt"))
            {
                for (int i = 0; i < chat.messages.Count; i++)
                {
                    if(chat.messages[i].author != log.owner)
                        file.WriteLine(chat.messages[i].text);
                }
                
            }
        }
    }
}

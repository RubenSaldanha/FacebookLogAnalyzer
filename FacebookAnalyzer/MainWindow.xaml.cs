using HtmlAgilityPack;
using System;
using System.Collections.Generic;
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
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static MainWindow current;

        List<UserControl> controlList;

        FaceLog log;

        public MainWindow()
        {
            current = this;
            InitializeComponent();

            AppData.Load();

            Closing += MainWindow_Closing;

            controlList = new List<UserControl>();

            pushControl(new StartScreen());
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            AppData.Save();
        }

        public void pushControl(UserControl control)
        {
            controlList.Add(control);
            mainControl.Content = control;
        }
        public void popControl()
        {
            controlList.RemoveAt(controlList.Count - 1);

            if (controlList.Count == 0)
                Application.Current.Shutdown(0);
            else
                mainControl.Content = controlList[controlList.Count - 1];
        }

        private void backButton_Click(object sender, RoutedEventArgs e)
        {
            popControl();
        }

        private void homeButton_Click(object sender, RoutedEventArgs e)
        {
            while (controlList.Count >= 1)
                popControl();
        }
    }
}

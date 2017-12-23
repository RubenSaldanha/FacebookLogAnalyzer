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
    public enum TimeMode { Linear, Year, WeekDay, HourMinute }

    /// <summary>
    /// Interaction logic for TimelineViewer.xaml
    /// </summary>
    public partial class TimelineViewer : UserControl
    {
        Person owner;

        List<PlotGraph> plotsGraphics;
        List<PlotGraph> selfPlotsGraphics;


        Timer updateTicker;

        List<Message> baseMessages;

        //Base scope
        DateTime baseTimeStart;
        DateTime baseTimeEnd;

        long beginTimeScope;
        long endTimeScope;

        List<TimeBounds> boundStack;

        string textFinder;

        public enum AnimationStatus { Idle, ZoomIn, ZoomOut, Split }
        AnimationStatus animation;
        double animationFactor;

        TimeMode timeMode;

        TimelineDataController binner;

        //Interface
        Button splitBut;
        Button linearTimeBut;
        Button yearTimeBut;
        Button weekTimeBut;
        Button hourTimeBut;
        Label timeOverviewLabel;
        Label textFinderLabel;
        TextBox textFinderBox;

        //Cosmetics
        SolidColorBrush timeModeInnactive;
        SolidColorBrush timeModeActive;
        Rectangle PlotAreaRect;
        Rectangle ZoomRect;

        //Area definition
        GraphicArea AbovePlotArea;
        GraphicArea AxisArea;
        GraphicArea PlotArea;
        GraphicArea XLabelArea;
        GraphicArea TimeModeArea;
        //Bot left area
        GraphicArea WordSoupArea;

        List<Label> labels;
        List<Line> axisMarkers;

        List<Label> wordSoupLabels;

        public TimelineViewer(List<Message> messages, DateTime baseTimeStart, DateTime baseTimeEnd, Person owner, TimelineDataController dataController)
        {
            this.baseMessages = messages;
            this.baseTimeStart = baseTimeStart;
            this.baseTimeEnd = baseTimeEnd;
            this.owner = owner;

            binner = dataController;

            InitializeComponent();

            mainCanvas.ClipToBounds = true;
            mainCanvas.MouseLeftButtonDown += MainCanvas_MouseLeftButtonDown;
            mainCanvas.MouseRightButtonDown += MainCanvas_MouseRightButtonDown;
            mainCanvas.MouseMove += MainCanvas_MouseMove;
            mainCanvas.MouseWheel += MainCanvas_MouseWheel;

            //Create base structure
            InitializeStructure();
            InitializeInterfaces();
            InitializeCosmetics();

            //Add one measure
            addMeasure(0);

            //Update base data
            UpdateData();

            //Begin updates
            updateTicker.Start();
        }

        private void MainCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta == 0)
                return;

            if(e.Delta > 0)
            {
                binner.PanRight();
            }
            else
            {
                binner.PanLeft();
            }
        }

        private void MainCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            Point pos = e.GetPosition(sender as IInputElement);

            if(PlotArea.isInside(pos))
            {
                double axisFactor = (pos.X - PlotArea.Left) / PlotArea.width;
                Tuple<double, double> zoomBounds = binner.getPreviewBounds(axisFactor);
                double width = (zoomBounds.Item2 - zoomBounds.Item1) * PlotArea.width;
                double height = PlotArea.height;
                ZoomRect.Width = width;
                ZoomRect.Height = height;

                Canvas.SetLeft(ZoomRect, PlotArea.Left +  zoomBounds.Item1 * PlotArea.width);
                Canvas.SetTop(ZoomRect, PlotArea.Top);

                ZoomRect.Visibility = Visibility.Visible;
            }
            else
            {
                ZoomRect.Visibility = Visibility.Hidden;
            }
        }

        private void MainCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point pos = e.GetPosition(sender as IInputElement);
            pos = new Point(pos.X - PlotArea.topLeftX, pos.Y - PlotArea.topLeftY);
            double xFactor = pos.X / PlotArea.width;

            binner.ZoomIn(xFactor);
        }

        private void MainCanvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            binner.ZoomOut();
        }

        private void InitializeStructure()
        {
            plotsGraphics = new List<PlotGraph>();
            selfPlotsGraphics = new List<PlotGraph>();
            boundStack = new List<TimeBounds>();

            wordSoupLabels = new List<Label>();

            //Create update ticker
            updateTicker = new Timer();
            updateTicker.Interval = 50;
            updateTicker.AutoReset = true;
            updateTicker.Elapsed += UpdateTicker_Elapsed;
        }

        private void InitializeInterfaces()
        {
            //Create Time Mode Buttons
            //Split But
            splitBut = new Button();
            splitBut.Content = "Split";
            splitBut.Click += SplitBut_Click;
            mainCanvas.Children.Add(splitBut);

            //TimeMode buttons
            linearTimeBut = new Button();
            linearTimeBut.Width = 50;
            linearTimeBut.Height = 50;
            linearTimeBut.Content = "-->";
            linearTimeBut.Margin = new Thickness(1);
            linearTimeBut.Click += linearTimeBut_Click;
            mainCanvas.Children.Add(linearTimeBut);

            yearTimeBut = new Button();
            yearTimeBut.Width = 50;
            yearTimeBut.Height = 50;
            yearTimeBut.Content = "Year";
            yearTimeBut.Margin = new Thickness(1);
            yearTimeBut.Click += yearTimeBut_Click;
            mainCanvas.Children.Add(yearTimeBut);

            weekTimeBut = new Button();
            weekTimeBut.Width = 50;
            weekTimeBut.Height = 50;
            weekTimeBut.Content = "Week";
            weekTimeBut.Margin = new Thickness(1);
            weekTimeBut.Click += weekTimeBut_Click;
            mainCanvas.Children.Add(weekTimeBut);

            hourTimeBut = new Button();
            hourTimeBut.Width = 50;
            hourTimeBut.Height = 50;
            hourTimeBut.Content = "Hour";
            hourTimeBut.Margin = new Thickness(1);
            hourTimeBut.Click += hourTimeBut_Click;
            mainCanvas.Children.Add(hourTimeBut);

            timeOverviewLabel = new Label();
            timeOverviewLabel.Content = "Time Overview";
            timeOverviewLabel.FontSize = 16;
            mainCanvas.Children.Add(timeOverviewLabel);

            textFinderLabel = new Label();
            textFinderLabel.Content = "Filter: ";
            textFinderLabel.Width = 40;
            textFinderLabel.VerticalAlignment = VerticalAlignment.Center;
            textFinderLabel.HorizontalAlignment = HorizontalAlignment.Right;
            mainCanvas.Children.Add(textFinderLabel);
            textFinderLabel.Visibility = Visibility.Collapsed;

            textFinderBox = new TextBox();
            textFinderBox.Width = 100;
            textFinderBox.VerticalAlignment = VerticalAlignment.Center;
            textFinderBox.HorizontalAlignment = HorizontalAlignment.Right;
            textFinderBox.TextChanged += TextFinderBox_TextChanged;
            mainCanvas.Children.Add(textFinderBox);
            textFinderBox.Visibility = Visibility.Collapsed;
        }

        private void SplitBut_Click(object sender, RoutedEventArgs e)
        {
            binner.SetSplitMode(!binner.GetSplitMode());
        }

        private void InitializeCosmetics()
        {
            //TimeMode Options
            timeModeActive = new SolidColorBrush(Colors.LightSkyBlue);
            timeModeInnactive = new SolidColorBrush(Colors.LightGray);

            PlotAreaRect = new Rectangle();
            PlotAreaRect.Fill = new SolidColorBrush(Colors.White);
            mainCanvas.Children.Add(PlotAreaRect);

            ZoomRect = new Rectangle();
            Color zoomRectFillColor = new Color();
            zoomRectFillColor.A = 128;
            zoomRectFillColor.R = 45;
            zoomRectFillColor.G = 113;
            zoomRectFillColor.B = 229;
            zoomRectFillColor = Colors.CadetBlue;
            zoomRectFillColor.A = 128;
            ZoomRect.Fill = new SolidColorBrush(zoomRectFillColor);
            ZoomRect.Stroke = new SolidColorBrush(Colors.LightCyan);
            ZoomRect.StrokeThickness = 3;
            mainCanvas.Children.Add(ZoomRect);
            Canvas.SetZIndex(ZoomRect, 1);

            //Initialize label structure
            labels = new List<Label>();
            axisMarkers = new List<Line>();
        }

        public void ChangeTimeMode(TimeMode newMode)
        {
            timeMode = newMode;

            binner.ChangeTimeMode(newMode);

            UpdateData();
        }

        public void addMeasure(int index)
        {
            binner.addMeasure(index);

            //Create graphical object associated with measure
            PlotGraph measureGraph = new PlotGraph();
            plotsGraphics.Add(measureGraph);

            //Create polygon associated with graphical object
            Polygon plotPoly = new Polygon();
            plotPoly.Stroke = new SolidColorBrush(Colors.Black);
            plotPoly.Fill = new SolidColorBrush(Colors.LightBlue);
            plotPoly.StrokeThickness = 2;
            measureGraph.plotPoly = plotPoly;
            mainCanvas.Children.Add(plotPoly);

            if(binner.GetSplitMode())
            {
                //Create self plotgraph
                PlotGraph selfMeasureGraph = new PlotGraph();
                selfPlotsGraphics.Add(selfMeasureGraph);

                //Create poly
                plotPoly = new Polygon();
                plotPoly.Stroke = new SolidColorBrush(Colors.Black);
                plotPoly.Fill = new SolidColorBrush(Colors.LightCoral);
                plotPoly.StrokeThickness = 2;
                selfMeasureGraph.plotPoly = plotPoly;
                mainCanvas.Children.Add(plotPoly);
            }

            binner.UpdateData();
        }
        public void removeMeasure(int measure)
        {
            binner.removeMeasure(measure);

            List<int> measures = binner.measures;

            for(int i=0;i<measures.Count;i++)
            {
                if(measures[i] == measure)
                {
                    //Remove Polygons
                    mainCanvas.Children.Remove(plotsGraphics[i].plotPoly);

                    //Remove graphics
                    plotsGraphics.RemoveAt(i);


                    //Remove selfData
                    if (binner.GetSplitMode())
                    {
                        //Remove Polygon
                        mainCanvas.Children.Remove(selfPlotsGraphics[i].plotPoly);

                        //remove selfPlotGraphics
                        plotsGraphics.RemoveAt(i);
                    }

                    break;
                }
            }

            UpdateData();
        }

        private void TextFinderBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            textFinder = textFinderBox.Text;
            UpdateData();
        }

        public void UpdateData()
        {
            binner.UpdateData();
        }

        private void UpdateTicker_Elapsed(object sender, ElapsedEventArgs e)
        {
            //Update();
            Action aa = new Action(Update);
            MainWindow.current.Dispatcher.BeginInvoke(aa);
        }
        private void Update()
        {
            TimeBounds originalBounds;
            TimeBounds targetBounds;
            switch (animation)
            {
                case AnimationStatus.Idle:
                    break;
                case AnimationStatus.ZoomIn:
                    animationFactor += 0.1;

                    if (animationFactor >= 1)
                        animationFactor = 1;

                    originalBounds = boundStack[boundStack.Count - 2];
                    targetBounds = boundStack[boundStack.Count - 1];

                    beginTimeScope = (long)(originalBounds.start * (1 - animationFactor) + targetBounds.start * animationFactor);
                    endTimeScope = (long)(originalBounds.end * (1 - animationFactor) + targetBounds.end * animationFactor);

                    UpdateData();

                    if (animationFactor == 1)
                        animation = AnimationStatus.Idle;
                    break;
                case AnimationStatus.ZoomOut:
                    break;
                case AnimationStatus.Split:
                    throw new NotImplementedException();
                    break;
            }

            UpdateGraphics();
        }
        private void UpdateGraphics()
        {
            double canvasHeight = mainCanvas.ActualHeight;
            double canvasWidth = mainCanvas.ActualWidth;
            Point bottomLeft = new Point(0, mainCanvas.ActualHeight);

            double leftColumnWidth = Math.Min(200, canvasWidth / 4);
            double PlotAxisMargin = 10;
            AbovePlotArea = new GraphicArea(leftColumnWidth, 0, canvasWidth - leftColumnWidth, 26);
            AxisArea = new GraphicArea(0, AbovePlotArea.Bottom, leftColumnWidth, 7 * canvasHeight / 12);
            PlotArea = new GraphicArea(AxisArea.Right + PlotAxisMargin, AbovePlotArea.Bottom, canvasWidth - AxisArea.width, AxisArea.height);
            XLabelArea = new GraphicArea(PlotArea.Left, PlotArea.Bottom, canvasWidth - AxisArea.width, 50);
            TimeModeArea = new GraphicArea(0,AxisArea.Bottom, AxisArea.width, XLabelArea.height);
            //Bot left area
            WordSoupArea = new GraphicArea(XLabelArea.Left, XLabelArea.Bottom, XLabelArea.width, canvasHeight - XLabelArea.Bottom);

            Canvas.SetLeft(PlotAreaRect, PlotArea.Left);
            Canvas.SetTop(PlotAreaRect, PlotArea.Top);
            PlotAreaRect.Width = PlotArea.width;
            PlotAreaRect.Height = PlotArea.height;

            //Show draw zones only
            if (false)
            {
                mainCanvas.Children.Clear();
                Rectangle a1 = new Rectangle();
                a1.Fill = new SolidColorBrush(Colors.Blue);
                a1.Width = AxisArea.width;
                a1.Height = AxisArea.height;
                mainCanvas.Children.Add(a1);
                Canvas.SetTop(a1, AxisArea.Top);
                Canvas.SetLeft(a1, AxisArea.Left);

                a1 = new Rectangle();
                a1.Fill = new SolidColorBrush(Colors.Red);
                a1.Width = PlotArea.width;
                a1.Height = PlotArea.height;
                //mainCanvas.Children.Add(a1);
                Canvas.SetTop(a1, PlotArea.Top);
                Canvas.SetLeft(a1, PlotArea.Left);

                a1 = new Rectangle();
                a1.Fill = new SolidColorBrush(Colors.Orange);
                a1.Width = XLabelArea.width;
                a1.Height = XLabelArea.height;
                mainCanvas.Children.Add(a1);
                Canvas.SetTop(a1, XLabelArea.Top);
                Canvas.SetLeft(a1, XLabelArea.Left);

                a1 = new Rectangle();
                a1.Fill = new SolidColorBrush(Colors.Purple);
                a1.Width = WordSoupArea.width;
                a1.Height = WordSoupArea.height;
                mainCanvas.Children.Add(a1);
                Canvas.SetTop(a1, WordSoupArea.Top);
                Canvas.SetLeft(a1, WordSoupArea.Left);

                a1 = new Rectangle();
                a1.Fill = new SolidColorBrush(Colors.Pink);
                a1.Width = TimeModeArea.width;
                a1.Height = TimeModeArea.height;
                mainCanvas.Children.Add(a1);
                Canvas.SetTop(a1, TimeModeArea.Top);
                Canvas.SetLeft(a1, TimeModeArea.Left);

                //return;
            }
            
            //Plot Measure Plots
            //Detect graphical elements size descrepancies
            if(plotsGraphics.Count != binner.measures.Count)
            {
                int diff = binner.measures.Count - plotsGraphics.Count;

                if(diff > 0)
                {
                    while (binner.measures.Count != plotsGraphics.Count)
                    {
                        //Create graphical object associated with measure
                        PlotGraph measureGraph = new PlotGraph();
                        plotsGraphics.Add(measureGraph);

                        //Create polygon associated with graphical object
                        Polygon plotPoly = new Polygon();
                        plotPoly.Stroke = new SolidColorBrush(Colors.Black);
                        plotPoly.Fill = new SolidColorBrush(Colors.LightBlue);
                        plotPoly.StrokeThickness = 2;
                        measureGraph.plotPoly = plotPoly;
                        mainCanvas.Children.Add(plotPoly);
                    }
                }
                else
                {
                    while (binner.measures.Count != plotsGraphics.Count)
                    {
                        //Remove Polygons
                        mainCanvas.Children.Remove(plotsGraphics[0].plotPoly);

                        //Remove graphics
                        plotsGraphics.RemoveAt(0);
                    }
                }
            }
            //Detect self graphical elements size descrepancies
            if(binner.GetSplitMode())
            {
                if (selfPlotsGraphics.Count != binner.measures.Count)
                {
                    int diff = binner.measures.Count - selfPlotsGraphics.Count;

                    if (diff > 0)
                    {
                        while (binner.measures.Count != selfPlotsGraphics.Count)
                        {
                            //Create graphical object associated with measure
                            PlotGraph measureGraph = new PlotGraph();
                            selfPlotsGraphics.Add(measureGraph);

                            //Create polygon associated with graphical object
                            Polygon plotPoly = new Polygon();
                            plotPoly.Stroke = new SolidColorBrush(Colors.Black);
                            plotPoly.Fill = new SolidColorBrush(Colors.Green);
                            plotPoly.StrokeThickness = 2;
                            measureGraph.plotPoly = plotPoly;
                            mainCanvas.Children.Add(plotPoly);
                        }
                    }
                    else
                    {
                        while (binner.measures.Count != selfPlotsGraphics.Count)
                        {
                            //Remove Polygons
                            mainCanvas.Children.Remove(selfPlotsGraphics[0].plotPoly);

                            //Remove graphics
                            selfPlotsGraphics.RemoveAt(0);
                        }
                    }
                }
            }

            //Update plot graphics
            double pace = PlotArea.width / (binner.binCount);
            double multiplier;
            TimelineDataController.PlotData data, selfData;
            PlotGraph graph, selfGraph;
            double axisHeight;
            PointCollection points;
            for (int m=0;m<binner.measures.Count;m++)
            {
                data = binner.plotsData[m];
                selfData = null;
                graph = plotsGraphics[m];

                if (!binner.GetSplitMode())
                {
                    axisHeight = 0;
                    multiplier = PlotArea.height / data.max;
                }
                else
                {
                    selfData = binner.selfPlotsData[m];

                    axisHeight = PlotArea.height * selfData.max / (data.max + selfData.max);
                    multiplier = PlotArea.height / (data.max + selfData.max);
                }

                points = new PointCollection();
                points.Add(new Point(PlotArea.Right, PlotArea.Bottom - axisHeight));
                points.Add(new Point(PlotArea.Left, PlotArea.Bottom - axisHeight));

                for (int b = 0; b < binner.binCount; b++)
                {
                    //Bin
                    points.Add(new Point(binner.binDefinitions[b].axisStart * PlotArea.width + PlotArea.Left, -(data.binValues[b] * multiplier) + PlotArea.Bottom -axisHeight));
                    points.Add(new Point(binner.binDefinitions[b].axisEnd * PlotArea.width + PlotArea.Left, -(data.binValues[b] * multiplier) + PlotArea.Bottom - axisHeight));

                    //Linear
                    //points.Add(new Point(binner.binDefinitions[b].axisStart * PlotArea.width + pace/2 + PlotArea.Left, -(data.binValues[b] * multiplier) + PlotArea.Bottom));
                }
                graph.plotPoly.Points = points;

                if(binner.GetSplitMode())
                {
                    selfGraph = selfPlotsGraphics[m];

                    points = new PointCollection();
                    points.Add(new Point(PlotArea.Right, PlotArea.Bottom - axisHeight));
                    points.Add(new Point(PlotArea.Left, PlotArea.Bottom - axisHeight));

                    for (int b = 0; b < binner.binCount; b++)
                    {
                        //Bin
                        points.Add(new Point(binner.binDefinitions[b].axisStart * PlotArea.width + PlotArea.Left, (selfData.binValues[b] * multiplier) + PlotArea.Bottom -axisHeight));
                        points.Add(new Point(binner.binDefinitions[b].axisEnd * PlotArea.width + PlotArea.Left, (selfData.binValues[b] * multiplier) + PlotArea.Bottom -axisHeight));

                        //Linear
                        //points.Add(new Point(binner.binDefinitions[b].axisStart * PlotArea.width + pace/2 + PlotArea.Left, -(data.binValues[b] * multiplier) + PlotArea.Bottom));
                    }
                    selfGraph.plotPoly.Points = points;
                }
            }

            //Update Top Controls
            //Split
            Canvas.SetLeft(splitBut, 0);
            Canvas.SetTop(splitBut, 0);
            splitBut.Content = binner.GetSplitMode() ? "All" : "Split";
            splitBut.Width = AbovePlotArea.Left;
            splitBut.Height = AxisArea.Top;

            //TimeOverviewer
            Canvas.SetLeft(timeOverviewLabel, AbovePlotArea.Left);
            Canvas.SetTop(timeOverviewLabel, AbovePlotArea.Top);
            timeOverviewLabel.Content = binner.timeOverview;

            //Text Finder
            Canvas.SetLeft(textFinderBox, AbovePlotArea.Right - 100);
            Canvas.SetTop(textFinderBox, AbovePlotArea.Top);
            Canvas.SetLeft(textFinderLabel, AbovePlotArea.Right - 100 - 40);
            Canvas.SetTop(textFinderLabel, AbovePlotArea.Top);

            //Update TimeMode Buttons Position
            double buttonSize = TimeModeArea.width / 4;
            Canvas.SetLeft(linearTimeBut, TimeModeArea.Left);
            Canvas.SetTop(linearTimeBut, TimeModeArea.Top);
            linearTimeBut.Width = buttonSize;
            Canvas.SetLeft(yearTimeBut, TimeModeArea.Left + buttonSize * 1);
            Canvas.SetTop(yearTimeBut, TimeModeArea.Top);
            yearTimeBut.Width = buttonSize;
            Canvas.SetLeft(weekTimeBut, TimeModeArea.Left + buttonSize * 2);
            Canvas.SetTop(weekTimeBut, TimeModeArea.Top);
            weekTimeBut.Width = buttonSize;
            Canvas.SetLeft(hourTimeBut, TimeModeArea.Left + buttonSize * 3);
            Canvas.SetTop(hourTimeBut, TimeModeArea.Top);
            hourTimeBut.Width = buttonSize;

            //Spread some labels around
            //Clear labels
            for (int i = 0; i < labels.Count; i++)
                mainCanvas.Children.Remove(labels[i]);
            labels.Clear();

            for (int i = 0; i < axisMarkers.Count; i++)
                mainCanvas.Children.Remove(axisMarkers[i]);
            axisMarkers.Clear();

            //Axis labels
            List<TimelineDataController.LabelDefinition> axisLabels = binner.getTimeAxisLabels();

            for(int i=0;i<axisLabels.Count;i++)
            {
                TimelineDataController.LabelDefinition labelData = axisLabels[i];

                if(labelData.axisFactor >= 0 && labelData.axisFactor <= 1)
                {
                    double xx = PlotArea.Left + PlotArea.width * labelData.axisFactor;
                    Label newLabel = new Label();
                    newLabel.Width = 100;
                    newLabel.Content = labelData.mainText;
                    newLabel.HorizontalContentAlignment = HorizontalAlignment.Center;
                    Canvas.SetTop(newLabel, PlotArea.Bottom);
                    Canvas.SetLeft(newLabel, xx - 50);
                    mainCanvas.Children.Add(newLabel);
                    labels.Add(newLabel);

                    if(labelData.subText != "")
                    {
                        newLabel = new Label();
                        newLabel.Width = 100;
                        newLabel.Content = labelData.subText;
                        newLabel.HorizontalContentAlignment = HorizontalAlignment.Center;
                        Canvas.SetTop(newLabel, PlotArea.Bottom + XLabelArea.height/4);
                        Canvas.SetLeft(newLabel, xx - 50);
                        mainCanvas.Children.Add(newLabel);
                        labels.Add(newLabel);
                    }

                    Line newMarker = new Line();
                    newMarker.Stroke = new SolidColorBrush(Colors.Black);
                    newMarker.StrokeThickness = 2;
                    newMarker.X1 = xx;
                    newMarker.Y1 = PlotArea.Bottom - 6;
                    newMarker.X2 = xx;
                    newMarker.Y2 = PlotArea.Bottom + 6;
                    mainCanvas.Children.Add(newMarker);
                    axisMarkers.Add(newMarker);
                }
            }


            //WordSoup
            for (int i = 0; i < wordSoupLabels.Count; i++)
                mainCanvas.Children.Remove(wordSoupLabels[i]);
            wordSoupLabels.Clear();

            List<WordSoupMeta> wordMeta = new List<WordSoupMeta>();
            int wordSoupCount = 5;
            double accumulated = 0;
            for(int i=0;i<wordSoupCount && i< binner.lexicAnomalies.Count;i++)
            {
                TimelineDataController.LexicPresence presence = binner.lexicAnomalies[i];
                WordSoupMeta meta = new WordSoupMeta(presence.word, presence.weight, presence.placement);

                wordMeta.Add(meta);
                accumulated += meta.weight;
            }

            double orderAcum = 0;
            for(int i=0;i<wordMeta.Count;i++)
            {
                WordSoupMeta wmeta = wordMeta[i];
                Label lbl = new Label();
                lbl.Content = wmeta.word;
                lbl.HorizontalContentAlignment = HorizontalAlignment.Center;
                lbl.VerticalContentAlignment = VerticalAlignment.Center;
                double cS = (wmeta.weight / accumulated);
                lbl.Background = new SolidColorBrush(Colors.CornflowerBlue);
                lbl.BorderBrush = new SolidColorBrush(Colors.Blue);
                double hh = cS * WordSoupArea.height;
                lbl.Height = hh;
                double ww = cS * WordSoupArea.width;
                lbl.Width = ww;

                Canvas.SetTop(lbl, WordSoupArea.Top +  orderAcum * WordSoupArea.height);
                Canvas.SetLeft(lbl, WordSoupArea.Left + wmeta.axis * WordSoupArea.width - ww / 2);

                orderAcum += cS;

                wordSoupLabels.Add(lbl);
                mainCanvas.Children.Add(lbl);
            }

        }

        public class WordSoupMeta
        {
            public string word;
            public double weight;
            public double axis;

            public WordSoupMeta(string word, double weight, double axis)
            {
                this.word = word;
                this.weight = weight;
                this.axis = axis;
            }
        }
        
        private void linearTimeBut_Click(object sender, RoutedEventArgs e)
        {
            InactivateTimeModeCosmetics();
            ChangeTimeMode(TimeMode.Linear);
            linearTimeBut.Background = timeModeActive;
        }
        private void yearTimeBut_Click(object sender, RoutedEventArgs e)
        {
            InactivateTimeModeCosmetics();
            ChangeTimeMode(TimeMode.Year);
            yearTimeBut.Background = timeModeActive;
        }
        private void weekTimeBut_Click(object sender, RoutedEventArgs e)
        {
            InactivateTimeModeCosmetics();
            ChangeTimeMode(TimeMode.WeekDay);
            weekTimeBut.Background = timeModeActive;
        }
        private void hourTimeBut_Click(object sender, RoutedEventArgs e)
        {
            InactivateTimeModeCosmetics();
            ChangeTimeMode(TimeMode.HourMinute);
            hourTimeBut.Background = timeModeActive;
        }
        private void InactivateTimeModeCosmetics()
        {
            switch(timeMode)
            {
                case TimeMode.Linear:
                    linearTimeBut.Background = timeModeInnactive;
                    break;
                case TimeMode.Year:
                    yearTimeBut.Background = timeModeInnactive;
                    break;
                case TimeMode.WeekDay:
                    weekTimeBut.Background = timeModeInnactive;
                    break;
                case TimeMode.HourMinute:
                    hourTimeBut.Background = timeModeInnactive;
                    break;
            }
        }


        public struct GraphicArea
        {
            public double topLeftX;
            public double topLeftY;
            public double width;
            public double height;

            public double Left
            {
                get { return topLeftX; }
            }
            public double Right
            {
                get { return topLeftX + width; }
            }
            public double Top
            {
                get { return topLeftY; }
            }
            public double Bottom
            {
                get { return topLeftY + height; }
            }

            public GraphicArea(double topLeftX, double topLeftY, double width, double height)
            {
                if(width > 0)
                {
                    this.topLeftX = topLeftX;
                    this.width = width;
                }
                else
                {
                    this.topLeftX = topLeftX - width;
                    this.width = -width;
                }

                if(height > 0)
                {
                    this.topLeftY = topLeftY;
                    this.height = height;
                }
                else
                {
                    this.topLeftY = topLeftY - height;
                    this.height = -height;
                }
            }

            public bool isInside(Point point)
            {
                return point.X > Left && point.X < Right && point.Y < Bottom && point.Y > Top;
            }
            public Point getRelativeCoords(Point point)
            {
                return new Point(point.X - Left, point.Y - Top);
            }
            public Point getAbsoluteCoords(Point point)
            {
                return new Point(point.X + Left, point.Y + Top);
            }
        }

        public class PlotGraph
        {
            public Polygon plotPoly;

        }

        public class TimeBounds
        {
            public long start;
            public long end;

            public TimeBounds(long start, long end)
            {
                this.start = start;
                this.end = end;
            }
        }
    }
}

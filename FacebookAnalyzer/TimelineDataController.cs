using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FacebookAnalyzer
{
    public class TimelineDataController
    {
        Person owner;

        public int binCount;
        public List<int> measures;
        public BinDefinition[] binDefinitions;
        public List<PlotData> plotsData;

        bool selfSplitMode = false;
        public List<PlotData> selfPlotsData;

        List<Message> baseMessages;
        List<Person> peopleFilter;

        DateTime baseTimeStart;
        DateTime baseTimeEnd;

        long beginTimeScope;
        long endTimeScope;

        public int baseWordCount;
        public Dictionary<string, LexicPresence> baseLexic;

        public int currentWordCount;
        public Dictionary<string, LexicPresence> currentLexic;

        public List<LexicPresence> lexicAnomalies;

        public string timeOverview;

        public delegate bool BinTimeChecker(Message msg);
        BinTimeChecker boundChecker;

        TimeMode timeMode;

        public enum ZoomLevel { All , Year, Month , Week , Day, Hour }
        ZoomLevel zoom;

        public int ownerCharCount;
        public int otherCharCount;
        public int ownerWordCount;
        public int otherWordCount;
        public int ownerMessageCount;
        public int otherMessageCount;
        public int ownerCommaCount;
        public int otherCommaCount;
        public int ownerQuestionCount;
        public int otherQuestionCount;
        public int ownerExclamationCount;
        public int otherExclamationCount;
        public int ownerLinkCount;
        public int otherLinkCount;

        public TimelineDataController(List<Message> messages, DateTime baseTimeStart, DateTime baseTimeEnd, Person owner)
        {
            this.baseMessages = messages;
            this.baseTimeStart = baseTimeStart;
            this.baseTimeEnd = baseTimeEnd;
            this.owner = owner;

            InitializeStructure();

            //Compute base data
            ComputeBaseLexic();

            //Initialize Lexic structure
            currentLexic = new Dictionary<string, LexicPresence>();
            lexicAnomalies = new List<LexicPresence>();

            //Add one measure
            addMeasure(0); //Updates base data
        }

        private void InitializeStructure()
        {
            //Initialize binCount
            binCount = 60;

            //Compute initial bounds
            resetBounds();

            //Measures base
            measures = new List<int>();
            plotsData = new List<PlotData>();
            selfPlotsData = new List<PlotData>();
        }



        public void UpdateData()
        {
            //Reset scope measures
            ownerCharCount = 0;
            otherCharCount = 0;
            ownerWordCount = 0;
            otherWordCount = 0;
            ownerMessageCount = 0;
            otherMessageCount = 0;
            ownerCommaCount = 0;
            otherCommaCount = 0;
            ownerQuestionCount = 0;
            otherQuestionCount = 0;
            ownerExclamationCount = 0;
            otherExclamationCount = 0;
            ownerLinkCount = 0;
            otherLinkCount = 0;

            //Reset lexical measures
            currentLexic.Clear();
            currentWordCount = 0;

            //Helpful time objects
            TimeSpan fullYear = new TimeSpan(365, 0, 0, 0);
            TimeSpan fullWeek = new TimeSpan(7, 0, 0, 0);
            TimeSpan fullDay = new TimeSpan(24, 0, 0);

            //Create BoundChecker
            switch (timeMode)
            {
                case TimeMode.Linear:
                    boundChecker = (m) => m.time.Ticks > beginTimeScope && m.time.Ticks < endTimeScope;
                    break;
                case TimeMode.Year:
                    boundChecker = (m) => m.timeMeasures[0] > beginTimeScope && m.timeMeasures[0] < endTimeScope;
                    break;
                case TimeMode.WeekDay:
                    boundChecker = (m) => m.timeMeasures[1] > beginTimeScope && m.timeMeasures[1] < endTimeScope;
                    break;
                case TimeMode.HourMinute:
                    boundChecker = (m) => m.timeMeasures[2] > beginTimeScope && m.timeMeasures[2] < endTimeScope;
                    break;
            }

            //CreateBins
            binDefinitions = new BinDefinition[binCount];
            for (int i = 0; i < binCount; i++)
            {
                BinDefinition binDef = new BinDefinition();
                binDefinitions[i] = binDef;

                //find bins acording to zoom

                binDef.axisStart = i * (1.0 / binCount);
                binDef.axisEnd = (i + 1) * (1.0 / binCount);

                switch (timeMode)
                {
                    case TimeMode.Linear:
                        binDef.timeStart = beginTimeScope + i * (endTimeScope - beginTimeScope) / (binCount); //Integer division because of irrelevant sizes
                        binDef.timeEnd = beginTimeScope + (i + 1) * (endTimeScope - beginTimeScope) / (binCount); //Integer division because of irrelevant sizes

                        binDef.checker = (m) => m.time.Ticks > binDef.timeStart && m.time.Ticks < binDef.timeEnd;
                        break;
                    case TimeMode.Year:
                        binDef.timeStart = beginTimeScope + (long)(i * (endTimeScope - beginTimeScope) / (double)binCount);
                        binDef.timeEnd = beginTimeScope + (long)((i + 1) * (endTimeScope - beginTimeScope) / (double)binCount);

                        binDef.checker = (m) => m.timeMeasures[0] > binDef.timeStart && m.timeMeasures[0] < binDef.timeEnd;
                        break;
                    case TimeMode.WeekDay:
                        binDef.timeStart = beginTimeScope + (long)(i * (endTimeScope - beginTimeScope) / (double)binCount);
                        binDef.timeEnd = beginTimeScope + (long)((i + 1) * (endTimeScope - beginTimeScope) / (double)binCount);

                        binDef.checker = (m) => m.timeMeasures[1] > binDef.timeStart && m.timeMeasures[1] < binDef.timeEnd;
                        break;
                    case TimeMode.HourMinute:
                        binDef.timeStart = beginTimeScope + (long)(i * (endTimeScope - beginTimeScope) / (double)binCount);
                        binDef.timeEnd = beginTimeScope + (long)((i + 1) * (endTimeScope - beginTimeScope) / (double)binCount);

                        binDef.checker = (m) => m.timeMeasures[2] > binDef.timeStart && m.timeMeasures[2] < binDef.timeEnd;
                        break;
                }
            }

            //Synchronize data measures and output data structures
            if(plotsData.Count != measures.Count)
            {
                int diff = measures.Count - plotsData.Count;
                if(diff > 0)
                {
                    while(measures.Count != plotsData.Count)
                    {
                        plotsData.Add(new PlotData());
                    }
                }
                else
                {
                    while (measures.Count != plotsData.Count)
                        plotsData.RemoveAt(0);
                }
            }
            //Synchronize self data measures and output data structures
            if (selfSplitMode)
            {
                if (selfPlotsData.Count != measures.Count)
                {
                    int diff = measures.Count - selfPlotsData.Count;
                    if (diff > 0)
                    {
                        while (measures.Count != selfPlotsData.Count)
                        {
                            selfPlotsData.Add(new PlotData());
                        }
                    }
                    else
                    {
                        while (measures.Count != selfPlotsData.Count)
                            selfPlotsData.RemoveAt(0);
                    }
                }
            }

            //Reset data
            for (int i = 0; i < measures.Count; i++)
            {
                PlotData plotData = plotsData[i];

                plotData.measure = measures[i];

                if (plotData.binValues == null)
                    plotData.binValues = new double[binCount];
                if (plotData.binValues.Length != binCount)
                    plotData.binValues = new double[binCount];

                //Clear previous data
                for (int k = 0; k < plotData.binValues.Length; k++)
                    plotData.binValues[k] = 0;

                //Reset maximum value
                plotData.max = double.MinValue;


                //Reset self data
                if (selfSplitMode)
                {
                    plotData = selfPlotsData[i];

                    plotData.measure = measures[i];

                    if (plotData.binValues == null)
                        plotData.binValues = new double[binCount];
                    if (plotData.binValues.Length != binCount)
                        plotData.binValues = new double[binCount];

                    //Clear previous data
                    for (int k = 0; k < plotData.binValues.Length; k++)
                        plotData.binValues[k] = 0;

                    //Reset maximum value
                    plotData.max = double.MinValue;
                }
            }

            //process messsages
            Message msg;
            LexicPresence presence;
            int bin = 0;
            for (int k = 0; k < baseMessages.Count; k++)
            {
                msg = baseMessages[k];

                //Skip message if author not on people filter
                if (peopleFilter != null)
                    if (!peopleFilter.Contains(msg.author))
                        continue;


                //Skip message if outside time bounds
                if (!boundChecker(msg))
                    continue;

                //Compute Full scope measures
                if(msg.author == owner)
                {
                    ownerCharCount += (int)msg.measures[0];
                    ownerWordCount += (int)msg.measures[1];
                    ownerMessageCount++;
                    ownerCommaCount += (int)msg.measures[5];
                    ownerQuestionCount += (int)msg.measures[2];
                    ownerExclamationCount += (int)msg.measures[3];
                    ownerLinkCount += (int)msg.measures[6];
                }
                else
                {
                    otherCharCount += (int)msg.measures[0];
                    otherWordCount += (int)msg.measures[1];
                    otherMessageCount++;
                    otherCommaCount += (int)msg.measures[5];
                    otherQuestionCount += (int)msg.measures[2];
                    otherExclamationCount += (int)msg.measures[3];
                    otherLinkCount += (int)msg.measures[6];
                }


                //TODO Check first if bin changed optimization
                //Find aggregation bin
                for (int b = 0; b < binCount; b++)
                {
                    if (binDefinitions[b].checker(msg))
                    {
                        bin = b;
                        break;
                    }
                }

                //Compute lexic
                string[] words = msg.text.Split(' ');
                currentWordCount += words.Length;
                for (int w = 0; w < words.Length; w++)
                {
                    string word = words[w];
                    if (currentLexic.ContainsKey(word))
                    {
                        presence = currentLexic[word];
                        presence.count++;
                        presence.placement += binDefinitions[bin].AxisCenter;
                    }
                    else
                        currentLexic.Add(word, new LexicPresence(word, 1,binDefinitions[bin].AxisCenter));
                }

                //Aggregate message to measure bins
                for (int m = 0; m < measures.Count; m++)
                {
                    if (!selfSplitMode)
                    {
                        //Regular non-split mode
                        plotsData[m].binValues[bin] += msg.measures[measures[m]];
                    }
                    else
                    {
                        //Split mode
                        if (msg.author == owner)
                        {
                            //add to selfplotdata
                            selfPlotsData[m].binValues[bin] += msg.measures[measures[m]];
                        }
                        else
                        {
                            //add to regular plotdata
                            plotsData[m].binValues[bin] += msg.measures[measures[m]];
                        }
                    }
                }
            }


            //Process final results
            //Get maxims
            for (int m = 0; m < measures.Count; m++)
            {
                for (int b = 0; b < binCount; b++)
                {
                    if (plotsData[m].binValues[b] > plotsData[m].max)
                        plotsData[m].max = plotsData[m].binValues[b];

                    if (selfSplitMode)
                    {
                        if (selfPlotsData[m].binValues[b] > selfPlotsData[m].max)
                            selfPlotsData[m].max = selfPlotsData[m].binValues[b];
                    }
                }
            }

            ComputeLexicalAnomalies();
        }

        private void ComputeLexicalAnomalies()
        {
            int presenceCount = 30;

            lexicAnomalies.Clear();

            foreach (LexicPresence presence in currentLexic.Values)
            {
                presence.percentage = presence.count / (double)currentWordCount;

                presence.placement = presence.placement / presence.count;
                presence.ratioDiference = presence.percentage / baseLexic[presence.word].percentage;

                if (presence.ratioDiference > 1.05 && presence.word != "" && presence.count > 1)
                {
                    presence.weight = presence.ratioDiference;
                    //presence.weight = presence.percentage;
                    lexicAnomalies.Add(presence);
                }
            }

            lexicAnomalies.Sort((x, y) => y.weight.CompareTo(x.weight));
        }

        private void ComputeBaseLexic()
        {
            baseLexic = new Dictionary<string, LexicPresence>();
            baseWordCount = 0;

            LexicPresence presence;
            for (int k = 0; k <baseMessages.Count; k++)
            {
                Message msg = baseMessages[k];

                string[] words = msg.text.Split(' ');
                baseWordCount += words.Length;

                for (int w = 0; w < words.Length; w++)
                {
                    string word = words[w];
                    if (baseLexic.ContainsKey(word))
                    {
                        presence = baseLexic[word];
                        presence.count++;
                    }
                    else
                        baseLexic.Add(word, new LexicPresence(word, 1, 0.5f));
                }
            }

            foreach( LexicPresence presenceItem in baseLexic.Values)
            {
                presenceItem.percentage = presenceItem.count / (double)baseWordCount;
            }
        }

        public void ChangeTimeMode(TimeMode newMode)
        {
            timeMode = newMode;

            //Sort messages based on timemode for optimizations purpose
            switch (timeMode)
            {
                case TimeMode.Linear:
                    baseMessages.Sort(Comparer<Message>.Create((m1, m2) => m1.time.Ticks.CompareTo(m2.time.Ticks)));
                    break;
                case TimeMode.Year:
                    baseMessages.Sort(Comparer<Message>.Create((m1, m2) => m1.time.DayOfYear.CompareTo(m2.time.DayOfYear)));
                    break;
                case TimeMode.WeekDay:
                    baseMessages.Sort(Comparer<Message>.Create((m1, m2) => m1.time.DayOfWeek.CompareTo(m2.time.DayOfWeek)));
                    break;
                case TimeMode.HourMinute:
                    baseMessages.Sort(Comparer<Message>.Create((m1, m2) => m1.time.Hour.CompareTo(m2.time.Hour)));
                    break;
            }

            resetBounds();
            UpdateData();
        }

        public bool GetSplitMode()
        {
            return selfSplitMode;
        }
        public void SetSplitMode(bool split)
        {
            selfSplitMode = split;
            UpdateData();
        }

        public void addMeasure(int index)
        {
            measures.Add(index);

            //Create data object associated with measure
            PlotData measureData = new PlotData();
            measureData.measure = index;
            plotsData.Add(measureData);


            if (selfSplitMode)
            {
                //create selfMeasure
                PlotData selfMeasureData = new PlotData();
                selfMeasureData.measure = index;
                selfPlotsData.Add(selfMeasureData);
            }

            UpdateData();
        }
        public void removeMeasure(int measure)
        {
            for (int i = 0; i < measures.Count; i++)
            {
                if (measures[i] == measure)
                {

                    //Remove Data
                    plotsData.RemoveAt(i);


                    //Remove selfData
                    if (selfSplitMode)
                    {
                        //remove selfplotdata
                        selfPlotsData.RemoveAt(i);
                    }

                    break;
                }
            }

            UpdateData();
        }

        public void ZoomIn(double cursor)
        {
            if (zoom != ZoomLevel.Hour)
            {
                zoom = (ZoomLevel)((int)zoom + 1);

                long currentSpan = endTimeScope - beginTimeScope;
                long center = (long)(beginTimeScope + cursor * currentSpan);
                ComputeBinsAndBounds(center);
                UpdateData();
            }
        }
        public void ZoomOut()
        {
            if (zoom == ZoomLevel.All)
                return;

            if (zoom == ZoomLevel.Year && timeMode == TimeMode.Year)
                return;

            if (zoom == ZoomLevel.Week && timeMode == TimeMode.WeekDay)
                return;

            if (zoom == ZoomLevel.Day && timeMode == TimeMode.HourMinute)
                return;

            zoom = (ZoomLevel)((int)zoom - 1);
            ComputeBinsAndBounds((beginTimeScope + endTimeScope)/2);

            UpdateData();
        }

        public void PanRight()
        {
            Pan(1);
        }
        public void PanLeft()
        {
            Pan(-1);
        }
        public void Pan(int leap)
        {
            //Helpful time objects
            TimeSpan fullYear = new TimeSpan(365, 0, 0, 0);
            TimeSpan fullWeek = new TimeSpan(7, 0, 0, 0);
            TimeSpan fullMonth = new TimeSpan(31, 0, 0, 0);
            TimeSpan fullDay = new TimeSpan(24, 0, 0);
            TimeSpan fullHour = new TimeSpan(1, 0, 0);

            long center = (beginTimeScope + endTimeScope) / 2;
            switch (zoom)
            {
                case ZoomLevel.All:
                    //Do nothing, cant pan on allzoom
                    break;
                case ZoomLevel.Year:
                    ComputeBinsAndBounds(center + leap * fullMonth.Ticks);
                    break;
                case ZoomLevel.Month:
                    ComputeBinsAndBounds(center + leap * fullMonth.Ticks/4);
                    break;
                case ZoomLevel.Week:
                    ComputeBinsAndBounds(center + leap * fullDay.Ticks);
                    break;
                case ZoomLevel.Day:
                    ComputeBinsAndBounds(center + leap * fullHour.Ticks);
                    break;
                case ZoomLevel.Hour:
                    ComputeBinsAndBounds(center + leap * fullHour.Ticks / 4);
                    break;
            }

            UpdateData();
        }

        private void resetBounds()
        {
            switch (timeMode)
            {
                case TimeMode.Linear:
                    beginTimeScope = baseTimeStart.Ticks;
                    endTimeScope = baseTimeEnd.Ticks;
                    zoom = ZoomLevel.All;
                    break;
                case TimeMode.Year:
                    beginTimeScope = 0;
                    endTimeScope = new TimeSpan(366, 0, 0, 0).Ticks;
                    zoom = ZoomLevel.Year;
                    break;
                case TimeMode.WeekDay:
                    beginTimeScope = 0;
                    endTimeScope = new TimeSpan(7, 0, 0, 0).Ticks;
                    zoom = ZoomLevel.Week;
                    break;
                case TimeMode.HourMinute:
                    beginTimeScope = 0;
                    endTimeScope = new TimeSpan(24, 0, 0).Ticks;
                    zoom = ZoomLevel.Day;
                    break;
            }
        }
        private void ComputeBinsAndBounds(long center)
        {
            long span = 0;

            //Helpful time objects
            TimeSpan fullYear = new TimeSpan(365, 0, 0, 0);
            TimeSpan fullWeek = new TimeSpan(7, 0, 0, 0);
            TimeSpan fullMonth = new TimeSpan(31, 0, 0, 0);
            TimeSpan fullDay = new TimeSpan(24, 0, 0);
            TimeSpan fullHour = new TimeSpan(1, 0, 0);

            switch (zoom)
            {
                case ZoomLevel.All:
                    binCount = 100;
                    span = baseTimeEnd.Ticks - baseTimeStart.Ticks;
                    break;
                case ZoomLevel.Year:
                    binCount = 48;
                    span = fullYear.Ticks;
                    break;
                case ZoomLevel.Month:
                    binCount = 31;
                    span = fullMonth.Ticks;
                    break;
                case ZoomLevel.Week:
                    binCount = 7 * 8;
                    span = fullWeek.Ticks;
                    break;
                case ZoomLevel.Day:
                    binCount = 24 * 2;
                    span = fullDay.Ticks;
                    break;
                case ZoomLevel.Hour:
                    binCount = 60;
                    span = fullHour.Ticks;
                    break;
            }

            switch (timeMode)
            {
                case TimeMode.Linear:
                    if (zoom == ZoomLevel.All)
                    {
                        beginTimeScope = baseTimeStart.Ticks;
                        endTimeScope = baseTimeEnd.Ticks;
                    }
                    else
                    {
                        beginTimeScope = center - span / 2;
                        endTimeScope = center + span / 2;

                        //Bound upper bound
                        if (endTimeScope > baseTimeEnd.Ticks)
                        {
                            long diff = endTimeScope - baseTimeEnd.Ticks;

                            endTimeScope -= diff;
                            beginTimeScope -= diff;
                        }

                        //Bound lower bound
                        if (beginTimeScope < baseTimeStart.Ticks)
                        {
                            long diff = baseTimeStart.Ticks - beginTimeScope;

                            endTimeScope += diff;
                            beginTimeScope += diff;
                        }
                    }
                    break;
                case TimeMode.Year:
                    beginTimeScope = center - span / 2;
                    endTimeScope = center + span / 2;

                    //Bound upper bound
                    if (endTimeScope > fullYear.Ticks)
                    {
                        long diff = endTimeScope - fullYear.Ticks;

                        endTimeScope -= diff;
                        beginTimeScope -= diff;
                    }

                    //Bound lower bound
                    if (beginTimeScope < 0)
                    {
                        long diff = -beginTimeScope;

                        endTimeScope += diff;
                        beginTimeScope += diff;
                    }
                    break;
                case TimeMode.WeekDay:
                    beginTimeScope = center - span / 2;
                    endTimeScope = center + span / 2;

                    //Bound upper bound
                    if (endTimeScope > fullWeek.Ticks)
                    {
                        long diff = endTimeScope - fullWeek.Ticks;

                        endTimeScope -= diff;
                        beginTimeScope -= diff;
                    }

                    //Bound lower bound
                    if (beginTimeScope < 0)
                    {
                        long diff = -beginTimeScope;

                        endTimeScope += diff;
                        beginTimeScope += diff;
                    }
                    break;
                case TimeMode.HourMinute:
                    beginTimeScope = center - span / 2;
                    endTimeScope = center + span / 2;

                    //Bound upper bound
                    if (endTimeScope > fullDay.Ticks)
                    {
                        long diff = endTimeScope - fullDay.Ticks;

                        endTimeScope -= diff;
                        beginTimeScope -= diff;
                    }

                    //Bound lower bound
                    if (beginTimeScope < 0)
                    {
                        long diff = -beginTimeScope;

                        endTimeScope += diff;
                        beginTimeScope += diff;
                    }
                    break;
            }
        }

        public long AxisFactorToTime(double factor)
        {
            return (long)(beginTimeScope + factor * (endTimeScope - beginTimeScope));
        }
        public double AxisTimeToFactor(long time)
        {
            return (double)(time - beginTimeScope / (double)(endTimeScope - beginTimeScope));
        }

        public Tuple<double,double> getPreviewBounds(double axisCursor)
        {
            double span = 1;

            if (zoom == ZoomLevel.Hour)
                span = 1;

            //Helpful time objects
            TimeSpan fullYear = new TimeSpan(365, 0, 0, 0);
            TimeSpan fullWeek = new TimeSpan(7, 0, 0, 0);
            TimeSpan fullMonth = new TimeSpan(31, 0, 0, 0);
            TimeSpan fullDay = new TimeSpan(24, 0, 0);
            TimeSpan fullHour = new TimeSpan(1, 0, 0);

            long longSpan = 0;

            ZoomLevel newZoom = (ZoomLevel)((int)zoom + 1);
            switch (newZoom)
            {
                case ZoomLevel.All:
                    longSpan = baseTimeEnd.Ticks - baseTimeStart.Ticks;
                    break;
                case ZoomLevel.Year:
                    longSpan = fullYear.Ticks;
                    break;
                case ZoomLevel.Month:
                    longSpan = fullMonth.Ticks;
                    break;
                case ZoomLevel.Week:
                    longSpan = fullWeek.Ticks;
                    break;
                case ZoomLevel.Day:
                    longSpan = fullDay.Ticks;
                    break;
                case ZoomLevel.Hour:
                    longSpan = fullHour.Ticks;
                    break;
            }

            span = ((double)longSpan) / ((double)(endTimeScope - beginTimeScope));

            double start = axisCursor - span/2;
            double end = axisCursor + span/2;

            if(start < 0)
            {
                double diff = -start;

                start += diff;
                end += diff;
            }

            if(end > 1)
            {
                double diff = end - 1;

                start -= diff;
                end -= diff;
            }

            return new Tuple<double, double>(start, end);
        }

        public List<LabelDefinition> getTimeAxisLabels()
        {
            List<LabelDefinition> labelDefs = new List<LabelDefinition>();

            DateTime cursorDate;
            long cursorTime;

            DateTime referenceDate;
            switch(timeMode)
            {
                case TimeMode.Linear:
                    DateTime startDate = new DateTime(beginTimeScope);
                    DateTime endDate = new DateTime(endTimeScope);
                    timeOverview = startDate.ToLongDateString();

                    switch (zoom)
                    {
                        case ZoomLevel.All:
                            //go through years
                            for (int i = startDate.Year; i <= endDate.Year; i++)
                            {
                                DateTime tt = new DateTime(i, 1, 1);
                                long place = tt.Ticks - baseTimeStart.Ticks;
                                double factorPlace = place / (double)(baseTimeEnd.Ticks - baseTimeStart.Ticks);

                                labelDefs.Add(new LabelDefinition(factorPlace, "" + i, ""));
                            }
                            break;
                        case ZoomLevel.Year:
                            //go through months
                            cursorDate = new DateTime(startDate.Year, startDate.Month, 1);

                            while (cursorDate <= endDate)
                            {
                                long place = cursorDate.Ticks - startDate.Ticks;
                                double factorPlace = place / (double)(endDate.Ticks - startDate.Ticks);

                                labelDefs.Add(new LabelDefinition(factorPlace, "" + cursorDate.Month + "/" + cursorDate.Year, ""));

                                //Iterate a month
                                if (cursorDate.Month == 12)
                                {
                                    cursorDate = new DateTime(cursorDate.Year + 1, 1, 1);
                                }
                                else
                                    cursorDate = new DateTime(cursorDate.Year, cursorDate.Month + 1, 1);
                            }
                            break;
                        case ZoomLevel.Month:
                            //go through days
                            cursorDate = new DateTime(startDate.Year, startDate.Month, 1);
                            while (cursorDate <= endDate)
                            {
                                long place = cursorDate.Ticks - startDate.Ticks;
                                double factorPlace = place / (double)(endDate.Ticks - startDate.Ticks);

                                labelDefs.Add(new LabelDefinition(factorPlace, "" + cursorDate.Day, ""));// + "/" + cursorDate.Month , ""));

                                cursorDate = cursorDate.AddDays(1);
                            }
                            break;
                        case ZoomLevel.Week:
                            //go through days
                            cursorDate = new DateTime(startDate.Year, startDate.Month, 1);
                            while (cursorDate <= endDate)
                            {
                                long place = cursorDate.Ticks - startDate.Ticks;
                                double factorPlace = place / (double)(endDate.Ticks - startDate.Ticks);

                                labelDefs.Add(new LabelDefinition(factorPlace, "" + cursorDate.Day , "" + cursorDate.DayOfWeek));

                                cursorDate = cursorDate.AddDays(1);
                            }
                            break;
                        case ZoomLevel.Day:
                            //go through hours
                            cursorDate = new DateTime(startDate.Year, startDate.Month, startDate.Day, 0,0,0);
                            while (cursorDate <= endDate)
                            {
                                long place = cursorDate.Ticks - startDate.Ticks;
                                double factorPlace = place / (double)(endDate.Ticks - startDate.Ticks);

                                labelDefs.Add(new LabelDefinition(factorPlace, "" + cursorDate.Hour + ":" + cursorDate.Minute, ""));

                                cursorDate = cursorDate.AddHours(1);
                            }
                            break;
                        case ZoomLevel.Hour:
                            //go through minutes
                            cursorDate = new DateTime(startDate.Year, startDate.Month, startDate.Day, startDate.Hour, 0, 0);
                            while (cursorDate <= endDate)
                            {
                                long place = cursorDate.Ticks - startDate.Ticks;
                                double factorPlace = place / (double)(endDate.Ticks - startDate.Ticks);

                                labelDefs.Add(new LabelDefinition(factorPlace, "" + cursorDate.ToLongTimeString(), ""));

                                cursorDate = cursorDate.AddMinutes(5);
                            }
                            break;
                    }
                    break;
                    // ------------------------------- YEAR -----
                case TimeMode.Year:
                    switch (zoom)
                    {
                        case ZoomLevel.Year:
                            timeOverview = "Agregated Year";
                            //go through months
                            for (int i=1;i<=12;i++)
                            {
                                labelDefs.Add(new LabelDefinition((i - 1) / (double)12 + 0.001 , "" + monthFromNumber(i) , ""));
                            }
                            break;
                        case ZoomLevel.Month:
                            //go through days
                            referenceDate = new DateTime(2000, 1, 1); //some random year
                            cursorDate = new DateTime(referenceDate.Ticks + beginTimeScope);
                            cursorDate = new DateTime(cursorDate.Year, cursorDate.Month, cursorDate.Day);
                            timeOverview = cursorDate.ToString("MMMM , d");
                            while (cursorDate.Ticks - referenceDate.Ticks <= endTimeScope)
                            {
                                long place = cursorDate.Ticks - referenceDate.Ticks - beginTimeScope;
                                double factorPlace = place / (double)(endTimeScope - beginTimeScope);

                                labelDefs.Add(new LabelDefinition(factorPlace, "" + cursorDate.Day, ""));// + "/" + cursorDate.Month, ""));

                                cursorDate = cursorDate.AddDays(1);
                            }
                            break;
                        case ZoomLevel.Week:
                            //go through days
                            referenceDate = new DateTime(2000, 1, 1); //some random year
                            cursorDate = new DateTime(referenceDate.Ticks + beginTimeScope);
                            cursorDate = new DateTime(cursorDate.Year, cursorDate.Month, cursorDate.Day);
                            timeOverview = cursorDate.ToString("MMMM , d");
                            while (cursorDate.Ticks - referenceDate.Ticks <= endTimeScope)
                            {
                                long place = cursorDate.Ticks - referenceDate.Ticks - beginTimeScope;
                                double factorPlace = place / (double)(endTimeScope - beginTimeScope);

                                labelDefs.Add(new LabelDefinition(factorPlace, "" + cursorDate.Day + "/" + cursorDate.Month, ""));

                                cursorDate = cursorDate.AddDays(1);
                            }
                            break;
                        case ZoomLevel.Day:
                            //go through hours
                            referenceDate = new DateTime(2000, 1, 1); //some random year
                            cursorDate = new DateTime(referenceDate.Ticks + beginTimeScope);
                            cursorDate = new DateTime(cursorDate.Year, cursorDate.Month, cursorDate.Day, 0,0,0);
                            timeOverview = cursorDate.ToString("MMMM , d , dddd ");
                            while (cursorDate.Ticks - referenceDate.Ticks <= endTimeScope)
                            {
                                long place = cursorDate.Ticks - referenceDate.Ticks - beginTimeScope;
                                double factorPlace = place / (double)(endTimeScope - beginTimeScope);

                                labelDefs.Add(new LabelDefinition(factorPlace, "" + cursorDate.Hour + ":" + cursorDate.Minute, ""));

                                cursorDate = cursorDate.AddHours(1);
                            }
                            break;
                        case ZoomLevel.Hour:
                            //go through hours
                            referenceDate = new DateTime(2000, 1, 1); //some random year
                            cursorDate = new DateTime(referenceDate.Ticks + beginTimeScope);
                            cursorDate = new DateTime(cursorDate.Year, cursorDate.Month, cursorDate.Day, 0, 0, 0);
                            timeOverview = cursorDate.ToString("MMMM, d , dddd ");
                            while (cursorDate.Ticks - referenceDate.Ticks <= endTimeScope)
                            {
                                long place = cursorDate.Ticks - referenceDate.Ticks - beginTimeScope;
                                double factorPlace = place / (double)(endTimeScope - beginTimeScope);

                                labelDefs.Add(new LabelDefinition(factorPlace, "" + cursorDate.ToLongTimeString(), ""));

                                cursorDate = cursorDate.AddMinutes(5);
                            }
                            break;
                    }
                    break;
                    // -------------------------------------------- TimeMode Week -------------------
                case TimeMode.WeekDay:
                    switch (zoom)
                    {
                        case ZoomLevel.Week:
                            //go through days
                            timeOverview = "Agregated Week";
                            cursorDate = new DateTime(2017, 12, 10); //some week start
                            for (int i = 1; i <= 7; i++)
                            {
                                labelDefs.Add(new LabelDefinition((i - 1) / (double)7 + 0.001, "" + cursorDate.DayOfWeek, ""));
                                cursorDate = cursorDate.AddDays(1);
                            }
                            break;
                        case ZoomLevel.Day:
                            //go through hours
                            referenceDate = new DateTime(2017, 12, 10); //some random week start
                            cursorDate = new DateTime(referenceDate.Ticks + beginTimeScope);
                            cursorDate = new DateTime(cursorDate.Year, cursorDate.Month, cursorDate.Day, 0, 0, 0);
                            timeOverview = cursorDate.ToString("dddd");
                            while (cursorDate.Ticks - referenceDate.Ticks <= endTimeScope)
                            {
                                long place = cursorDate.Ticks - referenceDate.Ticks - beginTimeScope;
                                double factorPlace = place / (double)(endTimeScope - beginTimeScope);

                                labelDefs.Add(new LabelDefinition(factorPlace, "" + cursorDate.Hour + ":" + cursorDate.Minute, ""));

                                cursorDate = cursorDate.AddHours(1);
                            }
                            break;
                        case ZoomLevel.Hour:
                            //go through hours
                            referenceDate = new DateTime(2017, 12, 10); //some random week start
                            cursorDate = new DateTime(referenceDate.Ticks + beginTimeScope);
                            cursorDate = new DateTime(cursorDate.Year, cursorDate.Month, cursorDate.Day, 0, 0, 0);
                            timeOverview = cursorDate.ToString("dddd");
                            while (cursorDate.Ticks - referenceDate.Ticks <= endTimeScope)
                            {
                                long place = cursorDate.Ticks - referenceDate.Ticks - beginTimeScope;
                                double factorPlace = place / (double)(endTimeScope - beginTimeScope);

                                labelDefs.Add(new LabelDefinition(factorPlace, "" + cursorDate.ToLongTimeString(), ""));

                                cursorDate = cursorDate.AddMinutes(5);
                            }
                            break;
                    }
                    break;
                    // ------------------------------------------------- Time Mode Hour Minute --------------------------------
                case TimeMode.HourMinute:
                    timeOverview = "Agregated Day";
                    switch (zoom)
                    {
                        case ZoomLevel.Day:
                            //go through months
                            cursorDate = new DateTime(2017, 12, 11); //reference day
                            for (int i = 1; i <= 24; i++)
                            {
                                labelDefs.Add(new LabelDefinition((i - 1) / (double)24 + 0.001, "" + cursorDate.Hour + ":" + cursorDate.Minute, ""));
                                cursorDate = cursorDate.AddHours(1);
                            }
                            break;
                        case ZoomLevel.Hour:
                            //go through hours
                            referenceDate = new DateTime(2017, 12, 10); //some random day start
                            cursorDate = new DateTime(referenceDate.Ticks + beginTimeScope);
                            cursorDate = new DateTime(cursorDate.Year, cursorDate.Month, cursorDate.Day, 0, 0, 0);
                            while (cursorDate.Ticks - referenceDate.Ticks <= endTimeScope)
                            {
                                long place = cursorDate.Ticks - referenceDate.Ticks - beginTimeScope;
                                double factorPlace = place / (double)(endTimeScope - beginTimeScope);

                                labelDefs.Add(new LabelDefinition(factorPlace, "" + cursorDate.ToLongTimeString(), ""));

                                cursorDate = cursorDate.AddMinutes(5);
                            }
                            break;
                    }
                    break;
            }
            

            return labelDefs;
        }

        private string monthFromNumber(int number)
        {
            switch (number)
            {
                case 1: return "January";
                case 2: return "February";
                case 3: return "March";
                case 4: return "April";
                case 5: return "May";
                case 6: return "June";
                case 7: return "July";
                case 8: return "August";
                case 9: return "September";
                case 10: return "October";
                case 11: return "November";
                case 12: return "December";
                default:
                    throw new FormatException("Month number exception: " + number);
            }
        }

        public class LabelDefinition
        {
            public string mainText;
            public string subText;
            public double axisFactor;

            public LabelDefinition(double axisFactor, string mainText, string subText)
            {
                this.axisFactor = axisFactor;
                this.mainText = mainText;
                this.subText = subText;
            }
        }

        public class BinDefinition
        {
            public double axisStart;
            public double axisEnd;
            public long timeStart;
            public long timeEnd;

            public double AxisCenter
            {
                get
                {
                    return (axisEnd + axisStart) / 2;
                }
            }

            public BinTimeChecker checker;
        }

        public class PlotData
        {
            public int measure;
            public double[] binValues;
            public double max;
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

        public class LexicPresence
        {
            public string word;
            public int count;
            public double placement;
            public double percentage;
            public double ratioDiference;
            public double weight;

            public LexicPresence(string word, int count, double placement)
            {
                this.word = word;
                this.count = count;
                this.placement = placement;
            }
        }
    }

}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FacebookAnalyzer
{
    public class Message
    {
        public Person author;
        public string text;
        public DateTime time;


        public enum MeasureType { CharCount, WordCount, InterrogationPresence, ExclamationPresence, ReticencePresence, CommaPresence, LinkPresence }
        public double[] measures;

        /// <summary>
        /// Array representing auxiliary relative time measures
        /// </summary>
        public long[] timeMeasures;
        
        public Message(Person author, string text, DateTime time)
        {
            this.author = author;
            this.text = text;
            this.time = time;

            process();
        }

        private void process()
        {
            measures = new double[7];
            measures[0] = text.Length;
            measures[1] = text.Split(' ').Length;
            measures[2] = text.Contains("?")? 1 : 0;
            measures[3] = text.Contains("!") ? 1 : 0;
            measures[4] = text.Contains("...") ? 1 : 0;
            measures[5] = text.Contains(",") ? 1 : 0;
            measures[6] = text.Contains("http:")|| text.Contains("https:") ? 1 : 0;

            //Time measures
            timeMeasures = new long[3];
            //year
            timeMeasures[0] = time.Subtract(getBeginningOfYear(time)).Ticks;
            //week
            timeMeasures[1] = time.Subtract(getBeginningOfWeek(time)).Ticks;
            //hour
            timeMeasures[2] = time.TimeOfDay.Ticks;

        }

        private DateTime getBeginningOfWeek(DateTime time)
        {
            DateTime beginning = new DateTime(time.Ticks);

            while ((int)beginning.DayOfWeek != 0)
                beginning = beginning.AddDays(-1);

            beginning = beginning.AddHours(-beginning.Hour);
            beginning = beginning.AddMinutes(-beginning.Minute);
            beginning = beginning.AddSeconds(-beginning.Second);

            return beginning;
        }

        private DateTime getBeginningOfYear(DateTime time)
        {
            return new DateTime(time.Year, 1, 1);
        }
    }
}

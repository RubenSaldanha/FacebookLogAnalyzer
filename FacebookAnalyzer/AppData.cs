using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FacebookAnalyzer
{
    class AppData
    {
        public static AppData data;

        public List<string> recentLogs;

        public AppData()
        {
            recentLogs = new List<string>();
        }

        public static void Load()
        {
            string location = System.AppDomain.CurrentDomain.BaseDirectory;
            if (System.IO.File.Exists(location + "\\AppData"))
            {
                using (System.IO.StreamReader file = new System.IO.StreamReader("AppData"))
                {
                    data = new AppData();
                    while(!file.EndOfStream)
                        data.recentLogs.Add(file.ReadLine());
                }
            }
            else
            {
                data = new AppData();
                Save();
            }
        }
        public static void Save()
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter("AppData"))
            {
                for (int i = 0; i < data.recentLogs.Count; i++)
                {
                    file.WriteLine(data.recentLogs[i]);
                }
            }
        }

        public void AddLog(string location)
        {
            for (int i = 0; i < recentLogs.Count; i++)
                if (recentLogs[i] == location)
                    return;

            recentLogs.Add(location);
        }
    }
}

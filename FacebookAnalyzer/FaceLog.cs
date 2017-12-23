using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FacebookAnalyzer
{
    public class FaceLog
    {
        bool obfuscatePeople = false;
        bool obfuscateWords = false;

        //Base elements
        public string location;
        public Person owner;

        public List<ChatThread> threads;
        public List<Person> people;

        //Processed Elements
        public List<Message> allMessages;

        public DateTime beginScope;
        public DateTime endScope;

        public bool LoadPrints = false;

        public int threadFileCount;
        public int threadsProcessed = 0;

        public bool loadFinished = false;

        public FaceLog()
        {

        }
        private void loadFromHtml()
        {
            people = new List<Person>();
            threads = new List<ChatThread>();


            //Parse HTML
            Console.WriteLine("Parsing Html.");
            HtmlDocument htmldoc = new HtmlDocument();
            htmldoc.Load(location + "messages.htm");
            Console.WriteLine("Html Parsed.");

            //Find own name
            Console.WriteLine("Finding owner name, #TODO, locked at Ruben Filipe.");
            owner = new Person("Ruben Filipe");
            people.Add(owner);

            //Select threads
            Console.WriteLine("Processing threads.");
            HtmlNodeCollection htmlThreads = htmldoc.DocumentNode.SelectNodes("//*[@class='thread']");
            //Process threads
            int totalMessages = 0;
            int unfiguredAuthorCount = 0;
            for (int i=0;i<htmlThreads.Count;i++)
            {

                //Console.WriteLine("Processing thread: " + i);
                HtmlNode thread = htmlThreads[i];
                //Get people in thread
                String names = thread.FirstChild.OuterHtml;
                String[] nameList = names.Split(',');

                //Process other participants
                List<Person> participants = new List<Person>();
                for (int k = 0; k < nameList.Length; k++)
                {
                    Person participant = null;
                    nameList[k] = removeOuterSpaces(nameList[k]);

                    if (!people.Exists(x => x.name == nameList[k]))
                    {
                        //Update People library
                        participant = new Person(nameList[k]);
                        people.Add(participant);
                        Console.WriteLine("Added to people Library: " + participant.name);
                    }

                    participants.Add(people.Find(x => x.name == nameList[k]));
                }

                if (!participants.Contains(owner))
                {
                    //throw new FormatException("Invalid thread, owner not present.");
                    Console.Write("Thread found without owner, with participants:");
                    for (int k = 0; k < participants.Count; k++)
                        Console.Write(" '" + participants[k].name + "' ");
                    Console.WriteLine("");
                    continue;
                }

                //Create ChatThread
                ChatThread chat = findThreadWith(participants);
                if (chat == null)
                {
                    chat = new ChatThread(participants);
                    threads.Add(chat);
                }


                //Console.WriteLine("Begin processing messages.");
                int nodeCursor = 1; //index 0 is namelist
                while(nodeCursor < thread.ChildNodes.Count)
                {
                    //Console.WriteLine("Processing cursor at: " + nodeCursor);
                    //Process (Message Header, Text) node pair

                    //Check if there is a message header on cursor
                    if (!thread.ChildNodes[nodeCursor].HasAttributes)
                        throw new FormatException("Invalid node structure in threads. Expected message header , found: " + thread.ChildNodes[nodeCursor].OuterHtml);

                    if (!thread.ChildNodes[nodeCursor].Attributes.Contains("class"))
                        throw new FormatException("Invalid node structure in threads. Expected message header , found: " + thread.ChildNodes[nodeCursor].OuterHtml);

                    if (!thread.ChildNodes[nodeCursor].HasClass("message"))
                        throw new FormatException("Invalid node structure in threads. Expected message header , found: " + thread.ChildNodes[nodeCursor].OuterHtml);

                    if (!(thread.ChildNodes[nodeCursor].FirstChild != null))
                        throw new FormatException("Invalid node structure in threads. Expected message header , found: " + thread.ChildNodes[nodeCursor].OuterHtml);

                    if (!(thread.ChildNodes[nodeCursor].FirstChild.HasAttributes))
                        throw new FormatException("Invalid node structure in threads. Expected message header , found: " + thread.ChildNodes[nodeCursor].OuterHtml);

                    if (!(thread.ChildNodes[nodeCursor].FirstChild.Attributes.Contains("class")))
                        throw new FormatException("Invalid node structure in threads. Expected message header , found: " + thread.ChildNodes[nodeCursor].OuterHtml);

                    if (!thread.ChildNodes[nodeCursor].FirstChild.HasClass("message_header"))
                        throw new FormatException("Invalid node structure in threads. Expected message header , found: " + thread.ChildNodes[nodeCursor].OuterHtml);

                    if (!(thread.ChildNodes[nodeCursor].FirstChild.ChildNodes.Count == 2))
                        throw new FormatException("Invalid node structure in threads. Expected message header , found: " + thread.ChildNodes[nodeCursor].OuterHtml);

                    //Check if there is a paragraph after the header
                    if (nodeCursor + 1 >= thread.ChildNodes.Count)
                        throw new FormatException("Invalid node sructure in thread. Expected paragraph, found nothing.");

                    //Retrieve information
                    string author = thread.ChildNodes[nodeCursor].FirstChild.ChildNodes[0].InnerText;
                    DateTime time = processTimeFormat(thread.ChildNodes[nodeCursor].FirstChild.ChildNodes[1].InnerText);
                    string text = thread.ChildNodes[nodeCursor + 1].InnerText;

                    Person pAuthor = people.Find(x => x.name == author);
                    if (!participants.Contains(pAuthor))
                    {
                        //throw new FormatException("Invalid message found, on thread " + i + " : '" + pAuthor.name + "' shouldn't figure in this thread.");
                        unfiguredAuthorCount++;
                        nodeCursor += 2;
                        continue;
                    }

                    Message msg = new Message(pAuthor, text, time);
                    chat.messages.Add(msg);
                    totalMessages++;

                    nodeCursor += 2;
                }

                //Console.WriteLine("Messages: " + chat.messages.Count);
            }

            Console.WriteLine("-----------------");
            Console.WriteLine("Log processed.");
            Console.WriteLine("Total threads count: " + threads.Count);
            Console.WriteLine("Total message count: " + totalMessages);
            Console.WriteLine("Unfigured Authors message count: " + unfiguredAuthorCount);

            threads.Sort(Comparer<ChatThread>.Create((c1, c2) => c2.messages.Count.CompareTo(c1.messages.Count)));
            for(int k=0;k<threads.Count;k++)
            {
                threads[k].people.Sort(Comparer<Person>.Create((p1, p2) => p1.name.CompareTo(p2.name)));
                threads[k].messages.Sort(Comparer<Message>.Create((m1, m2) => m1.time.Ticks.CompareTo(m2.time.Ticks)));

                if (LoadPrints)
                {
                    Console.Write(("" + (k + 1)).PadRight(4) + " : " + ("" + threads[k].messages.Count).PadRight(10) + " :: ");
                    for (int j = 0; j < threads[k].people.Count; j++)
                        Console.Write(" '" + threads[k].people[j].name + "' ");

                    Console.WriteLine();
                }
            }

            FinalizeLoad();
        }

        private void loadFromHtmlv2()
        {
            people = new List<Person>();
            threads = new List<ChatThread>();

            //Find owner name
            Console.WriteLine("Finding owner name");
            ParseOwner();
            Console.WriteLine("Owner:" + owner.name);
            //owner = new Person("Ruben Filipe");
            //people.Add(owner);

            //Get message files
            if (!Directory.Exists(location + "\\messages"))
                throw new Exception("Invalid Log directory");

            string[] threadFiles = Directory.GetFiles(location + "\\messages");
            threadFileCount = threadFiles.Length;

            //Parse HTML
            Console.WriteLine("Parsing Html.");
            List<HtmlNodeCollection> allHtmlThreads = new List<HtmlNodeCollection>();
            for(int i=0;i<threadFiles.Length;i++)
            {
                HtmlDocument htmldoc = new HtmlDocument();
                try
                {
                    htmldoc.DetectEncodingAndLoad(threadFiles[i], true);
                    threadsProcessed++;
                }
                catch(Exception e)
                {
                    Console.WriteLine("Failed to parse html thread file: '" + threadFiles[i] + "'");
                    continue;
                }

                allHtmlThreads.Add(htmldoc.DocumentNode.SelectNodes("//*[@class='thread']"));
            }

            //Process threads
            int totalMessages = 0;
            int unfiguredAuthorCount = 0;
            int emptyParticipantCount = 0;
            int untreatableThreadsCount = 0;
            string startString = "Conversation with&nbsp;";
            for (int j = 0; j < allHtmlThreads.Count; j++)
            {
                HtmlNodeCollection htmlThreads = allHtmlThreads[j];

                for (int i = 0; i < htmlThreads.Count; i++)
                {
                    try
                    {
                        //Console.WriteLine("Processing thread: " + i);
                        HtmlNode thread = htmlThreads[i];
                        //Get people in thread
                        string header = thread.FirstChild.OuterHtml;
                        if (header[0] == '\n')
                            header = header.Substring(1);

                        //Remove startString from people
                        string names = header.Substring(startString.Length);

                        if (names == "")
                        {
                            Console.WriteLine("Found empty participant Thread.");
                            emptyParticipantCount++;
                            continue;
                        }

                        if(LoadPrints)
                            Console.WriteLine("Starting thread with: " + names);
                        String[] nameList = names.Split(',');

                        //Process other participants
                        List<Person> participants = new List<Person>();
                        for (int k = 0; k < nameList.Length; k++)
                        {
                            Person participant = null;
                            nameList[k] = removeOuterSpaces(nameList[k]);

                            if (!people.Exists(x => x.name == nameList[k]))
                            {
                                //Update People library
                                participant = new Person(nameList[k]);
                                people.Add(participant);
                                if(LoadPrints)
                                    Console.WriteLine("Added to people Library: " + participant.name);
                            }

                            participants.Add(people.Find(x => x.name == nameList[k]));
                        }

                        //Deprecated, only happenned in older version
                        if (!participants.Contains(owner) && false)
                        {
                            //throw new FormatException("Invalid thread, owner not present.");
                            Console.Write("Thread found without owner, with participants:");
                            for (int k = 0; k < participants.Count; k++)
                                Console.Write(" '" + participants[k].name + "' ");
                            Console.WriteLine("");
                            continue;
                        }
                        //Force Add
                        participants.Add(owner);

                        //Create ChatThread
                        ChatThread chat = findThreadWith(participants);
                        if (chat == null)
                        {
                            chat = new ChatThread(participants);
                            threads.Add(chat);
                        }


                        //Console.WriteLine("Begin processing messages.");
                        //TODO better option find first message node
                        int nodeCursor = 1; //index 0 is namelist
                        while (nodeCursor < thread.ChildNodes.Count)
                        {
                            HtmlNode node = thread.ChildNodes[nodeCursor];
                            //Console.WriteLine("Processing cursor at: " + nodeCursor);
                            //Process (Message Header, Text) node pair

                            //Check if there is a message header on cursor
                            if (!node.HasAttributes)
                                throw new FormatException("Invalid node structure in threads. Expected message header , found: " + thread.ChildNodes[nodeCursor].OuterHtml);

                            if (!node.Attributes.Contains("class"))
                                throw new FormatException("Invalid node structure in threads. Expected message header , found: " + thread.ChildNodes[nodeCursor].OuterHtml);

                            if (!node.HasClass("message"))
                                throw new FormatException("Invalid node structure in threads. Expected message header , found: " + thread.ChildNodes[nodeCursor].OuterHtml);

                            if (!(node.FirstChild != null))
                                throw new FormatException("Invalid node structure in threads. Expected message header , found: " + thread.ChildNodes[nodeCursor].OuterHtml);

                            if (!(node.FirstChild.HasAttributes))
                                throw new FormatException("Invalid node structure in threads. Expected message header , found: " + thread.ChildNodes[nodeCursor].OuterHtml);

                            if (!(node.FirstChild.Attributes.Contains("class")))
                                throw new FormatException("Invalid node structure in threads. Expected message header , found: " + thread.ChildNodes[nodeCursor].OuterHtml);

                            if (!node.FirstChild.HasClass("message_header"))
                                throw new FormatException("Invalid node structure in threads. Expected message header , found: " + thread.ChildNodes[nodeCursor].OuterHtml);

                            if (!(node.FirstChild.ChildNodes.Count == 2))
                                throw new FormatException("Invalid node structure in threads. Expected message header , found: " + thread.ChildNodes[nodeCursor].OuterHtml);

                            //Check if there is a paragraph after the header
                            if (nodeCursor + 1 >= thread.ChildNodes.Count)
                                throw new FormatException("Invalid node sructure in thread. Expected paragraph, found nothing.");

                            //Retrieve information
                            string author = thread.ChildNodes[nodeCursor].FirstChild.ChildNodes[0].InnerText;
                            DateTime time = processTimeFormat(thread.ChildNodes[nodeCursor].FirstChild.ChildNodes[1].InnerText);
                            string text = thread.ChildNodes[nodeCursor + 1].InnerText;

                            Person pAuthor = people.Find(x => x.name == author);
                            if (!participants.Contains(pAuthor))
                            {
                                //throw new FormatException("Invalid message found, on thread " + i + " : '" + pAuthor.name + "' shouldn't figure in this thread.");
                                unfiguredAuthorCount++;
                                nodeCursor += 2;
                                continue;
                            }

                            Message msg = new Message(pAuthor, text, time);
                            chat.messages.Add(msg);
                            totalMessages++;

                            nodeCursor += 2;
                        }

                        //Console.WriteLine("Messages: " + chat.messages.Count);
                    }
                    catch(Exception e)
                    {
                        untreatableThreadsCount++;
                    }
                }
            }

            Console.WriteLine("-----------------");
            Console.WriteLine("Log processed.");
            Console.WriteLine("Total threads count: " + threads.Count);
            Console.WriteLine("Total message count: " + totalMessages);
            Console.WriteLine("Unfigured Authors message count: " + unfiguredAuthorCount);
            Console.WriteLine("Empty participant threads count: " + emptyParticipantCount);

            threads.Sort(Comparer<ChatThread>.Create((c1, c2) => c2.messages.Count.CompareTo(c1.messages.Count)));
            for (int k = 0; k < threads.Count; k++)
            {
                threads[k].people.Sort(Comparer<Person>.Create((p1, p2) => p1.name.CompareTo(p2.name)));
                threads[k].messages.Sort(Comparer<Message>.Create((m1, m2) => m1.time.Ticks.CompareTo(m2.time.Ticks)));

                if (LoadPrints)
                {
                    Console.Write(("" + (k + 1)).PadRight(4) + " : " + ("" + threads[k].messages.Count).PadRight(10) + " :: ");
                    for (int j = 0; j < threads[k].people.Count; j++)
                        Console.Write(" '" + threads[k].people[j].name + "' ");

                    Console.WriteLine();
                }
            }

            FinalizeLoad();

            //GenerateMultivariateData();

            AppData.data.AddLog(location);

            ComputeLexic();
            obfuscate();

            loadFinished = true;
        }
        private void ParseOwner()
        {
            string[] directoryFiles = Directory.GetFiles(location);

            HtmlDocument htmldoc = null;
            for (int i=0;i<directoryFiles.Length;i++)
            {
                if(Path.GetFileName(directoryFiles[i]) == "index.htm")
                {
                    htmldoc = new HtmlDocument();
                    htmldoc.Load(directoryFiles[i]);
                    break;
                }
            }

            if (htmldoc == null)
                throw new Exception("No Index found on folder");

            HtmlNodeCollection contents = htmldoc.DocumentNode.SelectNodes("//*[@class='contents']");

            for(int i=0;i< contents.Count;i++)
            {
                if(contents[i].FirstChild.InnerText != "")
                {
                    string name = contents[i].FirstChild.InnerText;
                    owner = new Person(name);
                    people.Add(owner);
                    return;
                }
            }

            throw new Exception("Index file did not match specifications.");
        }

        private void FinalizeLoad()
        {
            //Create full message log
            allMessages = new List<Message>();
            for(int i=0;i<threads.Count;i++)
            {
                ChatThread thread = threads[i];
                for(int k=0;k<thread.messages.Count;k++)
                {
                    allMessages.Add(thread.messages[k]);
                }
            }
            allMessages.Sort(Comparer<Message>.Create((m1, m2) => m1.time.Ticks.CompareTo(m2.time.Ticks)));

            beginScope = allMessages[0].time;
            endScope = allMessages[allMessages.Count - 1].time;
        }

        private void GenerateMultivariateData()
        {
            List<DataRow> data = new List<DataRow>();
            for(int i=0;i<threads.Count;i++)
            {
                ChatThread thread = threads[i];

                //Skip group threads
                if (thread.people.Count != 2)
                    continue;

                //get other person
                Person other = (thread.people[0] == owner) ? thread.people[1] : thread.people[0];

                DataRow row = new DataRow();
                row.selfData = new PersonMetrics();
                row.otherData = new PersonMetrics();

                for(int k=0;k<thread.messages.Count;k++)
                {
                    Message msg = thread.messages[k];

                    //Check who's message it is
                    if(msg.author == owner)
                    {
                        row.selfData.charCount += (int)msg.measures[(int)Message.MeasureType.CharCount];
                        row.selfData.wordCount += (int)msg.measures[(int)Message.MeasureType.WordCount];
                        row.selfData.messageCount += 1;
                        row.selfData.interrogationCount += (int)msg.measures[(int)Message.MeasureType.InterrogationPresence];
                        row.selfData.exclamationCount += (int)msg.measures[(int)Message.MeasureType.ExclamationPresence];
                        row.selfData.reticencesCount += (int)msg.measures[(int)Message.MeasureType.ReticencePresence];
                        row.selfData.commaCount += (int)msg.measures[(int)Message.MeasureType.CommaPresence];
                        row.selfData.linkCount += (int)msg.measures[(int)Message.MeasureType.LinkPresence];

                        row.selfData.euCount += (msg.text.Contains("Eu ") || msg.text.Contains(" eu ")) ? 1 : 0;
                        row.selfData.tuCount += (msg.text.Contains("Tu ") || msg.text.Contains(" tu ")) ? 1 : 0;
                    }
                    else //other
                    {
                        row.otherData.charCount += (int)msg.measures[(int)Message.MeasureType.CharCount];
                        row.otherData.wordCount += (int)msg.measures[(int)Message.MeasureType.WordCount];
                        row.otherData.messageCount += 1;
                        row.otherData.interrogationCount += (int)msg.measures[(int)Message.MeasureType.InterrogationPresence];
                        row.otherData.exclamationCount += (int)msg.measures[(int)Message.MeasureType.ExclamationPresence];
                        row.otherData.reticencesCount += (int)msg.measures[(int)Message.MeasureType.ReticencePresence];
                        row.otherData.commaCount += (int)msg.measures[(int)Message.MeasureType.CommaPresence];
                        row.otherData.linkCount += (int)msg.measures[(int)Message.MeasureType.LinkPresence];

                        row.otherData.euCount += (msg.text.Contains("Eu ") || msg.text.Contains(" eu ")) ? 1 : 0;
                        row.otherData.tuCount += (msg.text.Contains("Tu ") || msg.text.Contains(" tu ")) ? 1 : 0;
                    }
                }

                data.Add(row);
            }
            data.Sort(Comparer<DataRow>.Create((d1, d2) => (d2.selfData.charCount + d2.otherData.charCount).CompareTo(d1.selfData.charCount + d1.otherData.charCount)));

            using (StreamWriter file =
            new StreamWriter("multivariateData.txt"))
            {
                //file.WriteLine("Rank " + DataRow.getHeader());
                file.WriteLine(DataRow.getHeader());
                for (int i=0;i<data.Count;i++)
                {
                    //file.WriteLine((i + 1) + " " + data[i].ToString());
                    file.WriteLine(data[i].ToString());
                }
            }
        }

        public Dictionary<string, int> ownerLexic;
        public Dictionary<string, int> otherLexic;
        private void ComputeLexic()
        {
            ownerLexic = new Dictionary<string, int>();
            otherLexic = new Dictionary<string, int>();

            for (int i = 0; i < threads.Count; i++)
            {
                ChatThread chat = threads[i];

                for (int k = 0; k < chat.messages.Count; k++)
                {
                    Message msg = chat.messages[k];

                    string[] words = msg.text.Split(' ');

                    for (int w = 0; w < words.Length; w++)
                    {
                        string word = words[w];
                        if (msg.author == owner)
                        {
                            if (ownerLexic.ContainsKey(word))
                                ownerLexic[word]++;
                            else
                                ownerLexic.Add(word, 1);
                        }
                        else
                        {
                            if (otherLexic.ContainsKey(word))
                                otherLexic[word]++;
                            else
                                otherLexic.Add(word, 1);
                        }
                    }
                }
            }
        }

        public struct DataRow
        {
            public PersonMetrics selfData;
            public PersonMetrics otherData;

            public override string ToString()
            {
                String s = "";

                s += selfData.ToString();
                s += otherData.ToString();

                return s;
            }

            public static string getHeader()
            {
                return "selfChar selfWords selfMsgs selfQuest selfExclam selfReti selfComma selfLink selfEu selfTu otherChar otherWords otherMsgs otherQuest otherExclam otherReti otherComma otherLink otherEu otherTu";
            }
        }
        public struct PersonMetrics
        {
            public int charCount;
            public int wordCount;
            public int messageCount;
            public int interrogationCount;
            public int exclamationCount;
            public int reticencesCount;
            public int commaCount;
            public int linkCount;

            public int euCount;
            public int tuCount;

            public override string ToString()
            {
                String s = " ";

                s += charCount + " ";
                s += wordCount + " ";
                s += messageCount + " ";
                s += interrogationCount + " ";
                s += exclamationCount + " ";
                s += reticencesCount + " ";
                s += commaCount + " ";
                s += linkCount + " ";

                s += euCount + " ";
                s += tuCount + " ";

                return s;
            }
        }


        //Example: Thursday, 11 October 2012 at 03:35 UTC+01
        private DateTime processTimeFormat(string timeString)
        {
            string[] timeData = timeString.Split();
            string[] hourData = timeData[5].Split(':');
            DateTime time = new DateTime(Int32.Parse(timeData[3]), processMonth(timeData[2]), Int32.Parse(timeData[1]), Int32.Parse(hourData[0]), Int32.Parse(hourData[1]), 0, DateTimeKind.Utc);
            return time;
        }

        private int processMonth(string monthName)
        {
            switch (monthName)
            {
                case "January":
                    return 1;
                case "February":
                    return 2;
                case "March":
                    return 3;
                case "April":
                    return 4;
                case "May":
                    return 5;
                case "June":
                    return 6;
                case "July":
                    return 7;
                case "August":
                    return 8;
                case "September":
                    return 9;
                case "October":
                    return 10;
                case "November":
                    return 11;
                case "December":
                    return 12;
                default:
                    throw new FormatException("Month format exception while parsing log, expected month name, found: " + monthName);
            }
        }

        private string removeOuterSpaces(string entry)
        {
            while (entry[0] == ' ')
                entry = entry.Substring(1);

            while (entry[entry.Length - 1] == ' ')
                entry = entry.Substring(0, entry.Length - 1);

            return entry;
        }

        private ChatThread findThreadWith(List<Person> people)
        {
            for(int i=0;i<threads.Count;i++)
            {
                if (threads[i].people.Count == people.Count)
                {
                    bool found = true;
                    for (int k = 0; k < people.Count; k++)
                        if (!threads[i].people.Contains(people[k]))
                        {
                            found = false;
                            break;
                        }

                    if(found)
                        return threads[i];
                }
            }

            return null;
        }


        private void obfuscate()
        {
            if (obfuscatePeople)
            {
                for (int i = 0; i < people.Count; i++)
                {
                    if (people[i] != owner)
                    {
                        people[i].name = "Person " + i;
                    }
                }
            }

            
            if(obfuscateWords)
            {
                List<WordPresence> words = new List<WordPresence>();

                foreach(KeyValuePair<string, int> node in ownerLexic)
                {
                    words.Add(new WordPresence(node.Key, node.Value));
                }

                words.Sort((x, y) => x.count.CompareTo(y.count));

                int removeMost = 0;
                int pool = 140;
                while(words.Count > pool && removeMost < 40)
                {
                    words.RemoveAt(0);
                    removeMost++;
                }

                if (words.Count < pool)
                    pool = words.Count;

                Random rdm = new Random(0);
                Dictionary<string, string> obfuscator = new Dictionary<string, string>();
                foreach (KeyValuePair<string, int> node in ownerLexic)
                {
                    //obfuscator.Add(node.Key, words[rdm.Next(pool)].word);
                    obfuscator.Add(node.Key, "word " + rdm.Next(pool));
                }
                foreach (KeyValuePair<string, int> node in otherLexic)
                {
                    if (!obfuscator.ContainsKey(node.Key))
                    {
                        //obfuscator.Add(node.Key, words[rdm.Next(pool)].word);
                        obfuscator.Add(node.Key, "word " + rdm.Next(pool));
                    }
                }

                for(int i=0;i<threads.Count;i++)
                {
                    ChatThread tt = threads[i];
                    for(int m=0;m<tt.messages.Count;m++)
                    {
                        Message msg = tt.messages[m];

                        string[] tokens = msg.text.Split(' ');

                        msg.text = "";
                        for (int k = 0; k < tokens.Length; k++)
                            msg.text += obfuscator[tokens[k]] + " ";

                    }
                }
            }
        }

        private class WordPresence
        {
            public int count;
            public string word;

            public WordPresence(string word, int count)
            {
                this.word = word;
                this.count = count;
            }
        }

        public static FaceLog LoadLogAssynchronous(string location)
        {
            FaceLog log = new FaceLog();
            log.location = location;

            Thread loader = new Thread(log.loadFromHtmlv2);
            loader.Start();

            return log;
        }
        public static FaceLog LoadLog(string location)
        {
            //Check if preprocessed log is present

            //HTML SOLUTION
            FaceLog log = new FaceLog();
            log.location = location;

            log.loadFromHtmlv2();
            return log;
        }
    }
}

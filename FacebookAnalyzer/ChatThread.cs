using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FacebookAnalyzer
{
    public class ChatThread
    {
        public List<Person> people;
        public List<Message> messages;

        public ChatThread(List<Person> participants)
        {
            people = participants;
            messages = new List<Message>();
        }
    }
}

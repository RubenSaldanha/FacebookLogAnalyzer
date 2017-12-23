using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FacebookAnalyzer
{
    public class Person
    {
        public String name;
        List<Message> messages;

        public Person(string name)
        {
            this.name = name;
        }

        public override string ToString()
        {
            return name;
        }
    }
}

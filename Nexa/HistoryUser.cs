using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nexa
{
    internal class HistoryUser
    {
        public int Id { get; set; }

        public string FirstAndLastName { get; set; }

        public string YourID { get; set; }

        public string Bio { get; set; }

        public override string ToString()
        {
            return FirstAndLastName;
        }
    }
}

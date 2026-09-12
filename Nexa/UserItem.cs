using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nexa
{
    internal class UserItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string YourID { get; set; }

        public override string ToString()
        {
            return Name + "   @" + YourID;
        }
    }
}

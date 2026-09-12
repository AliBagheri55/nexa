using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nexa
{
    internal class GroupInfo
    {
        public int Id { get; set; }
        public string GroupName { get; set; }
        public string GroupBio { get; set; }
        public byte[] GroupPhoto { get; set; }
        public int CreatedBy { get; set; }

        public override string ToString()
        {
            return GroupName;
        }
    }
}

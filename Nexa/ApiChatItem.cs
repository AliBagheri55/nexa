using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nexa
{
    public class ApiChatItem
    {
        public string Type { get; set; }

        public int UserId { get; set; }

        public string YourID { get; set; }

        public string FirstAndLastName { get; set; }

        public bool IsBlocked { get; set; }

        public int Id { get; set; }

        public string GroupName { get; set; }

        public string GroupBio { get; set; }

        public string GroupPhoto { get; set; }

        public int CreatedBy { get; set; }

        public string InviteCode { get; set; }
    }
}

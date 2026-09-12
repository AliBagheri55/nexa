using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nexa
{
    internal class GroupMessage
    {
        public int Id { get; set; }

        public int GroupId { get; set; }

        public int SenderId { get; set; }

        public string SenderName { get; set; }

        public string SenderYourID { get; set; }

        public string MessageText { get; set; }

        public DateTime SentAt { get; set; }

        public override string ToString()
        {
            return SenderName +
                   ": " +
                   MessageText;
        }
    }
}

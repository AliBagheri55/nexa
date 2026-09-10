using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nexa
{
    internal class MessageReaction
    {
        public int Id { get; set; }

        public int MessageId { get; set; }

        public int UserId { get; set; }

        public string ReactionType { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}

using System;

namespace Nexa
{
    internal class SavedMessage
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public int MessageId { get; set; }

        public string MessageText { get; set; }

        public DateTime SavedAt { get; set; }

        public override string ToString()
        {
            return MessageText +
                   "    |    " +
                   SavedAt.ToString("HH:mm  yyyy/MM/dd");
        }
    }
}
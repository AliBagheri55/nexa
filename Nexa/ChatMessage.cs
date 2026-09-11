using System;

namespace Nexa
{
    internal class ChatMessage
    {
        public int MessageId { get; set; }

        public string Text { get; set; }

        public bool IsRead { get; set; }

        public DateTime SentAt { get; set; }

        public string MessageType { get; set; }

        public byte[] VoiceData { get; set; }

        public string FileName { get; set; }

        public byte[] FileData { get; set; }

        public byte[] ImageData { get; set; }

        public byte[] VideoData { get; set; }

        public byte[] GifData { get; set; }

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }

        public string Reaction { get; set; }

        public int ReactionCount { get; set; }

        public override string ToString()
        {
            string result = Text ?? "";

            if (!string.IsNullOrWhiteSpace(Reaction))
            {
                result += "   " + Reaction;
            }

            return result;
        }
    }
}
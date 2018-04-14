using System;

namespace UnkoService
{
    public class Message
    {
        public string MessageType { get; set; }
        public string ClientName { get; set; }
        public string What { get; set; }
        public DateTimeOffset RecieveTime { get; set; }
    }

    public class JoinMessage : Message
    {
        public const string MessageTypeKeyword = "JoinMessage";
    }
}
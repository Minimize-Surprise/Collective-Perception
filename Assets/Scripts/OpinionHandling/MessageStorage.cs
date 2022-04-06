using System.Collections.Generic;
using System.Text;

namespace Assets
{
    public class MessageStorage
    {
        private readonly int maxSize;
        private HashSet<string> currentSenders = new HashSet<string>();
        private Queue<OpinionMessage> msgQueue = new Queue<OpinionMessage>();

        public MessageStorage(int maxSize)
        {
            this.maxSize = maxSize;
        }

        public void EnqueueNewMessage(OpinionMessage newMessage)
        {
            if (currentSenders.Contains(newMessage.sender)) return;

            currentSenders.Add(newMessage.sender);

            if (msgQueue.Count >= maxSize)
            {
                var rmMessage = msgQueue.Dequeue();
                currentSenders.Remove(rmMessage.sender);
            }
            
            msgQueue.Enqueue(newMessage);
        }

        public OpinionMessage[] GetMessageArray()
        {
            return this.msgQueue.ToArray();
        }

        public void Clear()
        {
            currentSenders = new HashSet<string>();
            msgQueue = new Queue<OpinionMessage>();
        }

        public string GetFocusInfoText()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"<b>Message Storage</b> currently with {msgQueue.Count}/{maxSize} messages:");
            foreach (var msg in msgQueue)
            {
                sb.AppendLine($" - <i>{msg.ToString()}</i>");
            }

            return sb.ToString();
        }
    }
}
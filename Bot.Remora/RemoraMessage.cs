using Bot.Api;

namespace Bot.Remora
{
    public class RemoraMessage : IMessage
    {
        public string Content { get; }

        public RemoraMessage(string content = "")
        {
            Content = content;
        }
    }
}

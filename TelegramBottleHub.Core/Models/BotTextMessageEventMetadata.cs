using Telegram.Bot.Args;

namespace TelegramBottleHub.Core.Models
{
    public class BotTextMessageEventMetadata : BotMessageEventMetadata
    {
        public MessageEventArgs MessageEventArgs { get; set; }
    }
}

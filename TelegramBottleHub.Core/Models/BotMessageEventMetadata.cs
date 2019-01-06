using Telegram.Bot.Types;

namespace TelegramBottleHub.Core.Models
{
    public abstract class BotMessageEventMetadata
    {
        public User From { get; set; }

        public Chat Chat { get; set; }
    }
}

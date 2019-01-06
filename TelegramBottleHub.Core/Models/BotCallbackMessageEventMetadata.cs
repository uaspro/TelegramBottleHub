using Telegram.Bot.Args;

namespace TelegramBottleHub.Core.Models
{
    public class BotCallbackMessageEventMetadata : BotMessageEventMetadata
    {
        public CallbackQueryEventArgs CallbackQueryEventArgs { get; set; }
    }
}

using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using TelegramBottleHub.General.Managers.Core;

namespace TelegramBottleHub.Core
{
    public sealed class TelegramClientManager : ManagerCore
    {
        private const string BotTokenKey = "AppSettings:BottleBotToken";

        public TelegramBotClient BotClient { get; }

        public TelegramClientManager(IConfigurationRoot configuration) : base(configuration)
        {
            BotClient = new TelegramBotClient(configuration[BotTokenKey]);
        }

        public void Start()
        {
            BotClient.StartReceiving();
        }

        public void Stop()
        {
            BotClient.StopReceiving();
        }

        public override void Dispose()
        {
            Stop();
        }
    }
}

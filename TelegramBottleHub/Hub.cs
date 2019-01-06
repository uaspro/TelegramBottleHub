using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using TelegramBottleHub.Core;
using TelegramBottleHub.Core.Bots;
using TelegramBottleHub.Db.Core.Managers;
using TelegramBottleHub.HubBot;
using TelegramBottleHub.KinoBot;

namespace TelegramBottleHub
{
    public sealed class Hub : IDisposable
    {
        public static Hub Instance { get; private set; }

        private readonly IConfigurationRoot _configuration;

        private readonly MongoDbManager _mongoDbManager;
        private readonly TelegramClientManager _telegramClientManager;

        private readonly List<BotCore> Bots = new List<BotCore>();

        private Hub(IConfigurationRoot configuration)
        {
            _configuration = configuration;

            _mongoDbManager = new MongoDbManager(configuration);
            _telegramClientManager = new TelegramClientManager(configuration);

            CreateBots();
        }
         
        private void CreateBots()
        {
            Bots.Add(new HubBottleBot(_telegramClientManager.BotClient, _mongoDbManager));
            Bots.Add(new KinoBottleBot(_telegramClientManager.BotClient, _mongoDbManager));
        }

        public void Start()
        {
            _telegramClientManager.Start();
        }

        public void Stop()
        {
            _telegramClientManager.Stop();
        }

        public static void Configure(IConfigurationRoot configuration)
        {
            Instance = new Hub(configuration);
        }

        public void Dispose()
        {
            _mongoDbManager.Dispose();
            _telegramClientManager.Dispose();        

            foreach(var bot in Bots)
            {
                bot.Dispose();
            }
        }
    }
}

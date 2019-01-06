using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using TelegramBottleHub.Core.Helpers;
using TelegramBottleHub.Core.Models;
using TelegramBottleHub.Db.Core.Managers;

namespace TelegramBottleHub.Core.Bots
{
    public abstract class BotCore : IDisposable
    {
        protected const int TimeoutSeconds = 10;

        protected abstract string BotId { get; }

        protected TelegramBotClient BotClient { get; private set; }

        protected MongoDbManager MongoDbManager { get; private set; }

        protected abstract Dictionary<string, Func<BotMessageEventMetadata, Task<bool>>> BotActions { get; }

        protected BotCore(TelegramBotClient botClient, MongoDbManager mongoDbManager)
        {
            BotClient = botClient;
            MongoDbManager = mongoDbManager;

            BotClient.OnMessage += Bot_OnMessage;
            BotClient.OnCallbackQuery += Bot_OnCallbackQuery;
        }

        protected virtual async void Bot_OnMessage(object sender, MessageEventArgs e)
        {
            var now = DateTime.UtcNow;
            if (e.Message.Text == null || (now - e.Message.Date).TotalSeconds > TimeoutSeconds)
            {
                return;
            }

            if (BotActions.ContainsKey(e.Message.Text))
            {
                try
                {
                    await BotActions[e.Message.Text](new BotTextMessageEventMetadata
                    {
                        From = e.Message.From,
                        Chat = e.Message.Chat,
                        MessageEventArgs = e
                    });
                }
                catch (Exception)
                {
                    // ignored, for now
                }
            }
        }
        
        protected virtual async void Bot_OnCallbackQuery(object sender, CallbackQueryEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(e.CallbackQuery.Data))
            {
                return;
            }

            var commandData = BotHelper.ParseCallbackDataString(e);
            if (BotActions.ContainsKey(commandData[0]))
            {
                try
                {
                    await BotActions[commandData[0]](new BotCallbackMessageEventMetadata
                    {
                        From = e.CallbackQuery.From,
                        Chat = e.CallbackQuery.Message.Chat,
                        CallbackQueryEventArgs = e
                    });
                }
                catch (Exception)
                {
                    // ignored, for now
                }
            }
        }

        public virtual void Dispose()
        {
            BotClient.OnMessage -= Bot_OnMessage;
            BotClient.OnCallbackQuery -= Bot_OnCallbackQuery;

            BotClient = null;
        }

        public override bool Equals(object obj)
        {
            var core = obj as BotCore;
            return core != null &&
                   BotId == core.BotId;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(BotId);
        }
    }
}

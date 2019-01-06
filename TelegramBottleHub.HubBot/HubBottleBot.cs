using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBottleHub.Core.Bots;
using TelegramBottleHub.Core.Extensions;
using TelegramBottleHub.Core.Helpers;
using TelegramBottleHub.Core.Models;
using TelegramBottleHub.Db.Core.Managers;
using TelegramBottleHub.KinoBot;

namespace TelegramBottleHub.HubBot
{
    public sealed class HubBottleBot : BotCore
    {
        public const string GetStartActionKey = "/start";

        protected override string BotId => nameof(HubBottleBot);

        protected override Dictionary<string, Func<BotMessageEventMetadata, Task<bool>>> BotActions => 
            new Dictionary<string, Func<BotMessageEventMetadata, Task<bool>>>
            {
                { GetStartActionKey, HubStart }
            };

        public HubBottleBot(TelegramBotClient botClient, MongoDbManager mongoDbManager) : base(botClient, mongoDbManager)
        {
        }

        private async Task<bool> HubStart(BotMessageEventMetadata eventMetadata)
        {
            await MongoDbManager.InsertOrUpdateUser(eventMetadata.From);

            const string startText = "Зараз доступні такі категорії:";
            var replyMarkup = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        BotHelper.GetInlineCallbackButton("📽️ Фільми", KinoBottleBot.GetMenuActionKey)
                    }
                });

            var callbackEventMetadata = eventMetadata as BotCallbackMessageEventMetadata;
            if (callbackEventMetadata == null) {
                await BotClient.SendTextMessageAsync(
                    chatId: eventMetadata.Chat,
                    text: startText,
                    replyMarkup: replyMarkup);
            }
            else
            {
                await BotClient.EditMessageTextAsync(
                    chatId: eventMetadata.Chat,
                    messageId: callbackEventMetadata.CallbackQueryEventArgs.CallbackQuery.Message.MessageId,
                    text: startText,
                    replyMarkup: replyMarkup);

                await BotClient.AnswerCallback(callbackEventMetadata.CallbackQueryEventArgs);
            }

            return true;
        }
    }
}

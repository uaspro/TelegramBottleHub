using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBottleHub.Core.Bots;
using TelegramBottleHub.Core.Helpers;
using TelegramBottleHub.Core.Models;
using TelegramBottleHub.Db.Core.Managers;
using TelegramBottleHub.KinoBot.Extensions;
using TelegramBottleHub.KinoBot.Helpers;
using TelegramBottleHub.KinoBot.Parsers.Core.Models;
using TelegramBottleHub.KinoBot.Scheduled;

namespace TelegramBottleHub.KinoBot
{
    public sealed class KinoBottleBot : BotCore
    {
        public const string StartActionKey = "/start";
        public const string GetMenuActionKey = "/" + nameof(KinoBottleBot) + "_menu";
        public const string GetAdditionalActionKey = "/" + nameof(KinoBottleBot) + "__additional";
        public const string GetSubscriptionsActionKey = "/" + nameof(KinoBottleBot) + "__subscriptions";
        public const string SubscribeActionKey = "/" + nameof(KinoBottleBot) + "_subscribe";
        public const string GetKinosListActionKey = "/" + nameof(KinoBottleBot) + "_list";
        public const string GetKinoTrailerActionKey = "/" + nameof(KinoBottleBot) + "_trailer";

        private const int DefaultKinosPageLimit = 10;

        protected override string BotId => nameof(KinoBottleBot);

        protected override Dictionary<string, Func<BotMessageEventMetadata, Task<bool>>> BotActions =>
            new Dictionary<string, Func<BotMessageEventMetadata, Task<bool>>>
            {
                { GetMenuActionKey, GetMenu },
                { GetAdditionalActionKey, GetAdditional },
                { GetSubscriptionsActionKey, GetSubscriptions },
                { SubscribeActionKey, Subscribe },
                { GetKinosListActionKey, GetKinosList },
                { GetKinoTrailerActionKey, GetKinoTrailer }
            };

        public KinoBottleBot(TelegramBotClient botClient, MongoDbManager mongoDbManager) : base(botClient, mongoDbManager)
        {
            KinoChecker.Start(mongoDbManager, botClient);
        }

        private async Task<bool> GetMenu(BotMessageEventMetadata eventMetadata)
        {
            var callbackEventMetadata = eventMetadata as BotCallbackMessageEventMetadata;
            if (callbackEventMetadata == null)
            {
                return false;
            }

            await BotClient.EditMessageTextAsync(
                chatId: eventMetadata.Chat,
                messageId: callbackEventMetadata.CallbackQueryEventArgs.CallbackQuery.Message.MessageId,
                text: "Що тебе цікавить?",
                replyMarkup: new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        BotHelper.GetInlineCallbackButton("📽️ Фільми, що зараз у прокаті", GetKinosListActionKey, Kino.KinoState.RunningOrSelling.ToString())
                    },
                    new[]
                    {
                        BotHelper.GetInlineCallbackButton("🎬 Фільми, що скоро вийдуть", GetKinosListActionKey, Kino.KinoState.ComingSoon.ToString())
                    },
                    new[]
                    {
                        BotHelper.GetInlineCallbackButton("⚙️ Додатково", GetAdditionalActionKey)
                    },
                    new[]
                    {
                        BotHelper.GetInlineCallbackButton("⬅️ Назад", StartActionKey)
                    }
                }));

            await BotClient.AnswerCallback(callbackEventMetadata.CallbackQueryEventArgs);

            return true;
        }

        private async Task<bool> GetAdditional(BotMessageEventMetadata eventMetadata)
        {
            var callbackEventMetadata = eventMetadata as BotCallbackMessageEventMetadata;
            if (callbackEventMetadata == null)
            {
                return false;
            }

            await BotClient.EditMessageTextAsync(
                chatId: eventMetadata.Chat,
                messageId: callbackEventMetadata.CallbackQueryEventArgs.CallbackQuery.Message.MessageId,
                text: "Додаткові функції:",
                replyMarkup: new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        BotHelper.GetInlineCallbackButton("🔔 Підписки", GetSubscriptionsActionKey)
                    },
                    new[]
                    {
                        BotHelper.GetInlineCallbackButton("⬅️ Назад", GetMenuActionKey)
                    }
                }));

            await BotClient.AnswerCallback(callbackEventMetadata.CallbackQueryEventArgs);

            return true;
        }

        private async Task<bool> GetSubscriptions(BotMessageEventMetadata eventMetadata)
        {
            return await GenerateSubscriptionsMessage(eventMetadata, GetAdditionalActionKey);
        }

        private async Task<bool> GenerateSubscriptionsMessage(BotMessageEventMetadata eventMetadata, string prevActionKey = null)
        {
            var markupGenerationResult = await GenerateSubscribeKeyboardMarkup(eventMetadata, prevActionKey);
            if (markupGenerationResult.replyMarkup == null)
            {
                return false;
            }
            
            var callbackEventMetadata = eventMetadata as BotCallbackMessageEventMetadata;
            if (prevActionKey == null)
            {
                await BotClient.SendTextMessageAsync(
                    chatId: eventMetadata.Chat,
                    text: GenerateSubscribeMessageText(markupGenerationResult.isSubscribed),
                    replyMarkup: markupGenerationResult.replyMarkup);
            }
            else
            {
                await BotClient.EditMessageTextAsync(
                    chatId: eventMetadata.Chat,
                    messageId: callbackEventMetadata.CallbackQueryEventArgs.CallbackQuery.Message.MessageId,
                    text: GenerateSubscribeMessageText(markupGenerationResult.isSubscribed),
                    replyMarkup: markupGenerationResult.replyMarkup);
            }

            await BotClient.AnswerCallback(
                callbackEventMetadata.CallbackQueryEventArgs,
                markupGenerationResult.isSubscribed ? "Ти стежиш за оновленнями" : "Ти не стежиш за оновленнями");

            return true;
        }

        private string GenerateSubscribeMessageText(bool isSubscribed)
        {
            return isSubscribed ? 
                "Ти стежиш за виходом нових фільмів" : 
                "Хочеш стежити за виходом нових фільмів у прокат?";
        }

        private async Task<(InlineKeyboardMarkup replyMarkup, bool isSubscribed)> 
            GenerateSubscribeKeyboardMarkup(BotMessageEventMetadata eventMetadata, string prevActionKey = null)
        {
            var callbackEventMetadata = eventMetadata as BotCallbackMessageEventMetadata;
            if (callbackEventMetadata == null)
            {
                return (null, false);
            }

            var callbackCommandData = BotHelper.ParseCallbackDataString(callbackEventMetadata.CallbackQueryEventArgs);
            if (string.IsNullOrWhiteSpace(prevActionKey) && callbackCommandData != null && callbackCommandData.Length > 1)
            {
                prevActionKey = callbackCommandData[1];
            }

            var isSubscribed = await MongoDbManager.GetUserIsSubscribed(eventMetadata.From);
            var subscribeButton = BotHelper.GetInlineCallbackButton(
                isSubscribed ? "❌ Не стежити" : "✔️ Почати стежити",
                SubscribeActionKey, prevActionKey);

            var keyboardMarkup = new List<InlineKeyboardButton[]>
            {
                new[]
                {
                    subscribeButton
                }
            };

            if (!string.IsNullOrWhiteSpace(prevActionKey))
            {
                keyboardMarkup.Add(new[]
                    {
                        BotHelper.GetInlineCallbackButton("⬅️ Назад", GetAdditionalActionKey)
                    });
            }

            return (new InlineKeyboardMarkup(keyboardMarkup), isSubscribed);
        }

        private async Task<bool> Subscribe(BotMessageEventMetadata eventMetadata)
        {
            await MongoDbManager.SubscribeUnsubscribeUser(eventMetadata.From, eventMetadata.Chat);

            return await GenerateSubscriptionsMessage(eventMetadata, string.Empty);
        }

        private async Task<bool> GetKinosList(BotMessageEventMetadata eventMetadata)
        {
            var callbackEventMetadata = eventMetadata as BotCallbackMessageEventMetadata;
            if (callbackEventMetadata == null)
            {
                return false;
            }

            var callbackCommandData = BotHelper.ParseCallbackDataString(callbackEventMetadata.CallbackQueryEventArgs);
            object kinoStateValue;
            if (callbackCommandData.Length < 2 || !Enum.TryParse(typeof(Kino.KinoState), callbackCommandData[1], out kinoStateValue))
            {
                return false;
            }

            int skip;
            if (callbackCommandData.Length < 3 || !int.TryParse(callbackCommandData[2], out skip))
            {
                skip = 0;
            }

            var kinoState = (Kino.KinoState) kinoStateValue;
            var dbKinos = await MongoDbManager.GetDbKinosByState(kinoState, skip, DefaultKinosPageLimit);
            if (!dbKinos.Any())
            {
                await BotClient.AnswerCallback(callbackEventMetadata.CallbackQueryEventArgs, "Фільмів не знайдено 😮");

                return false;
            }

            var sendMessagesTasks = new List<Task>();
            foreach (var kino in dbKinos)
            {
                await BotClient.SendKinoMessage(callbackEventMetadata.Chat, kino);
            }

            var dbKinosCount = await MongoDbManager.GetDbKinosByStateTotalCount(kinoState);
            var hasPrevPage = skip > 0;
            var hasNextPage = dbKinosCount - skip - DefaultKinosPageLimit > 0;
            var replyMarkupNextPrevButtons = new List<InlineKeyboardButton>();
            if (hasPrevPage)
            {
                replyMarkupNextPrevButtons.Add(
                    BotHelper.GetInlineCallbackButton(
                        $"⏪ Попередні ({DefaultKinosPageLimit})",
                        GetKinosListActionKey,
                        kinoState.ToString(), 
                        (skip - DefaultKinosPageLimit).ToString()));
            }

            if (hasNextPage)
            {
                var countKinosLeft = dbKinosCount - skip - DefaultKinosPageLimit;
                replyMarkupNextPrevButtons.Add(
                    BotHelper.GetInlineCallbackButton(
                        $"Наступні ({(countKinosLeft > DefaultKinosPageLimit ? DefaultKinosPageLimit : countKinosLeft % DefaultKinosPageLimit)}) ⏩",
                        GetKinosListActionKey,
                        kinoState.ToString(),
                        (skip + DefaultKinosPageLimit).ToString()));
            }

            if(kinoState == Kino.KinoState.RunningOrSelling)
            {
                var isSubscribed = await MongoDbManager.GetUserIsSubscribed(callbackEventMetadata.From);
                if (!isSubscribed)
                {
                    await GenerateSubscriptionsMessage(callbackEventMetadata);
                }
            }

            await BotClient.SendTextMessageAsync(
                chatId: callbackEventMetadata.Chat,
                text: "Навігація:",
                replyMarkup: new InlineKeyboardMarkup(new[]
                {
                    replyMarkupNextPrevButtons.ToArray(),
                    new[]
                    {
                        BotHelper.GetInlineCallbackButton("⬅️ Назад", GetMenuActionKey)
                    }
                }));

            await BotClient.AnswerCallback(callbackEventMetadata.CallbackQueryEventArgs);

            return true;
        }

        private async Task<bool> GetKinoTrailer(BotMessageEventMetadata eventMetadata)
        {
            var callbackEventMetadata = eventMetadata as BotCallbackMessageEventMetadata;
            if(callbackEventMetadata == null)
            {
                return false;
            }

            var eventCommandData = BotHelper.ParseCallbackDataString(callbackEventMetadata.CallbackQueryEventArgs);
            if(eventCommandData.Length < 2)
            {
                return false;
            }

            var kinoCode = eventCommandData[1];
            var kino = await MongoDbManager.GetDbKinoByCode(kinoCode);
            if(kino == null)
            {
                return false;
            }

            await BotClient.SendTextMessageAsync(
                chatId: eventMetadata.Chat,
                text: kino.TrailerUrl,
                replyMarkup: new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        BotHelper.GetInlineCallbackButton("⬅️ Назад", GetMenuActionKey)
                    }
                }));

            await BotClient.AnswerCallback(callbackEventMetadata.CallbackQueryEventArgs);

            return true;
        }
    }
}

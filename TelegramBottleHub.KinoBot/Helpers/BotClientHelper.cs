using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBottleHub.Core.Helpers;
using TelegramBottleHub.KinoBot.Parsers.Core.Models;

namespace TelegramBottleHub.KinoBot.Helpers
{
    public static class BotClientHelper
    {
        public static async Task SendKinoMessage(this TelegramBotClient botClient, ChatId chat, Kino kino)
        {
            var inlineKeyboardMarkup = new List<InlineKeyboardButton>();
            if(!string.IsNullOrWhiteSpace(kino.TrailerUrl))
            {
                inlineKeyboardMarkup.Add(
                    BotHelper.GetInlineCallbackButton("🎥 Трейлер", KinoBottleBot.GetKinoTrailerActionKey, kino.ExternalCode));
            }

            if (!string.IsNullOrWhiteSpace(kino.Url))
            {
                inlineKeyboardMarkup.Add(InlineKeyboardButton.WithUrl("ℹ️ Детальніше", kino.Url));
            }
            
            await botClient.SendPhotoAsync(
                chatId: chat,
                photo: kino.ImageUrl,
                caption: $"<b>{kino.Name}</b>" + (kino.StartRunningDate != null ? $"\n(з {kino.StartRunningDate.Value.ToString("dd.MM.yyyy")})" : string.Empty),
                parseMode: ParseMode.Html,
                replyMarkup: new InlineKeyboardMarkup(inlineKeyboardMarkup));
        }
    }
}

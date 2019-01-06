using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBottleHub.Core.Helpers
{
    public static class BotHelper
    {
        private const char DefaultCallbackDataDelimiter = '|';

        public static string CreateCallbackDataString(params string[] callbackParams)
        {
            return string.Join(DefaultCallbackDataDelimiter, callbackParams);
        }

        public static string[] ParseCallbackDataString(CallbackQueryEventArgs callbackQueryEventArgs)
        {
            return callbackQueryEventArgs.CallbackQuery.Data.Split(DefaultCallbackDataDelimiter);
        }

        public static InlineKeyboardButton GetInlineCallbackButton(string text, params string[] data)
        {
            return InlineKeyboardButton.WithCallbackData(text, CreateCallbackDataString(data));
        }

        public static async Task AnswerCallback(this TelegramBotClient botClient, CallbackQueryEventArgs callbackQueryEventArgs, string text = null)
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQueryId: callbackQueryEventArgs.CallbackQuery.Id,
                text: text);
        }
    }
}

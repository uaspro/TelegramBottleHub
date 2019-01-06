using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBottleHub.Core.Helpers;
using TelegramBottleHub.Db.Core.Managers;
using TelegramBottleHub.KinoBot.Extensions;
using TelegramBottleHub.KinoBot.Parsers.Core.Models;
using TelegramBottleHub.KinoBot.Parsers.PlanetaKino;

namespace TelegramBottleHub.KinoBot.Scheduled
{
    public static class KinoChecker
    {
        private const int UpdateIntervalMinutes = 60;
        private const int SyncIntervalMinutes = 6 * 60;

        public static void Start(MongoDbManager mongoDbManager, TelegramBotClient botClient)
        {
            Task.Factory.StartNew(async () =>
            {
                while(true)
                {
                    try
                    {
                        var now = DateTime.UtcNow;
                        var kinosSync = await mongoDbManager.GetLastKinosSync();
                        if (kinosSync == null || (now - kinosSync.SyncDate).TotalMinutes > SyncIntervalMinutes)
                        {
                            var newComingSoonKinos = await GetComingSoonKinos(mongoDbManager);
                            var newRunningKinos = await GetRunningKinos(mongoDbManager);
                            if (newRunningKinos != null && newRunningKinos.Any())
                            {
                                await NotifySubscribers(newRunningKinos, mongoDbManager, botClient);
                            }

                            await mongoDbManager.InsertKinosCheck(newComingSoonKinos.Count, newRunningKinos.Count);
                        }
                    }
                    catch (Exception)
                    {
                        // ignored, for now
                    }
                    
                    Thread.Sleep(UpdateIntervalMinutes * 60 * 1000);
                }
            }, TaskCreationOptions.LongRunning);
        }

        private static async Task<IList<Kino>> GetComingSoonKinos(MongoDbManager mongoDbManager)
        {
            IList<Kino> kinosToInsert = null;
            try
            {
                var comingSoonKinos = PlanetaKinoParser.ParseComingSoonKinos();
                var dbKinos = await mongoDbManager.GetDbKinosByState(Kino.KinoState.ComingSoon);

                kinosToInsert = comingSoonKinos.Except(dbKinos).ToList();
                await mongoDbManager.InsertOrUpdateKinos(kinosToInsert);

                var kinosNotComingSoon = dbKinos.Except(comingSoonKinos).ToList();
                foreach (var kino in kinosNotComingSoon)
                {
                    await mongoDbManager.SetKinoState(kino, Kino.KinoState.Undefined);
                }
            }
            catch (Exception)
            {
                // ignored
            }

            return kinosToInsert;
        }

        private static async Task<IList<Kino>> GetRunningKinos(MongoDbManager mongoDbManager)
        {
            IList<Kino> kinosToInsert = null;
            try
            {
                var todayKinos = PlanetaKinoParser.ParseTodayKinos();
                var dbKinos = await mongoDbManager.GetDbKinosByState(Kino.KinoState.Running);

                kinosToInsert = todayKinos.Except(dbKinos).ToList();
                await mongoDbManager.InsertOrUpdateKinos(kinosToInsert);

                var kinosStoppedRunning = dbKinos.Except(todayKinos).ToList();
                foreach (var kino in kinosStoppedRunning)
                {
                    await mongoDbManager.SetKinoState(kino, Kino.KinoState.StoppedRunning);
                }
            }
            catch (Exception)
            {
                // ignored
            }

            return kinosToInsert;
        }

        private static async Task NotifySubscribers(IList<Kino> newKinos, MongoDbManager mongoDbManager, TelegramBotClient botClient)
        {
            try
            {
                var subscribers = await mongoDbManager.GetSubscribers();
                foreach (var subscriber in subscribers)
                {
                    try
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: subscriber.ChatId,
                            text: "У прокаті з'явились нові фільми!",
                            replyMarkup: new InlineKeyboardMarkup(new[]
                            {
                                new[]
                                {
                                    BotHelper.GetInlineCallbackButton("Переглянути", KinoBottleBot.GetKinosListActionKey, Kino.KinoState.Running.ToString())
                                }
                            }));
                    }
                    catch (Exception)
                    {
                        // ignored

                        await mongoDbManager.SubscribeUnsubscribeUser(subscriber.User, subscriber.ChatId);
                    }
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }
}

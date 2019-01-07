using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using TelegramBottleHub.Db.Core.Managers;
using TelegramBottleHub.General.Helpers;
using TelegramBottleHub.KinoBot.Models;
using TelegramBottleHub.KinoBot.Parsers.Core.Models;

namespace TelegramBottleHub.KinoBot.Extensions
{
    public static class KinoDbExtensions
    {
        public const string KinosDbCollectionName = nameof(KinoBottleBot) + "_kinos";
        public const string KinosSyncDbCollectionName = nameof(KinoBottleBot) + "_kinossync";
        public const string SubscribersDbCollectionName = nameof(KinoBottleBot) + "_subscribers";

        public static async Task<KinosCheck> GetLastKinosSync(this MongoDbManager mongoDbManager)
        {
            var kinosSyncCollection = mongoDbManager.Database.GetCollection<KinosCheck>(KinosSyncDbCollectionName);
            var lastKinosSync = await kinosSyncCollection.Find(Builders<KinosCheck>.Filter.Empty)
                .Sort(Builders<KinosCheck>.Sort.Descending(nameof(KinosCheck.SyncDate)))
                .FirstOrDefaultAsync();

            return lastKinosSync;
        }

        public static async Task InsertKinosCheck(this MongoDbManager mongoDbManager, int newComingSoonKinos, int newRunningKinosCount)
        {
            var now = TimeHelper.GetNow();
            var kinosSyncCollection = mongoDbManager.Database.GetCollection<KinosCheck>(KinosSyncDbCollectionName);
            await kinosSyncCollection.InsertOneAsync(new KinosCheck
            {
                SyncDate = now,
                NewComingSoonKinosCount = newComingSoonKinos,
                NewRunningKinosCount = newRunningKinosCount
            });
        }

        public static async Task<IList<Subscriber>> GetSubscribers(this MongoDbManager mongoDbManager)
        {
            var subscribersCollection = mongoDbManager.Database.GetCollection<Subscriber>(SubscribersDbCollectionName);
            var subscribers = await subscribersCollection.Find(Builders<Subscriber>.Filter.Eq($"{nameof(Subscriber.IsActive)}", true))
                .ToListAsync();

            return subscribers;
        }

        public static async Task<Subscriber> GetSubscriber(this MongoDbManager mongoDbManager, User user)
        {
            var subscribersCollection = mongoDbManager.Database.GetCollection<Subscriber>(SubscribersDbCollectionName);
            var subscriber = await subscribersCollection.Find(Builders<Subscriber>.Filter.Eq($"{nameof(Subscriber.User)}.{nameof(Subscriber.User.Id)}", user.Id))
                .FirstOrDefaultAsync();

            return subscriber;
        }

        public static async Task<bool> GetUserIsSubscribed(this MongoDbManager mongoDbManager, User user)
        {
            var subscriber = await mongoDbManager.GetSubscriber(user);
            return subscriber != null && subscriber.IsActive;
        }

        public static async Task<bool> SubscribeUnsubscribeUser(this MongoDbManager mongoDbManager, User user, ChatId chatId)
        {
            var subscribersCollection = mongoDbManager.Database.GetCollection<Subscriber>(SubscribersDbCollectionName);
            var subscriber = await mongoDbManager.GetSubscriber(user);
            var isSubscribed = subscriber != null && subscriber.IsActive;
            if (subscriber == null)
            {
                await subscribersCollection.InsertOneAsync(new Subscriber
                {
                    User = user,
                    ChatId = chatId.Identifier
                });
            }
            else
            {
                await subscribersCollection.UpdateOneAsync(Builders<Subscriber>.Filter.Eq($"{nameof(Subscriber.User)}.{nameof(Subscriber.User.Id)}", user.Id),
                                                           Builders<Subscriber>.Update.Set(nameof(Subscriber.IsActive), !isSubscribed));
            }

            return !isSubscribed;
        }

        private static FilterDefinition<Kino> GetRunningKinosByStateFilter(Kino.KinoState kinoState)
        {
            var now = TimeHelper.GetNow();
            var stateFilter = Builders<Kino>.Filter.Eq(nameof(Kino.State), kinoState);
            if (kinoState == Kino.KinoState.ComingSoon)
            {
                return stateFilter;
            }

            return Builders<Kino>.Filter.And(
                    stateFilter,
                    Builders<Kino>.Filter.Or(
                        Builders<Kino>.Filter.Eq(nameof(Kino.StartRunningDate), (DateTime?)null),
                        Builders<Kino>.Filter.Lt(nameof(Kino.StartRunningDate), now)));
        }

        public static async Task<long> GetDbKinosByStateTotalCount(this MongoDbManager mongoDbManager, Kino.KinoState kinoState)
        {
            var kinosCollection = mongoDbManager.Database.GetCollection<Kino>(KinosDbCollectionName);
            var kinosCount = await kinosCollection.CountDocumentsAsync(GetRunningKinosByStateFilter(kinoState));

            return kinosCount;
        }

        public static async Task<IList<Kino>> GetDbKinosByState(this MongoDbManager mongoDbManager, Kino.KinoState kinoState, int? skip = null, int? limit = null)
        {
            var kinosCollection = mongoDbManager.Database.GetCollection<Kino>(KinosDbCollectionName);
            var kinos = await kinosCollection.Find(GetRunningKinosByStateFilter(kinoState))
                .Sort(Builders<Kino>.Sort.Ascending(nameof(Kino.StartRunningDate)))
                .Skip(skip)
                .Limit(limit)
                .ToListAsync();

            return kinos;
        }

        public static async Task<Kino> GetDbKinoByCode(this MongoDbManager mongoDbManager, string externalCode)
        {
            var kinosCollection = mongoDbManager.Database.GetCollection<Kino>(KinosDbCollectionName);
            var kino = await kinosCollection.Find(Builders<Kino>.Filter.Eq(nameof(Kino.ExternalCode), externalCode))
                .FirstOrDefaultAsync();

            return kino;
        }

        public static async Task InsertOrUpdateKinos(this MongoDbManager mongoDbManager, IList<Kino> kinos)
        {
            var kinosCollection = mongoDbManager.Database.GetCollection<Kino>(KinosDbCollectionName);
            foreach(var kino in kinos)
            {
                var kinoDb = await mongoDbManager.GetDbKinoByCode(kino.ExternalCode);
                if(kinoDb == null)
                {
                    await kinosCollection.InsertOneAsync(kino);
                }
                else
                {
                    kino.Id = kinoDb.Id;
                    kino.State = kino.State > kinoDb.State ? kino.State : kinoDb.State;

                    await kinosCollection.ReplaceOneAsync(Builders<Kino>.Filter.Eq(nameof(Kino.ExternalCode), kino.ExternalCode), kino);
                }
            }
        }

        public static async Task SetKinoState(this MongoDbManager mongoDbManager, Kino kino, Kino.KinoState newState)
        {
            var kinosCollection = mongoDbManager.Database.GetCollection<Kino>(KinosDbCollectionName);
            await kinosCollection.UpdateOneAsync(Builders<Kino>.Filter.Eq(nameof(Kino.ExternalCode), kino.ExternalCode),
                                                 Builders<Kino>.Update.Set(nameof(Kino.State), newState));
        }
    }
}

using MongoDB.Driver;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using TelegramBottleHub.Core.Bots;
using TelegramBottleHub.Db.Core.Managers;

namespace TelegramBottleHub.Core.Extensions
{
    public static class CoreDbExtensions
    {
        public const string UsersDbCollectionName = nameof(BotCore) + "_users";

        public static async Task InsertOrUpdateUser(this MongoDbManager mongoDbManager, User user)
        {
            var usersCollection = mongoDbManager.Database.GetCollection<User>(UsersDbCollectionName);
            var dbUser = await usersCollection.Find(Builders<User>.Filter.Eq(nameof(User.Id), user.Id)).FirstOrDefaultAsync();
            if (dbUser == null)
            {
                await usersCollection.InsertOneAsync(user);
            }
            else
            {
                await usersCollection.ReplaceOneAsync(Builders<User>.Filter.Eq(nameof(User.Id), user.Id), user);
            }
        }
    }
}

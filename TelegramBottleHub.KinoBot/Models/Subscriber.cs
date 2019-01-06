using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Telegram.Bot.Types;

namespace TelegramBottleHub.KinoBot.Models
{
    public class Subscriber
    {
        [BsonId]
        public ObjectId Id { get; set; }

        public long ChatId { get; set; }

        public User User { get; set; }

        public bool IsActive { get; set; } = true;
    }
}

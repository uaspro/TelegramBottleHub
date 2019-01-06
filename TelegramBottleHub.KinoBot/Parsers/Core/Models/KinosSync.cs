using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace TelegramBottleHub.KinoBot.Parsers.Core.Models
{
    public class KinosCheck
    {
        [BsonId]
        public ObjectId Id { get; set; }

        public DateTime SyncDate { get; set; }

        public int NewComingSoonKinosCount { get; set; }

        public int NewRunningKinosCount { get; set; }
    }
}

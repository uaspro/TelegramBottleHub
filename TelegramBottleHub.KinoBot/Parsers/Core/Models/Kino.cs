using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace TelegramBottleHub.KinoBot.Parsers.Core.Models
{
    public class Kino
    {
        [BsonId]
        public ObjectId Id { get; set; }

        public string ExternalCode { get; set; }

        public string Name { get; set; }

        public string Url { get; set; }

        public string ImageUrl { get; set; }

        public string TrailerUrl { get; set; }

        public DateTime? StartRunningDate { get; set; }

        public KinoState State { get; set; }
        
        public List<ShowtimeDay> ShowtimeDays { get; set; } = new List<ShowtimeDay>();

        public override bool Equals(object obj)
        {
            var kino = obj as Kino;
            return kino != null &&
                   ExternalCode == kino.ExternalCode;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ExternalCode);
        }

        public enum KinoState
        {
            Undefined,
            ComingSoon,
            RunningOrSelling,
            StoppedRunning
        }
    }
}

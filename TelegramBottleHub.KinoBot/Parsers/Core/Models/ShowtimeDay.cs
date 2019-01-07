using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace TelegramBottleHub.KinoBot.Parsers.Core.Models
{
    public class ShowtimeDay
    {
        [BsonId]
        public ObjectId Id { get; set; }

        [BsonElement]
        public DateTime Day { get; }

        [BsonElement]
        public List<ShowtimeSchedule> Schedule { get; } = new List<ShowtimeSchedule>();

        public ShowtimeDay(DateTime day)
        {
            Day = day;
        }

        public override bool Equals(object obj)
        {
            var day = obj as ShowtimeDay;
            return day != null &&
                   Day == day.Day;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Day);
        }
    }
}

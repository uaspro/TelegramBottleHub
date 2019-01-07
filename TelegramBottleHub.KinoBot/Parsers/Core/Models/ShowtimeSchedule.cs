using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace TelegramBottleHub.KinoBot.Parsers.Core.Models
{
    public class ShowtimeSchedule
    {
        [BsonId]
        public ObjectId Id { get; set; }

        [BsonElement]
        public TimeSpan Time { get; set; }

        [BsonElement]
        public KinoTechnology Technology { get; set; }

        [BsonElement]
        public KinoFormat Format { get; set; }

        [BsonElement]
        public string BuyUrl { get; set; }

        public override bool Equals(object obj)
        {
            var schedule = obj as ShowtimeSchedule;
            return schedule != null &&
                   Time.Equals(schedule.Time) &&
                   Technology == schedule.Technology &&
                   Format == schedule.Format;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Time, Technology, Format);
        }

        public enum KinoTechnology
        {
            Undefined,
            CinetechPlus,
            Imax,
            _4dx
        }

        public enum KinoFormat
        {
            Undefined,
            _2d,
            _3d
        }
    }
}

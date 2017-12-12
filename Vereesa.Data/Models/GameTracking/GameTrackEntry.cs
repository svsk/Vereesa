using System;
using Vereesa.Data.Interfaces;

namespace Vereesa.Data.Models.GameTracking
{
    public class GameTrackEntry : IEntity
    {
        public static GameTrackEntry CreateInstance(string eventType)
        {
            return new GameTrackEntry
            {
                Id = Guid.NewGuid().ToString(),
                Event = eventType,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
        }

        public string Id { get; set; }
        public string Event { get; set; }
        public long Timestamp { get; set; }
    }
}
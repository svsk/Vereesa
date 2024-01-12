using System.Collections.Generic;
using Vereesa.Neon.Data.Interfaces;

namespace Vereesa.Neon.Data.Models.Giveaways
{
    public class Giveaway : IEntity
    {
        public string Id { get; set; }
        public string CreatedBy { get; set; }
        public long CreatedTimestamp { get; set; }
        public string TargetChannel { get; set; }
        public string Prize { get; set; }
        public int Duration { get; set; }
        public int NumberOfWinners { get; set; }
        public List<string> WinnerNames { get; set; }
        public ulong AnnouncementMessageId { get; set; }
    }
}

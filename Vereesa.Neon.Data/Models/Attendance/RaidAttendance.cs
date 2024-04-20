using System.Collections.Generic;
using Vereesa.Neon.Data.Interfaces;

namespace Vereesa.Neon.Data.Models.Attendance;

public class RaidAttendance : IEntity
{
    public RaidAttendance(string id, long timestamp, string zoneName, string zoneId, string logUrl)
    {
        Id = id;
        Timestamp = timestamp;
        ZoneId = zoneId;
        ZoneName = zoneName;
        LogUrl = logUrl;
        Attendees = new List<string>();
    }

    public string Id { get; set; }
    public bool Excluded { get; set; }
    public long Timestamp { get; set; }
    public string ZoneName { get; set; }
    public string ZoneId { get; set; }
    public List<string> Attendees { get; set; }
    public string LogUrl { get; set; }
}

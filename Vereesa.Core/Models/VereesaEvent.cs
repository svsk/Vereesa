using System;

namespace Vereesa.Core.Models;

public class VereesaEvent
{
    public ulong Id { get; set; }
    public ulong GuildId { get; set; }
    public string Name { get; set; }
    public VereesaEventStatus Status { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset? EndTime { get; set; }
}

public enum VereesaEventStatus
{
    Scheduled = 1,
    Active,
    Completed,
    Cancelled,
}

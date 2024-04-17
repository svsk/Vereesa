using Vereesa.Neon.Data.Interfaces;

namespace Vereesa.Neon.Data;

public class UserTimezoneSettings : IEntity
{
    public string Id { get; set; }
    public string UserId { get; set; }
    public string TimeZoneId { get; set; }
}

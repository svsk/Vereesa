using Vereesa.Neon.Data.Models.Wowhead;

namespace Vereesa.Neon.Integrations.Interfaces
{
    public interface IWowheadClient
    {
        TodayInWow GetTodayInWow();
    }
}

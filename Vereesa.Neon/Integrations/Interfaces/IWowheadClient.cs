using Vereesa.Neon.Data.Models.Wowhead;

namespace Vereesa.Neon.Integrations.Interfaces
{
    public interface IWowheadClient
    {
        Task<List<ElementalStorm>?> GetCurrentElementalStorms();
        Task<List<GrandHunt>?> GetCurrentGrandHunts();
        Task<List<RadiantEchoesEvent>?> GetCurrentRadiantEchoesEvents();
        Task<TodayInWowSection[]> GetTodayInWow();
    }
}

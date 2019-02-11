using Vereesa.Data.Models.Wowhead;

namespace Vereesa.Core.Integrations.Interfaces 
{
    public interface IWowheadClient 
    {
        TodayInWow GetTodayInWow();
    }
}
using System.Collections.Generic;
using System.Threading.Tasks;
using Vereesa.Core.Models;

namespace Vereesa.Core
{
    public interface IEventsClient
    {
        Task<List<VereesaEvent>> GetGuildEvents(ulong guildId);
        Task StartEvent(ulong guildId, ulong eventId);
    }
}

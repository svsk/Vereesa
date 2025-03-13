using System.Collections.Generic;
using System.Threading.Tasks;
using Vereesa.Core.Models;

namespace Vereesa.Core
{
    public interface IEventsClient
    {
        Task<List<ulong>> GetGuildEventParticipants(ulong guildId, ulong eventId);
        Task<List<VereesaEvent>> GetGuildEvents(ulong guildId, bool useCache = true);
        Task StartEvent(ulong guildId, ulong eventId);
    }
}

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Vereesa.Neon.Interfaces
{
    public interface IMessageProvider
    {
        Task<IEnumerable<IProvidedMessage>> CheckForNewMessagesAsync();
    }
}

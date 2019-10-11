using System.Collections.Generic;
using System.Threading.Tasks;

namespace Vereesa.Core.Interfaces
{
    public interface IMessageProvider
    {
        Task<IEnumerable<IProvidedMessage>> CheckForNewMessagesAsync();
    }
}
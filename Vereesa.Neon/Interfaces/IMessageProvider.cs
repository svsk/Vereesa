namespace Vereesa.Neon.Interfaces
{
    public interface IMessageProvider
    {
        Task<IEnumerable<IProvidedMessage>> CheckForNewMessagesAsync();
    }
}

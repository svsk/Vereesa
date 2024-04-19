namespace Vereesa.Neon.Exceptions;

public class NotSubscribedException : Exception
{
    public NotSubscribedException()
        : base("You aren't subscribed to this event.") { }
}

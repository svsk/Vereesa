namespace Vereesa.Neon.Exceptions;

public class AlreadySubscribedException : Exception
{
    public AlreadySubscribedException()
        : base("You are already subscribed to this event.") { }
}

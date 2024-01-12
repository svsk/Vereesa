using System;

namespace Vereesa.Neon.Exceptions
{
    public class UnexpectedResultException : Exception
    {
        public UnexpectedResultException(string message)
            : base(message) { }
    }
}

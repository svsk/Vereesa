using System;

namespace Vereesa.Core.Exceptions
{
    public class UnexpectedResultException : Exception
    {
        public UnexpectedResultException(string message)
            :base(message)
        {
        }
    }
}
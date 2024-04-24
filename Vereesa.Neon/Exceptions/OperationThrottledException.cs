namespace Vereesa.Neon.Exceptions
{
    [Serializable]
    internal class OperationThrottledException : Exception
    {
        public readonly TimeSpan MinWaitTime;

        public OperationThrottledException(TimeSpan minWaitTime)
        {
            MinWaitTime = minWaitTime;
        }
    }
}

using System;
using Microsoft.Extensions.Logging;

namespace Vereesa.Core.Infrastructure
{
    public class FileLogger : ILogger
    {
        private string _categoryName;
        private string _filePath;

        public FileLogger(string categoryName, string filePath) 
        {
            _categoryName = categoryName;
            _filePath = filePath;
        }   

        public IDisposable BeginScope<TState>(TState state)
        {
            //Todo: implement this correctly
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            //Todo: implement this correctly
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            //todo: implement this
            //Console.WriteLine(state);
        }
    }
}


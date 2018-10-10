using Microsoft.Extensions.Logging;

namespace Vereesa.Core.Infrastructure
{
    public class FileLoggerProvider : ILoggerProvider
    {
        private string _filePath;

        public FileLoggerProvider(string filePath) 
        {
            _filePath = filePath;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new FileLogger(categoryName, _filePath);
        }

        public void Dispose()
        {
        }
    }
}


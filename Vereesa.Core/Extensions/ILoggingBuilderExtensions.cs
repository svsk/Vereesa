using Microsoft.Extensions.Logging;
using Vereesa.Core.Infrastructure;

namespace Vereesa.Core.Extensions
{
    public static class ILoggingBuilderExtensions
    {
        public static void AddFile(this ILoggingBuilder builder, string file)
        {
            builder.AddProvider(new FileLoggerProvider(file));
        }
    }
}
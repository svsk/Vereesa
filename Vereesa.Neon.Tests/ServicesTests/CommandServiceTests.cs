using Xunit;
using Vereesa.Neon.Services;

namespace Vereesa.Neon.Tests.ServicesTests
{
    public class CommandServiceTests
    {
        [Fact]
        public void Test()
        {
            var test = CommandService.GetTagsByType<TimeUntilTag>(
                "Only {timeUntil:2018-08-14 00:00} remaining until Legion"
            );
            Assert.True(test != null);
        }
    }
}

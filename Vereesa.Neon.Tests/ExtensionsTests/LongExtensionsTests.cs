using Vereesa.Neon.Extensions;
using Xunit;

namespace Vereesa.Core.Tests.ExtensionsTests
{
    public class LongExtensionsTests
    {
        [Fact]
        public void ToHoursMinutesSeconds_TwoHoursInUnixTicks_SuccessfullyConvertsToString()
        {
            long twoHoursUnixTicks = 2 * 60 * 60;
            var twoHoursString = twoHoursUnixTicks.ToHoursMinutesSeconds();
            Assert.Equal("02:00:00", twoHoursString);
        }

        [Fact]
        public void ToDaysHoursMinutesSeconds_ThreeDaysInUnixTicks_SuccessfullyConvertsToString()
        {
            long threeDaysUnixTicks = 3 * 24 * 60 * 60;
            var threeDaysString = threeDaysUnixTicks.ToDaysHoursMinutesSeconds();
            Assert.Equal("03:00:00:00", threeDaysString);
        }
    }
}


using Vereesa.Core.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Vereesa.Core.Tests.ExtensionsTests
{
    [TestClass]
    public class LongExtensionsTests
    {
        [TestMethod]
        public void ToHoursMinutesSeconds_TwoHoursInUnixTicks_SuccessfullyConvertsToString()
        {
            long twoHoursUnixTicks = 2 * 60 * 60;
            var twoHoursString = twoHoursUnixTicks.ToHoursMinutesSeconds();
            Assert.AreEqual("02:00:00", twoHoursString);
        }

        [TestMethod]
        public void ToDaysHoursMinutesSeconds_ThreeDaysInUnixTicks_SuccessfullyConvertsToString()
        {
            long threeDaysUnixTicks = 3 * 24 * 60 * 60;
            var threeDaysString = threeDaysUnixTicks.ToDaysHoursMinutesSeconds();
            Assert.AreEqual("03:00:00:00", threeDaysString);
        }
    }
}
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Vereesa.Core.Services;

namespace Vereesa.Core.Tests.ServicesTests
{
    [TestClass]
    public class CommandServiceTests
    {
        [TestMethod]
        public void Test() 
        {
            var test = CommandService.GetTagsByType<TimeUntilTag>("Only {timeUntil:2018-08-14 00:00} remaining until Legion");
            Assert.IsTrue(test != null);
        }

    }
}
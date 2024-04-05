using System;
using Vereesa.Neon.Extensions;
using Vereesa.Neon.Data.Models.NeonApi;
using Xunit;

namespace Vereesa.Core.Tests
{
    public class ApplicationTests
    {
        [Fact]
        public void GetCreatedDateUtc_CalculatingCreatedDateUtc_CreatedDateUtcCalculatesCorrectly()
        {
            //Arrange
            var dateTimeString = "9/5/2018 0:12:14";

            var formatProvider = new System.Globalization.CultureInfo("en-US");
            var styles = System.Globalization.DateTimeStyles.None;
            DateTime.TryParse(dateTimeString, formatProvider, styles, out var parsedTime);

            //Act
            var createdDate = parsedTime.ToUtc("Europe/Berlin");

            //Assert
            Assert.Equal(4, createdDate.Day);
            Assert.Equal(9, createdDate.Month);
            Assert.Equal(2018, createdDate.Year);
            Assert.Equal(22, createdDate.Hour);
            Assert.Equal(12, createdDate.Minute);
        }
    }
}

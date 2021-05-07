using System;
using Vereesa.Core.Extensions;
using Vereesa.Data.Models.NeonApi;
using Xunit;

namespace Vereesa.Core.Tests
{

	public class ApplicationTests
	{

		[Fact]
		public void GetCreatedDateUtc_CalculatingCreatedDateUtc_CreatedDateUtcCalculatesCorrectly()
		{
			//Arrange
			var app = new ApplicationListItem();
			DateTime.TryParse("9/5/2018 0:12:14", out var parsedTime);

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
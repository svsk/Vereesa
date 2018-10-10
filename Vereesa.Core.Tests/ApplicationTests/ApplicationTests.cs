using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Vereesa.Data.Models.NeonApi;

namespace Vereesa.Core.Tests
{
    [TestClass]
    public class ApplicationTests
    {
        
        [TestMethod]
        public void GetCreatedDateUtc_CalculatingCreatedDateUtc_CreatedDateUtcCalculatesCorrectly()
        {
            //Arrange
            var app = new Application();
            app.ApplicationQuestions = new List<ApplicationQuestion>();

            app.ApplicationQuestions.Add(new ApplicationQuestion {
                Question = "Timestamp",
                Answer = "9/5/2018 0:12:14"
            });

            //Act
            var createdDate = app.GetCreatedDateUtc("Europe/Berlin");

            //Assert
            Assert.AreEqual(4, createdDate.Day);
            Assert.AreEqual(9, createdDate.Month);
            Assert.AreEqual(2018, createdDate.Year);
            Assert.AreEqual(22, createdDate.Hour);
            Assert.AreEqual(12, createdDate.Minute);
        }
    }
}
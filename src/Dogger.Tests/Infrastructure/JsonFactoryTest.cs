using System;
using System.Text.Json;
using System.Threading.Tasks;
using Dogger.Domain.Services.PullDog;
using Dogger.Infrastructure;
using Dogger.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dogger.Tests.Infrastructure
{
    [TestClass]
    public class JsonFactoryTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Serialize_ConfigurationFileWithExpiryInMinutes_SerializesCorrectly()
        {
            //Arrange
            var configurationFile = new ConfigurationFile()
            {
                Expiry = TimeSpan.FromMinutes(1)
            };

            //Act
            var json = JsonSerializer.Serialize(configurationFile, JsonFactory.GetOptions());

            //Assert
            Assert.AreEqual(@"{""isLazy"":false,""dockerComposeYmlFilePaths"":[""docker-compose.yml""],""expiry"":""00:01:00"",""conversationMode"":""singleComment""}", json);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Serialize_ConfigurationFileWithExpiryInDays_SerializesCorrectly()
        {
            //Arrange
            var configurationFile = new ConfigurationFile(Array.Empty<string>())
            {
                Expiry = TimeSpan.FromDays(1)
            };

            //Act
            var json = JsonSerializer.Serialize(configurationFile, JsonFactory.GetOptions());

            //Assert
            Assert.AreEqual(@"{""isLazy"":false,""dockerComposeYmlFilePaths"":[""docker-compose.yml""],""expiry"":""1.00:00:00"",""conversationMode"":""singleComment""}", json);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Serialize_ConfigurationFileWithExpiryInMoreThanAYear_SerializesCorrectly()
        {
            //Arrange
            var configurationFile = new ConfigurationFile(Array.Empty<string>())
            {
                Expiry = TimeSpan.FromDays(367)
            };

            //Act
            var json = JsonSerializer.Serialize(configurationFile, JsonFactory.GetOptions());

            //Assert
            Assert.AreEqual(@"{""isLazy"":false,""dockerComposeYmlFilePaths"":[""docker-compose.yml""],""expiry"":""367.00:00:00"",""conversationMode"":""singleComment""}", json);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Serialize_ConfigurationFileWithNoExpiry_SerializesCorrectly()
        {
            //Arrange
            var configurationFile = new ConfigurationFile(Array.Empty<string>());

            //Act
            var json = JsonSerializer.Serialize(configurationFile, JsonFactory.GetOptions());

            //Assert
            Assert.AreEqual(@"{""isLazy"":false,""dockerComposeYmlFilePaths"":[""docker-compose.yml""],""expiry"":""00:00:00"",""conversationMode"":""singleComment""}", json);
        }
    }
}

using System.Net;
using System.Net.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using Dogger.Tests.TestHelpers;
using Dogger.Tests.TestHelpers.Environments.Dogger;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Dogger.Tests
{
    [TestClass]
    public class ErrorHandlingTest
    {
        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task ThrowError_AnonymousUserAndProductionEnvironment_ReturnsNoErrorDetails()
        {
            //Arrange
            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync(new DoggerEnvironmentSetupOptions()
            {
                EnvironmentName = Environments.Production
            });

            //Act
            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync("http://localhost:14568/errors/throw");

            //Assert
            Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);

            var responseString = await response.Content.ReadAsStringAsync();
            var responseObject = JsonSerializer.Deserialize<ProblemDetails>(responseString);
            Assert.AreEqual("An error occured while processing your request.", responseObject.Title);
            Assert.IsNull(responseObject.Detail);
            Assert.AreEqual(500, responseObject.Status);
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task ThrowError_AnonymousUserAndDevelopmentEnvironment_ReturnsNoErrorDetails()
        {
            //Arrange
            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync(new DoggerEnvironmentSetupOptions()
            {
                EnvironmentName = Environments.Development
            });

            //Act
            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync("http://localhost:14568/errors/throw");

            //Assert
            Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);

            var responseString = await response.Content.ReadAsStringAsync();
            var responseObject = JsonSerializer.Deserialize<ProblemDetails>(responseString);
            Assert.AreNotEqual("This is some error.", responseObject.Title);
            Assert.IsNotNull(responseObject.Title);
            Assert.IsNotNull(responseObject.Detail);
            Assert.AreEqual(500, responseObject.Status);
        }
    }
}

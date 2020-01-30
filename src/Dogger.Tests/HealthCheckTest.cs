using System.Net.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using Dogger.Tests.TestHelpers;

namespace Dogger.Tests
{
    [TestClass]
    public class HealthCheckTest
    {
        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task HealthCheck_HealthEndpoint_ReturnsOk()
        {
            //Arrange
            await using var environment = await IntegrationTestEnvironment.CreateAsync();

            //Act
            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync("http://localhost:14568/health");

            //Assert
            response.EnsureSuccessStatusCode();
        }
    }
}

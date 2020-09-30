using System.Linq;
using Dogger.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Octokit;

namespace Dogger.Tests.Domain.Controllers
{
    [TestClass]
    public class ControllersTest
    {
        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public void AllControllersCanBeResolved()
        {
            //Arrange
            var controllerTypes = typeof(Startup)
                .Assembly
                .GetTypes()
                .Where(x => x.IsClass)
                .Where(x => x.Name.EndsWith(nameof(Controller)));

            var serviceProvider = TestServiceProviderFactory.CreateUsingStartup(services =>
            {
                services.AddSingleton(Substitute.For<IGitHubClient>());
            });

            //Act
            var controllerInstances = controllerTypes
                .Select(stateType => serviceProvider.GetRequiredService(stateType))
                .ToArray();

            //Assert
            Assert.AreNotEqual(0, controllerInstances.Length);

            foreach(var stateInstance in controllerInstances)
            {
                Assert.IsNotNull(stateInstance);
            }
        }
    }
}

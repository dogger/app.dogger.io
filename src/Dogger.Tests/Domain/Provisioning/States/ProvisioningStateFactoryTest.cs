using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Threading.Tasks;
using Dogger.Domain.Services.Provisioning.States;
using Dogger.Tests.TestHelpers;
using Dogger.Tests.TestHelpers.Environments.Dogger;

namespace Dogger.Tests.Domain.Provisioning.States
{
    [TestClass]
    public class ProvisioningStateFactoryTest
    {
        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Create_AllStatesCanBeCreated()
        {
            //Arrange
            var stateTypes = typeof(Startup)
                .Assembly
                .GetTypes()
                .Where(x => x.IsClass)
                .Where(x => !x.IsAbstract)
                .Where(x => !x.IsNestedPrivate)
                .Where(x => x
                    .GetInterfaces()
                    .Contains(typeof(IProvisioningState)));

            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync();

            var stateFactory = new ProvisioningStateFactory(environment.ServiceProvider);

            //Act
            var stateInstances = stateTypes
                .Select(stateType => typeof(IProvisioningStateFactory)
                    .GetMethod(nameof(IProvisioningStateFactory.Create))
                    ?.MakeGenericMethod(stateType))
                .Where(createMethod => createMethod != null)
                .Select(createMethod => createMethod
                    .Invoke(stateFactory, new object[] { null }))
                .ToArray();

            //Assert
            Assert.AreNotEqual(0, stateInstances.Length);

            foreach(var stateInstance in stateInstances)
            {
                Assert.IsNotNull(stateInstance);
            }
        }
    }
}

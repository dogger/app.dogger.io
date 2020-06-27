using System.Linq;
using System.Threading.Tasks;
using Dogger.Domain.Services.Provisioning.Stages;
using Dogger.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dogger.Tests.Domain.Services.Provisioning.Stages
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
                    .Contains(typeof(IProvisioningStage)));

            await using var environment = await IntegrationTestEnvironment.CreateAsync();

            var stateFactory = new ProvisioningStageFactory(environment.ServiceProvider);

            //Act
            var stateInstances = stateTypes
                .Select(stateType => typeof(IProvisioningStageFactory)
                    .GetMethod(nameof(IProvisioningStageFactory.Create))
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

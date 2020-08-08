using System.Threading.Tasks;
using Dogger.Tests.TestHelpers.Environments;
using Dogger.Tests.TestHelpers.Environments.Dogger;

namespace Dogger.Setup.Tests.TestHelpers.Environments
{
    class DoggerSetupIntegrationTestEnvironment : IntegrationTestEnvironment<DoggerSetupEnvironmentSetupOptions>
    {
        private DoggerSetupIntegrationTestEnvironment(DoggerSetupEnvironmentSetupOptions options = null) : base(options)
        {
            
        }

        public static async Task<DoggerSetupIntegrationTestEnvironment> CreateAsync(DoggerSetupEnvironmentSetupOptions options = null)
        {
            var environment = new DoggerSetupIntegrationTestEnvironment(options);
            await environment.InitializeAsync();

            return environment;
        }

        protected override IIntegrationTestEntrypoint GetEntrypoint(DoggerSetupEnvironmentSetupOptions options)
        {
            return new DoggerSetupStartupEntrypoint(options);
        }
    }
}

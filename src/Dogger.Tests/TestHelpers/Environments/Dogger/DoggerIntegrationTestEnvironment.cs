using System.Threading.Tasks;

namespace Dogger.Tests.TestHelpers.Environments.Dogger
{
    class DoggerIntegrationTestEnvironment : IntegrationTestEnvironment<DoggerEnvironmentSetupOptions>
    {
        private DoggerIntegrationTestEnvironment(DoggerEnvironmentSetupOptions options = null) : base(options)
        {
            
        }

        public static async Task<DoggerIntegrationTestEnvironment> CreateAsync(DoggerEnvironmentSetupOptions options = null)
        {
            var environment = new DoggerIntegrationTestEnvironment(options);
            await environment.InitializeAsync();

            return environment;
        }

        protected override IIntegrationTestEntrypoint GetEntrypoint(DoggerEnvironmentSetupOptions options)
        {
            return new DoggerStartupEntrypoint(options);
        }
    }
}

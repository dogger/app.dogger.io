using Dogger.Setup.Domain.Services;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DoggerIocRegistry = Dogger.Infrastructure.Ioc.IocRegistry;

namespace Dogger.Setup.Infrastructure
{
    public class IocRegistry : DoggerIocRegistry
    {
        public IocRegistry(
            IServiceCollection services,
            IConfiguration configuration) : base(services, configuration)
        {
            
        }

        public override void Register()
        {
            base.Register();

            ConfigureMediatr();
            ConfigureOptions();
            ConfigureDogfeeding();
        }

        private void ConfigureMediatr()
        {
            this.Services.AddMediatR(typeof(IocRegistry).Assembly);
        }

        private void ConfigureOptions()
        {
            Services.Configure<DogfeedOptions>(Configuration.GetSection("Dogfeed"));
        }

        private void ConfigureDogfeeding()
        {
            Services.AddTransient<IDogfeedService, DogfeedService>();
        }
    }
}

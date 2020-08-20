using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dogger.Infrastructure.Ioc;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Dogger.Tests.Reflection
{
    [TestClass]
    public class MediatorRegistrationTest
    {
        [TestMethod]
        public async Task ScanThroughAllRequestHandlers_ResolveWithIoc_AllDependenciesAreRegistered()
        {
            //Arrange
            var types = typeof(Program)
                .Assembly
                .DefinedTypes
                .Where(x => x.IsClass)
                .Select(x => x
                    .GetInterfaces()
                    .FirstOrDefault(i =>
                        i.IsGenericType &&
                        (i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>))))
                .Where(x => x != null);

            var serviceCollection = new ServiceCollection();

            var registry = new IocRegistry(serviceCollection, Substitute.For<IConfiguration>());
            registry.Register();

            var serviceProvider = serviceCollection.BuildServiceProvider();

            //Act & Assert
            foreach (var type in types)
            {
                serviceProvider.GetRequiredService(type);
            }
        }
    }
}

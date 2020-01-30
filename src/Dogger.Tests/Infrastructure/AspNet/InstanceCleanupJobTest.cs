using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dogger.Domain.Commands.Clusters.EnsureClusterWithId;
using Dogger.Domain.Commands.Instances.DeleteInstanceByName;
using Dogger.Domain.Models;
using Dogger.Domain.Queries.Amazon.Lightsail.GetLightsailInstanceByName;
using Dogger.Infrastructure.AspNet;
using Dogger.Infrastructure.Time;
using Dogger.Tests.TestHelpers;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Instance = Amazon.Lightsail.Model.Instance;

namespace Dogger.Tests.Infrastructure.AspNet
{
    [TestClass]
    public class InstanceCleanupJobTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task OnTick_NoDemoInstanceFoundInLightsailAndDatabase_DoesNothing()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(
                    Arg.Any<GetLightsailInstanceByNameQuery>(),
                    default)
                .Returns((Instance)null);

            fakeMediator
                .Send(
                    Arg.Is<EnsureClusterWithIdCommand>(args => args.Id == DataContext.DemoClusterId),
                    default)
                .Returns(new Cluster()
                {
                    Instances = new List<Dogger.Domain.Models.Instance>()
                });

            var fakeServiceProvider = GetServiceProviderForTimedServiceTesting();
            fakeServiceProvider
                .GetService(typeof(IMediator))
                .Returns(fakeMediator);

            //Act
            var job = new InstanceCleanupJob(fakeServiceProvider);
            try
            {
                await job.StartAsync(default);
            }
            finally
            {
                await job.StopAsync(default);
            }

            //Assert
            await fakeMediator
                .DidNotReceive()
                .Send(
                    Arg.Any<DeleteInstanceByNameCommand>(),
                    default);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task OnTick_NoDemoInstanceFoundInLightsailButFoundInDatabase_DeletesInstance()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(
                    Arg.Any<GetLightsailInstanceByNameQuery>(),
                    default)
                .Returns((Instance)null);

            fakeMediator
                .Send(
                    Arg.Is<EnsureClusterWithIdCommand>(args => args.Id == DataContext.DemoClusterId),
                    default)
                .Returns(new Cluster()
                {
                    Instances = new List<Dogger.Domain.Models.Instance>()
                    {
                        new Dogger.Domain.Models.Instance()
                    }
                });

            var fakeServiceProvider = GetServiceProviderForTimedServiceTesting();
            fakeServiceProvider
                .GetService(typeof(IMediator))
                .Returns(fakeMediator);

            //Act
            var job = new InstanceCleanupJob(fakeServiceProvider);
            try
            {
                await job.StartAsync(default);
            }
            finally
            {
                await job.StopAsync(default);
            }

            //Assert
            await fakeMediator
                .Received()
                .Send(
                    Arg.Any<DeleteInstanceByNameCommand>(),
                    default);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task OnTick_NoDatabaseDemoInstanceFoundButOldLightsailModelFound_DoesNothing()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(
                    Arg.Any<GetLightsailInstanceByNameQuery>(),
                    default)
                .Returns(new Instance()
                {
                    CreatedAt = DateTime.UtcNow.AddHours(-1)
                });

            fakeMediator
                .Send(
                    Arg.Is<EnsureClusterWithIdCommand>(args => args.Id == DataContext.DemoClusterId),
                    default)
                .Returns(new Cluster()
                {
                    Instances = new List<Dogger.Domain.Models.Instance>()
                });

            var fakeServiceProvider = GetServiceProviderForTimedServiceTesting();
            fakeServiceProvider
                .GetService(typeof(IMediator))
                .Returns(fakeMediator);

            //Act
            var job = new InstanceCleanupJob(fakeServiceProvider);
            try
            {
                await job.StartAsync(default);
            }
            finally
            {
                await job.StopAsync(default);
            }

            //Assert
            await fakeMediator
                .Received()
                .Send(
                    Arg.Any<DeleteInstanceByNameCommand>(),
                    default);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task OnTick_NoDatabaseDemoInstanceFoundButNewLightsailModelFound_DoesNothing()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(
                    Arg.Any<GetLightsailInstanceByNameQuery>(),
                    default)
                .Returns(new Instance()
                {
                    CreatedAt = DateTime.UtcNow
                });

            fakeMediator
                .Send(
                    Arg.Is<EnsureClusterWithIdCommand>(args => args.Id == DataContext.DemoClusterId),
                    default)
                .Returns(new Cluster()
                {
                    Instances = new List<Dogger.Domain.Models.Instance>()
                });

            var fakeServiceProvider = GetServiceProviderForTimedServiceTesting();
            fakeServiceProvider
                .GetService(typeof(IMediator))
                .Returns(fakeMediator);

            //Act
            var job = new InstanceCleanupJob(fakeServiceProvider);
            try
            {
                await job.StartAsync(default);
            }
            finally
            {
                await job.StopAsync(default);
            }

            //Assert
            await fakeMediator
                .DidNotReceive()
                .Send(
                    Arg.Any<DeleteInstanceByNameCommand>(),
                    default);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task OnTick_NewDemoInstanceFound_DoesNothing()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(
                    Arg.Any<GetLightsailInstanceByNameQuery>(),
                    default)
                .Returns(new Instance()
                {
                    CreatedAt = DateTime.UtcNow
                });

            fakeMediator
                .Send(
                    Arg.Is<EnsureClusterWithIdCommand>(args => args.Id == DataContext.DemoClusterId),
                    default)
                .Returns(new Cluster()
                {
                    Instances = new List<Dogger.Domain.Models.Instance>()
                    {
                        new Dogger.Domain.Models.Instance()
                    }
                });

            var fakeServiceProvider = GetServiceProviderForTimedServiceTesting();
            fakeServiceProvider
                .GetService(typeof(IMediator))
                .Returns(fakeMediator);

            //Act
            var job = new InstanceCleanupJob(fakeServiceProvider);
            try
            {
                await job.StartAsync(default);
            }
            finally
            {
                await job.StopAsync(default);
            }

            //Assert
            await fakeMediator
                .DidNotReceive()
                .Send(
                    Arg.Any<DeleteInstanceByNameCommand>(),
                    default);
        }

        private static IServiceProvider GetServiceProviderForTimedServiceTesting()
        {
            var fakeTime = Substitute.For<ITime>();
            fakeTime
                .CreateTimerAsync(
                    Arg.Any<TimeSpan>(),
                    Arg.Any<Func<Task>>())
                .Returns(async callInfo =>
                {
                    var callback = callInfo.Arg<Func<Task>>();
                    await callback();

                    return null;
                });

            var fakeServiceScopeFactory = Substitute.For<IServiceScopeFactory>();

            var fakeServiceScope = Substitute.For<IServiceScope>();
            fakeServiceScopeFactory
                .CreateScope()
                .Returns(fakeServiceScope);

            var fakeServiceProvider = Substitute.For<IServiceProvider>();
            fakeServiceScope.ServiceProvider.Returns(fakeServiceProvider);

            fakeServiceProvider
                .GetService(typeof(IServiceScopeFactory))
                .Returns(fakeServiceScopeFactory);

            fakeServiceProvider
                .GetService(typeof(ITime))
                .Returns(fakeTime);

            return fakeServiceProvider;
        }
    }
}

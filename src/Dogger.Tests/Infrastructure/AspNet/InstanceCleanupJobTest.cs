using System;
using System.Threading.Tasks;
using Dogger.Domain.Commands.Instances.DeleteInstanceByName;
using Dogger.Domain.Queries.Instances.GetExpiredInstances;
using Dogger.Infrastructure.AspNet;
using Dogger.Infrastructure.Time;
using Dogger.Tests.Domain.Models;
using Dogger.Tests.TestHelpers;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Dogger.Tests.Infrastructure.AspNet
{
    [TestClass]
    public class InstanceCleanupJobTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task OnTick_NoDemoInstanceFoundInLightsailButFoundInDatabase_DeletesInstance()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Any<GetExpiredInstancesQuery>())
                .Returns(new Dogger.Domain.Models.Instance[]
                {
                    new TestInstanceBuilder()
                        .WithName("some-instance-name-1"),
                    new TestInstanceBuilder()
                        .WithName("some-instance-name-2")
                        .Build()
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
                .Received(2)
                .Send(
                    Arg.Any<DeleteInstanceByNameCommand>(),
                    default);

            await fakeMediator
                .Received(1)
                .Send(
                    Arg.Is<DeleteInstanceByNameCommand>(args => args.Name == "some-instance-name-1"),
                    default);

            await fakeMediator
                .Received(1)
                .Send(
                    Arg.Is<DeleteInstanceByNameCommand>(args => args.Name == "some-instance-name-2"),
                    default);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task OnTick_NoExpiredInstancesFound_DoesNothing()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Any<GetExpiredInstancesQuery>())
                .Returns(new Dogger.Domain.Models.Instance[0]);

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
                .CreateTimer(
                    Arg.Any<TimeSpan>(),
                    Arg.Any<Func<Task>>())
                .Returns(callInfo =>
                {
                    var callback = callInfo.Arg<Func<Task>>();
                    callback()
                        .GetAwaiter()
                        .GetResult();

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

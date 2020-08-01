using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Lightsail;
using Amazon.Lightsail.Model;
using Dogger.Domain.Commands.Instances.ProvisionInstanceForUser;
using Dogger.Domain.Commands.Payment.SetActivePaymentMethodForUser;
using Dogger.Domain.Commands.Users.CreateUserForIdentity;
using Dogger.Domain.Queries.Plans.GetSupportedPlans;
using Dogger.Domain.Services.Amazon.Lightsail;
using Dogger.Domain.Services.Provisioning;
using Dogger.Domain.Services.Provisioning.Flows;
using Dogger.Infrastructure.Ssh;
using Dogger.Infrastructure.Time;
using Dogger.Tests.TestHelpers;
using Dogger.Tests.TestHelpers.Environments;
using Dogger.Tests.TestHelpers.Environments.Dogger;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Stripe;
using Instance = Amazon.Lightsail.Model.Instance;
using Plan = Dogger.Domain.Queries.Plans.GetSupportedPlans.Plan;

namespace Dogger.Tests.Domain.Commands.Instances
{
    [TestClass]
    public class ProvisionInstanceForUserCommandTest
    {
        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_ProperArgumentsGiven_UnprovisionedInstanceIsAddedToDatabase()
        {
            //Arrange
            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync(new DoggerEnvironmentSetupOptions()
            {
                IocConfiguration = services =>
                {
                    services.AddSingleton(Substitute.For<IProvisioningService>());
                }
            });

            var user = await environment.Mediator.Send(
                new CreateUserForIdentityCommand(
                    TestClaimsPrincipalFactory.CreateWithIdentityName("some-identity-name")));

            //Act
            await environment.Mediator.Send(
                new ProvisionInstanceForUserCommand(
                    new Plan(
                        "dummy",
                        1337,
                        new Bundle()
                        {
                            Price = 1337,
                            BundleId = "dummy"
                        },
                        Array.Empty<PullDogPlan>()),
                    user));

            //Assert
            await environment.WithFreshDataContext(async dataContext =>
            {
                var addedInstance = await dataContext
                    .Instances
                    .SingleAsync();

                Assert.IsNotNull(addedInstance);
                Assert.IsFalse(addedInstance.IsProvisioned);
            });
        }

        /// <summary>
        /// This test simulates a scenario that can happen during leap year (1 hour after midnight of 29th of February).
        /// </summary>
        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_ProperArgumentsGivenAtEndOfMonth_FullServerProvisioningFlowIsRunAndProperInstanceIsCreated()
        {
            //Arrange
            var lastDayOfLastMonth =
                new DateTime(
                        DateTime.Now.Year,
                        DateTime.Now.Month,
                        1)
                    .AddDays(-1);

            var fakeTimeProvider = Substitute.For<ITimeProvider>();
            fakeTimeProvider
                .UtcNow
                .Returns(lastDayOfLastMonth);

            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync(
                new DoggerEnvironmentSetupOptions()
                {
                    IocConfiguration = services =>
                    {
                        FakeOutMinimalLightsailFeaturesForFullProvisioning(services);

                        services.AddSingleton(fakeTimeProvider);
                    }
                });

            var provisioningService = environment
                .ServiceProvider
                .GetRequiredService<IProvisioningService>();

            var paymentMethodService = environment
                .ServiceProvider
                .GetRequiredService<PaymentMethodService>();

            var user = await environment.Mediator.Send(
                new CreateUserForIdentityCommand(
                    TestClaimsPrincipalFactory.CreateWithIdentityName("some-identity-name")));

            var paymentMethod = await CreatePaymentMethodAsync(paymentMethodService);
            await environment.Mediator.Send(
                new SetActivePaymentMethodForUserCommand(
                    user,
                    paymentMethod.Id));

            //Act
            await environment.Mediator.Send(
                new ProvisionInstanceForUserCommand(
                    new Plan(
                        "nano_2_0",
                        1337,
                        new Bundle()
                        {
                            Price = 1337,
                            BundleId = "nano_2_0"
                        },
                        Array.Empty<PullDogPlan>()),
                    user));

            await provisioningService.ProcessPendingJobsAsync();

            //Assert
            await environment.WithFreshDataContext(async dataContext =>
            {
                var provisionedInstance = await dataContext
                    .Instances
                    .Include(x => x.Cluster)
                    .ThenInclude(x => x.User)
                    .SingleAsync();

                Assert.IsTrue(provisionedInstance.IsProvisioned);
                Assert.AreEqual(provisionedInstance.Cluster.UserId, user.Id);
            });
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_ProperArgumentsGiven_FullServerProvisioningFlowIsRunAndProperInstanceIsCreated()
        {
            //Arrange
            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync(
                new DoggerEnvironmentSetupOptions()
                {
                    IocConfiguration = FakeOutMinimalLightsailFeaturesForFullProvisioning
                });

            var provisioningService = environment
                .ServiceProvider
                .GetRequiredService<IProvisioningService>();

            var paymentMethodService = environment
                .ServiceProvider
                .GetRequiredService<PaymentMethodService>();

            var user = await environment.Mediator.Send(
                new CreateUserForIdentityCommand(
                    TestClaimsPrincipalFactory.CreateWithIdentityName("some-identity-name")));

            var paymentMethod = await CreatePaymentMethodAsync(paymentMethodService);
            await environment.Mediator.Send(
                new SetActivePaymentMethodForUserCommand(
                    user,
                    paymentMethod.Id));

            //Act
            await environment.Mediator.Send(
                new ProvisionInstanceForUserCommand(
                    new Plan(
                        "nano_2_0",
                        1337,
                        new Bundle()
                        {
                            Price = 1337,
                            BundleId = "nano_2_0"
                        },
                        Array.Empty<PullDogPlan>()),
                    user));

            await provisioningService.ProcessPendingJobsAsync();

            //Assert
            await environment.WithFreshDataContext(async dataContext =>
            {
                var provisionedInstance = await dataContext
                    .Instances
                    .Include(x => x.Cluster)
                    .ThenInclude(x => x.User)
                    .SingleAsync();

                Assert.IsTrue(provisionedInstance.IsProvisioned);
                Assert.AreEqual(provisionedInstance.Cluster.UserId, user.Id);
            });
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_JobServiceThrowsError_NothingIsCommitted()
        {
            //Arrange
            var fakeProvisioningService = Substitute.For<IProvisioningService>();
            fakeProvisioningService
                .ScheduleJobAsync(Arg.Any<IProvisioningStateFlow>())
                .Throws(new TestException());

            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync(new DoggerEnvironmentSetupOptions()
            {
                IocConfiguration = services =>
                {
                    services.AddSingleton(fakeProvisioningService);
                }
            });

            var user = await environment.Mediator.Send(
                new CreateUserForIdentityCommand(
                    TestClaimsPrincipalFactory.CreateWithIdentityName("some-identity-name")));

            //Act
            var exception = await Assert.ThrowsExceptionAsync<TestException>(async () => 
                await environment.Mediator.Send(
                    new ProvisionInstanceForUserCommand(
                        new Plan(
                            "nano_2_0",
                            1337,
                            new Bundle()
                            {
                                Price = 1337,
                                BundleId = "nano_2_0"
                            },
                            Array.Empty<PullDogPlan>()),
                        user)));

            //Assert
            await environment.WithFreshDataContext(async dataContext =>
            {
                var addedInstance = await dataContext
                    .Instances
                    .SingleOrDefaultAsync();

                Assert.IsNotNull(exception);
                Assert.IsNull(addedInstance);
            });
        }

        private static void FakeOutMinimalLightsailFeaturesForFullProvisioning(
            IServiceCollection services)
        {
            var fakeSshClientFactory = Substitute.For<ISshClientFactory>();

            var fakeAmazonLightsail = Substitute.For<IAmazonLightsail>();
            fakeAmazonLightsail
                .CreateInstancesAsync(
                    Arg.Any<CreateInstancesRequest>(),
                    default)
                .Returns(new CreateInstancesResponse()
                {
                    Operations = new List<Operation>()
                });

            fakeAmazonLightsail
                .PutInstancePublicPortsAsync(
                    Arg.Any<PutInstancePublicPortsRequest>(),
                    default)
                .Returns(new PutInstancePublicPortsResponse()
                {
                    Operation = GenerateRandomSucceededOperation()
                });

            fakeAmazonLightsail
                .GetInstanceAsync(
                    Arg.Any<GetInstanceRequest>(),
                    default)
                .Returns(new GetInstanceResponse()
                {
                    Instance = new Instance()
                    {
                        State = new InstanceState()
                        {
                            Name = "running"
                        },
                        PublicIpAddress = "127.0.0.1"
                    }
                });

            var fakeLightsailOperationService = Substitute.For<ILightsailOperationService>();
            fakeLightsailOperationService
                .GetOperationsFromIdsAsync(
                    Arg.Any<IEnumerable<string>>())
                .Returns(new List<Operation>()
                {
                    GenerateRandomSucceededOperation()
                });

            services.AddSingleton(fakeLightsailOperationService);
            services.AddSingleton(fakeAmazonLightsail);
            services.AddSingleton(fakeSshClientFactory);
        }

        private static Operation GenerateRandomSucceededOperation()
        {
            return new Operation()
            {
                Id = Guid.NewGuid().ToString(),
                Status = OperationStatus.Succeeded
            };
        }

        private static async Task<PaymentMethod> CreatePaymentMethodAsync(PaymentMethodService paymentMethodService)
        {
            return await paymentMethodService
                .CreateAsync(new PaymentMethodCreateOptions()
                {
                    Card = new PaymentMethodCardCreateOptions()
                    {
                        Number = "4242424242424242",
                        Cvc = "123",
                        ExpMonth = 10,
                        ExpYear = 30
                    },
                    Type = "card"
                });
        }
    }
}

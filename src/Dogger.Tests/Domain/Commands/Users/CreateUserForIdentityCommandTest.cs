using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Dogger.Domain.Commands.Users.CreateUserForIdentity;
using Dogger.Domain.Models;
using Dogger.Infrastructure.Ioc;
using Dogger.Tests.TestHelpers;
using Dogger.Tests.TestHelpers.Environments;
using Dogger.Tests.TestHelpers.Environments.Dogger;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Stripe;

namespace Dogger.Tests.Domain.Commands.Users
{
    [TestClass]
    public class CreateUserForIdentityCommandTest
    {
        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_StripeExceptionThrown_NothingIsCommitted()
        {
            //Arrange
            var fakeIdentityName = Guid.NewGuid().ToString();
            var fakeIdentity = TestClaimsPrincipalFactory.CreateWithIdentityName(fakeIdentityName);

            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync(new DoggerEnvironmentSetupOptions()
            {
                IocConfiguration = services => services.AddSingleton(provider =>
                {
                    var partiallyFakeCustomerService = Substitute.ForPartsOf<CustomerService>(
                        provider.GetRequiredService<IOptionalService<IStripeClient>>().Value);
                    partiallyFakeCustomerService
                        .CreateAsync(Arg.Any<CustomerCreateOptions>())
                        .Throws(new TestException());

                    return partiallyFakeCustomerService;
                })
            });

            //Act
            var exception = await Assert.ThrowsExceptionAsync<TestException>(
                async () => await environment.Mediator.Send(new CreateUserForIdentityCommand(fakeIdentity)));

            //Assert
            var createdUser = await GetUserByIdentityNameAsync(environment, fakeIdentityName);

            Assert.IsNotNull(exception);
            Assert.IsNull(createdUser);
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_EmptyIdentityNameGiven_ExceptionIsThrown()
        {
            //Arrange
            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync();

            //Act
            var exception = await Assert.ThrowsExceptionAsync<IdentityNameNotProvidedException>(
                async () => await environment.Mediator.Send(new CreateUserForIdentityCommand(new ClaimsPrincipal())));

            //Assert
            Assert.IsNotNull(exception);
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_DevelopmentEnvironmentAndNoExistingStripeCustomer_NewStripeCustomerCreatedWithFakeEmail()
        {
            //Arrange
            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync();
            var customerService = environment.ServiceProvider.GetRequiredService<CustomerService>();

            var fakeIdentityName = Guid.NewGuid().ToString();
            var fakeIdentity = TestClaimsPrincipalFactory.CreateWithIdentityName(fakeIdentityName);

            //Act
            var createdUser = await environment.Mediator.Send(new CreateUserForIdentityCommand(fakeIdentity));

            //Assert
            var createdStripeCustomer = await customerService.GetAsync(createdUser.StripeCustomerId);

            Assert.IsNotNull(createdStripeCustomer);
            Assert.AreEqual(fakeIdentityName + "@example.com", createdStripeCustomer.Email);
            Assert.AreEqual(createdUser.Id.ToString(), createdStripeCustomer.Metadata["UserId"]);
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_ProductionEnvironmentAndExistingStripeCustomer_NewStripeCustomerCreated()
        {
            //Arrange
            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync(new DoggerEnvironmentSetupOptions()
            {
                EnvironmentName = Environments.Production
            });
            var customerService = environment.ServiceProvider.GetRequiredService<CustomerService>();

            var fakeIdentityName = Guid.NewGuid().ToString();
            var fakeIdentity = TestClaimsPrincipalFactory.CreateWithIdentityName(fakeIdentityName);

            //Act
            var createdUser = await environment.Mediator.Send(new CreateUserForIdentityCommand(fakeIdentity));
            var createdStripeCustomer = await customerService.GetAsync(createdUser.StripeCustomerId);

            //Assert
            try
            {
                Assert.IsNotNull(createdStripeCustomer);
                Assert.AreEqual(fakeIdentityName + "@example.com", createdStripeCustomer.Email);
                Assert.AreEqual(createdUser.Id.ToString(), createdStripeCustomer.Metadata["UserId"]);
            }
            finally
            {
                await customerService.DeleteAsync(createdStripeCustomer.Id);
            }
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_IdentityNameGiven_NewUserIsAddedToDatabase()
        {
            //Arrange
            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync();

            var fakeIdentityName = Guid.NewGuid().ToString();
            var fakeIdentity = TestClaimsPrincipalFactory.CreateWithIdentityName(fakeIdentityName);

            //Act
            await environment.Mediator.Send(new CreateUserForIdentityCommand(fakeIdentity));

            //Assert
            var createdUser = await GetUserByIdentityNameAsync(environment, fakeIdentityName);

            Assert.IsNotNull(createdUser);

            Assert.AreEqual(1, createdUser.Identities.Count);
            Assert.AreEqual(fakeIdentityName, createdUser.Identities.Single().Name);
        }

        private static async Task<User> GetUserByIdentityNameAsync(DoggerIntegrationTestEnvironment environment, string fakeIdentityName)
        {
            return await environment.WithFreshDataContext(async dataContext =>
            {
                return await dataContext
                    .Users
                    .Include(x => x.Identities)
                    .SingleOrDefaultAsync(x => x
                        .Identities
                        .Any(i => i.Name == fakeIdentityName));
            });
        }

        /// <summary>
        /// Creates a user (which in turn creates a Stripe customer), and then deletes the user from the database again, leading to the Stripe customer being orphaned.
        /// </summary>
        private static async Task<Customer> CreateOrphanedStripeCustomerAsync(
            DoggerIntegrationTestEnvironment environment,
            ClaimsPrincipal fakeIdentity)
        {
            var existingUser = await environment.Mediator.Send(new CreateUserForIdentityCommand(fakeIdentity));

            await environment.WithFreshDataContext(async dataContext =>
            {
                var userToRemove = await dataContext.Users.SingleAsync(x => x.Id == existingUser.Id);
                dataContext.Users.Remove(userToRemove);
            });

            return await environment
                .ServiceProvider
                .GetRequiredService<CustomerService>()
                .GetAsync(existingUser.StripeCustomerId);
        }
    }
}

using System.Linq;
using System.Threading.Tasks;
using Dogger.Domain.Commands.Payment.AdjustUserBalance;
using Dogger.Tests.Domain.Models;
using Dogger.Tests.TestHelpers;
using Dogger.Tests.TestHelpers.Environments.Dogger;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Stripe;

namespace Dogger.Tests.Domain.Commands.Payment.AdjustUserBalance
{
    [TestClass]
    public class AdjustUserBalanceCommandTest
    {
        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_NoExistingTransactionAndNoExistingBalance_CreatesBalanceTransaction()
        {
            //Arrange
            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync();

            var stripeCustomerService = environment.ServiceProvider.GetRequiredService<CustomerService>();
            var customer = await stripeCustomerService.CreateAsync(new CustomerCreateOptions());

            var user = new TestUserBuilder()
                .WithStripeCustomerId(customer.Id)
                .Build();

            //Act
            await environment.Mediator.Send(
                new AdjustUserBalanceCommand(
                    user,
                    23_45,
                    "some-idempotency-id"));

            //Assert
            var stripeBalanceService = environment.ServiceProvider.GetRequiredService<CustomerBalanceTransactionService>();

            var balanceAdjustment = await stripeBalanceService
                .ListAutoPagingAsync(user.StripeCustomerId)
                .SingleOrDefaultAsync();
            Assert.IsNotNull(balanceAdjustment);

            Assert.AreEqual(-23_45, balanceAdjustment.Amount);
            Assert.AreEqual("Idempotency ID: SOME-IDEMPOTENCY-ID", balanceAdjustment.Description);
            Assert.AreEqual("SOME-IDEMPOTENCY-ID", balanceAdjustment.Metadata[AdjustUserBalanceCommandHandler.IdempotencyKeyName]);
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_NoExistingTransactionAndNoExistingBalance_AddsBalanceToUser()
        {
            //Arrange
            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync();

            var stripeCustomerService = environment.ServiceProvider.GetRequiredService<CustomerService>();
            var customer = await stripeCustomerService.CreateAsync(new CustomerCreateOptions());

            var user = new TestUserBuilder()
                .WithStripeCustomerId(customer.Id)
                .Build();

            //Act
            await environment.Mediator.Send(
                new AdjustUserBalanceCommand(
                    user,
                    23_45,
                    "some-idempotency-id"));

            //Assert
            var refreshedCustomer = await stripeCustomerService.GetAsync(user.StripeCustomerId);
            Assert.AreEqual(-23_45, refreshedCustomer.Balance);
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_ExistingTransactionAlreadyCreatedWithSameIdempotencyId_DoesNothing()
        {
            //Arrange
            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync();

            var stripeCustomerService = environment.ServiceProvider.GetRequiredService<CustomerService>();
            var customer = await stripeCustomerService.CreateAsync(new CustomerCreateOptions());

            var user = new TestUserBuilder()
                .WithStripeCustomerId(customer.Id)
                .Build();

            await environment.Mediator.Send(
                new AdjustUserBalanceCommand(
                    user,
                    12_34,
                    "some-idempotency-id"));

            //Act
            await environment.Mediator.Send(
                new AdjustUserBalanceCommand(
                    user,
                    45_67,
                    "some-idempotency-id"));

            //Assert
            var stripeBalanceService = environment.ServiceProvider.GetRequiredService<CustomerBalanceTransactionService>();

            var balanceAdjustmentCount = await stripeBalanceService
                .ListAutoPagingAsync(user.StripeCustomerId)
                .CountAsync();
            Assert.AreEqual(1, balanceAdjustmentCount);
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_TwoBalanceAdjustmentsWithDifferentIdempotencyIds_BothAdjustmentsAreAdded()
        {
            //Arrange
            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync();

            var stripeCustomerService = environment.ServiceProvider.GetRequiredService<CustomerService>();
            var customer = await stripeCustomerService.CreateAsync(new CustomerCreateOptions());

            var user = new TestUserBuilder()
                .WithStripeCustomerId(customer.Id)
                .Build();

            //Act
            await environment.Mediator.Send(
                new AdjustUserBalanceCommand(
                    user,
                    1_00,
                    "some-idempotency-id-1"));

            await environment.Mediator.Send(
                new AdjustUserBalanceCommand(
                    user,
                    2_00,
                    "some-idempotency-id-2"));

            //Assert
            var refreshedCustomer = await stripeCustomerService.GetAsync(user.StripeCustomerId);
            Assert.AreEqual(-3_00, refreshedCustomer.Balance);
        }
    }
}


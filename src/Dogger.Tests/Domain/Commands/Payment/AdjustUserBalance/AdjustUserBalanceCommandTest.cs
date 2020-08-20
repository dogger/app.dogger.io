using System.Linq;
using System.Threading.Tasks;
using Dogger.Domain.Commands.Payment.AdjustUserBalance;
using Dogger.Domain.Models;
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

            var user = new User();

            await environment.DataContext.Users.AddAsync(user);
            await environment.DataContext.SaveChangesAsync();

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

            Assert.AreEqual(23_45, balanceAdjustment.Amount);
            Assert.AreEqual("Idempotency ID: some-idempotency-id", balanceAdjustment.Description);
            Assert.AreEqual("some-idempotency-id", balanceAdjustment.Metadata[AdjustUserBalanceCommandHandler.IdempotencyKeyName]);
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_NoExistingTransactionAndNoExistingBalance_AddsBalanceToUser()
        {
            //Arrange
            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync();

            var user = new User();

            await environment.DataContext.Users.AddAsync(user);
            await environment.DataContext.SaveChangesAsync();

            //Act
            await environment.Mediator.Send(
                new AdjustUserBalanceCommand(
                    user,
                    23_45,
                    "some-idempotency-id"));

            //Assert
            Assert.Fail();
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_ExistingTransactionFound_DoesNothing()
        {
            //Arrange
            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync();

            var user = new User();

            await environment.DataContext.Users.AddAsync(user);
            await environment.DataContext.SaveChangesAsync();

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
            Assert.IsNull(balanceAdjustment);

            Assert.Fail();
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_TwoBalanceAdjustmentsWithDifferentIdempotencyIds_BothAdjustmentsAreAdded()
        {
            //Arrange
            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync();

            var user = new User();

            await environment.DataContext.Users.AddAsync(user);
            await environment.DataContext.SaveChangesAsync();

            //Act
            await environment.Mediator.Send(
                new AdjustUserBalanceCommand(
                    user,
                    23_45,
                    "some-idempotency-id"));

            //Assert
            var stripeBalanceService = environment.ServiceProvider.GetRequiredService<CustomerBalanceTransactionService>();

            Assert.Fail();
        }
    }
}


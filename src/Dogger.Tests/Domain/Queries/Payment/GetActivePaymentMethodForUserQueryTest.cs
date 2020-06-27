using System;
using System.Threading.Tasks;
using Dogger.Domain.Commands.Payment.SetActivePaymentMethodForUser;
using Dogger.Domain.Commands.Users.CreateUserForIdentity;
using Dogger.Domain.Models;
using Dogger.Domain.Queries.Payment;
using Dogger.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Stripe;

namespace Dogger.Tests.Domain.Queries.Payment
{
    [TestClass]
    public class GetActivePaymentMethodForUserQueryTest
    {
        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_StripeCustomerIdNotFoundOnUser_ReturnsNull()
        {
            //Arrange
            await using var environment = await IntegrationTestEnvironment.CreateAsync();

            //Act
            var result = await environment.Mediator.Send(
                new GetActivePaymentMethodForUserQuery(
                    user: new User()
                    {
                        StripeCustomerId = null
                    }));

            //Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_NoPaymentMethodPresentOnStripeCustomer_NullReturned()
        {
            //Arrange
            await using var environment = await IntegrationTestEnvironment.CreateAsync();

            var createdUser = await environment.Mediator.Send(
                new CreateUserForIdentityCommand(
                    TestClaimsPrincipalFactory.CreateWithIdentityName($"some-identity-name-{Guid.NewGuid()}")));

            //Act
            var activePaymentMethod = await environment.Mediator.Send(
                new GetActivePaymentMethodForUserQuery(
                    createdUser));

            //Assert
            Assert.IsNull(activePaymentMethod);
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_SinglePaymentMethodPresentOnStripeCustomer_SinglePaymentMethodReturned()
        {
            //Arrange
            await using var environment = await IntegrationTestEnvironment.CreateAsync();

            var createdUser = await environment.Mediator.Send(
                new CreateUserForIdentityCommand(
                    TestClaimsPrincipalFactory.CreateWithIdentityName("some-identity-name")));

            var createdPaymentMethod = await environment
                .ServiceProvider
                .GetRequiredService<PaymentMethodService>()
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

            await environment.Mediator.Send(
                new SetActivePaymentMethodForUserCommand(
                    createdUser,
                    createdPaymentMethod.Id));

            //Act
            var activePaymentMethod = await environment.Mediator.Send(
                new GetActivePaymentMethodForUserQuery(
                    createdUser));

            //Assert
            Assert.IsNotNull(activePaymentMethod);
        }
    }
}

using System;
using System.Threading.Tasks;
using Dogger.Domain.Commands.Payment.ApplyCouponCodeForUser;
using Dogger.Domain.Commands.Users.CreateUserForIdentity;
using Dogger.Tests.TestHelpers;
using Dogger.Tests.TestHelpers.Environments.Dogger;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Stripe;

namespace Dogger.Tests.Domain.Commands.Payment.ApplyCouponCodeForUser
{
    [TestClass]
    public class ApplyCouponCodeForUserCommandTest
    {
        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_ValidConditions_AppliesPromotionCodeDiscountToCustomer()
        {
            //Arrange
            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync();

            var user = await environment.Mediator.Send(
                new CreateUserForIdentityCommand(
                    TestClaimsPrincipalFactory.CreateWithIdentityName("some-identity-name")));
            
            var customerService = environment
                .ServiceProvider
                .GetRequiredService<CustomerService>();

            var couponService = environment
                .ServiceProvider
                .GetRequiredService<CouponService>();
            var coupon = await couponService.CreateAsync(
                new CouponCreateOptions()
                {
                    AmountOff = 1_00,
                    Duration = "forever",
                    Currency = "USD"
                });

            var promotionCodeService = environment
                .ServiceProvider
                .GetRequiredService<PromotionCodeService>();
            var promotionCode = await promotionCodeService.CreateAsync(
                new PromotionCodeCreateOptions()
                {
                    Coupon = coupon.Id,
                    Code = Guid.NewGuid()
                        .ToString()
                        .Replace("-", "")
                });

            //Act
            var result = await environment.Mediator.Send(
                new ApplyCouponCodeForUserCommand(user, promotionCode.Code));

            //Assert
            Assert.IsTrue(result);

            var customer = await customerService.GetAsync(user.StripeCustomerId);
            Assert.IsNotNull(customer?.Discount?.Coupon);
            Assert.AreEqual(1_00, customer.Discount.Coupon.AmountOff);
        }
    }
}


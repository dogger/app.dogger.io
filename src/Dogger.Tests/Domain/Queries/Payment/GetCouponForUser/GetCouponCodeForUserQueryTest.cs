using System;
using System.Threading.Tasks;
using Dogger.Domain.Commands.Payment.ApplyCouponCodeForUser;
using Dogger.Domain.Commands.Users.CreateUserForIdentity;
using Dogger.Domain.Queries.Payment.GetCouponForUser;
using Dogger.Tests.TestHelpers;
using Dogger.Tests.TestHelpers.Environments.Dogger;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Stripe;

namespace Dogger.Tests.Domain.Queries.Payment.GetCouponForUser
{
    [TestClass]
    public class GetCouponCodeForUserQueryTest
    {
        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_CouponCodeExists_ReturnsCouponCode()
        {
            //Arrange
            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync();

            var user = await environment.Mediator.Send(
                new CreateUserForIdentityCommand(
                    TestClaimsPrincipalFactory.CreateWithIdentityName("some-identity-name")));

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
                        .Replace("-", ""),
                    Customer = user.StripeCustomerId
                });

            await environment.Mediator.Send(new ApplyCouponCodeForUserCommand(user, promotionCode.Code));

            //Act
            var result = await environment.Mediator.Send(
                new GetCouponForUserQuery(user));

            //Assert
            Assert.IsNotNull(result.Code);
            Assert.AreEqual(promotionCode.Code, result.Code);
        }
    }
}


using System;
using System.Threading.Tasks;
using Dogger.Domain.Queries.Payment.GetCouponById;
using Dogger.Tests.TestHelpers;
using Dogger.Tests.TestHelpers.Environments.Dogger;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Stripe;

namespace Dogger.Tests.Domain.Queries.Payment.GetCouponById
{
    [TestClass]
    public class GetCouponByIdQueryTest
    {
        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_SomeIntegrationCondition_SomeOutcome()
        {
            //Arrange
            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync();

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
                new GetCouponByIdQuery(promotionCode.Id));

            //Assert
            Assert.IsNotNull(result);

            Assert.AreEqual(coupon.Id, result.Coupon.Id);
            Assert.AreEqual(promotionCode.Id, result.Id);
        }
    }
}


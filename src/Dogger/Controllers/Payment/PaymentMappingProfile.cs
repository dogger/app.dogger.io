using AutoMapper;
using Stripe;

namespace Dogger.Controllers.Payment
{
    public class PaymentMappingProfile : Profile
    {
        public PaymentMappingProfile()
        {
            CreateMap<PaymentMethod, PaymentMethodResponse>()
                .ForMember(x => x.Brand, x => x.MapFrom(y => y.Card.Brand))
                .ForMember(x => x.Id, x => x.MapFrom(y => y.Id));

            CreateMap<PromotionCode, CouponCodeResponse>()
                .ForMember(x => x.Code, x => x.MapFrom(y => y.Code))
                .ForMember(x => x.AmountOffInHundreds, x => x.MapFrom(y => y.Coupon.AmountOff))
                .ForMember(x => x.AmountOffInPercentage, x => x.MapFrom(y => y.Coupon.PercentOff));
        }
    }
}

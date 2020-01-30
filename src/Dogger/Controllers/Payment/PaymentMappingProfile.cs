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
        }
    }
}

namespace Dogger.Controllers.Payment
{
    public class CouponCodeResponse
    {
        public string? Code { get; set; }
        public string? Name { get; set; }
        public long? AmountOffInHundreds { get; set; }
        public decimal? AmountOffInPercentage { get; set; }
    }
}

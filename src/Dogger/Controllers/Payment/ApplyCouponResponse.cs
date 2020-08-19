namespace Dogger.Controllers.Payment
{
    public class ApplyCouponResponse
    {
        public bool WasApplied { get; }

        public ApplyCouponResponse(bool wasApplied)
        {
            this.WasApplied = wasApplied;
        }
    }
}

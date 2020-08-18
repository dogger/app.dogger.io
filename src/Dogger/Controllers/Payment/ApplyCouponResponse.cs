using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

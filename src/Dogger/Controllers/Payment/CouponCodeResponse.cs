using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dogger.Controllers.Payment
{
    public class CouponCodeResponse
    {
        public string? Code { get; set; }
        public long? AmountOffInHundreds { get; set; }
        public decimal? AmountOffInPercentage { get; set; }
    }
}

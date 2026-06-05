using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace steptreck.Domain.Constants
{
    public static class PaymentConstants
    {
        public const string ProviderInternal = "internal";
        public const string StatusPaid = "paid";
        public const string StatusPending = "pending";
        public const string ReasonSubscribe = "subscription_start";
        public const string ReasonExtend = "subscription_extend";
    }
}

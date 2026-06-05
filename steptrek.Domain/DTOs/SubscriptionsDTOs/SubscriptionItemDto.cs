using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace steptreck.Domain.DTOs.SubscriptionsDTOs
{
    public class SubscriptionItemDto
    {
        public string Name { get; set; } = null!;

        public string Unit { get; set; } = null!;

        public decimal PricePerUnit { get; set; }

        public decimal MinQuantity { get; set; }

        public decimal MaxQuantity { get; set; }

        public decimal Step { get; set; }

        public string? Description { get; set; }
    }
}

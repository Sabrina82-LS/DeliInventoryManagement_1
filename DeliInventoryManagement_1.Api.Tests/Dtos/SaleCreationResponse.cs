using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeliInventoryManagement_1.Api.Tests.Dtos
{
    public class SaleCreationResponse
    {
        public string SaleId { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public string MovementId { get; set; } = string.Empty;
        public string OutboxId { get; set; } = string.Empty;
    }
}
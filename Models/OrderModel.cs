using System;
using System.Collections.Generic;

namespace Pgd.Magento.Models
{
    public class OrderModel
    {
        public int Id { get; set; }

        public string OrderNumber { get; set; }

        public string State { get; set; }

        public string Status { get; set; }

        public string Shipping { get; set; }

        public AddressModel Customer { get; set; }

        public AddressModel Recipient { get; set; }

        public string PaymentMethod { get; set; }

        public decimal OrderTotal { get; set; }

        public decimal ShippingTotal { get; set; }

        public List<LineItemModel> LineItems { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}

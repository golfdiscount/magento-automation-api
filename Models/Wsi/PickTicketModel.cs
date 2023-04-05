﻿using System;
using System.Collections.Generic;

namespace Pgd.Magento.Models.Wsi
{
    public class PickTicketModel
    {
        public string PickTicketNumber { get; set; }

        public string OrderNumber { get; set; }

        public char Action { get; set; }

        public int Store { get; set; }

        public AddressModel Customer { get; set; }

        public AddressModel Recipient { get; set; }

        public string ShippingMethod { get; set; }

        public List<PickTicketDetailModel> LineItems { get; set; }

        public DateTime OrderDate { get; set; }

        public int Channel { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}

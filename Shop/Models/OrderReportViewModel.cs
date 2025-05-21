using System;
using System.Collections.Generic;
using Shop.Models;

namespace Shop.Models
{
    public class OrderReportViewModel
    {
        public List<Order> Orders { get; set; }
        public int TotalQuantity { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string SelectedPeriod { get; set; }
    }
}
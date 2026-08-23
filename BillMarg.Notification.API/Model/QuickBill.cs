using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BillMarg.Notification.API.Models
{
    public class QuickBill
    {
        [Key]
        public int QuickBillId { get; set; }

        public int UserId { get; set; }

        public int CustomerId { get; set; }

        public string BillNumber { get; set; } = string.Empty;

        public string? CustomerName { get; set; }

        public string? CustomerMobile { get; set; }

        public decimal SubTotal { get; set; }

        public decimal CGSTAmount { get; set; }

        public decimal SGSTAmount { get; set; }

        public decimal IGSTAmount { get; set; }

        public decimal GSTAmount { get; set; }

        public decimal DiscountAmount { get; set; }

        public decimal GrandTotal { get; set; }

        public DateTime BillDate { get; set; } = DateTime.Now;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? UpdatedDate { get; set; }

        public bool IsDeleted { get; set; } = false;

        public ICollection<QuickBillItem> Items { get; set; }
            = new List<QuickBillItem>();

        
    }
}
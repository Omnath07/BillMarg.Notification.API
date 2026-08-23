using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BillMarg.Notification.API.Models
{
    public class InvoiceItems
    {
        [Key]
        public int ItemId { get; set; }

        public int InvoiceId { get; set; }
        [ForeignKey("InvoiceId")]
       

        [Required]
        public string Description { get; set; }

        [Required]
        public int Quantity { get; set; }

        public virtual Invoices Invoice { get; set; } // Ensure Navigation Property
        [Required]
        public decimal UnitPrice { get; set; }

        public decimal Total { get; set; }
        public string? ItemCode { get; set; }
        public int? ProductId { get; set; }

        [ForeignKey("ProductId")]
        public Product Product { get; set; }
        public DateTime CreatedOn { get; set; } = DateTime.Now;

    }

    public class TopSellingProductViewModel
    {
        public int ProductId { get; set; }

        public string ProductName { get; set; }

        public int TotalQtySold { get; set; }

        public decimal TotalSales { get; set; }

        public int InvoiceCount { get; set; }
    }
}

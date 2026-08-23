using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BillMarg.Notification.API.Models
{
    public class Purchase
    {
        [Key]
        public int PurchaseId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [MaxLength(50)]
        public string PurchaseNumber { get; set; }

        [MaxLength(255)]
        public string? SupplierName { get; set; }

        [MaxLength(100)]
        public string? SupplierInvoiceNumber { get; set; }

        public DateTime PurchaseDate { get; set; } = DateTime.Now;

        public decimal SubTotal { get; set; }

        public decimal Discount { get; set; }

        public decimal TaxAmount { get; set; }

        public decimal GrandTotal { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        [Required]
        [MaxLength(30)]
        public string PurchaseStatus { get; set; } = "Draft";

        public bool StockAdded { get; set; } = false;

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        public DateTime? UpdatedOn { get; set; }

    }
}
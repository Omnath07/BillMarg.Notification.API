using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BillMarg.Notification.API.Models
{
    public class Quotations
    {
        [Key]
        public int QuotationId { get; set; }

        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public Users Users { get; set; }

        public int? ClientId { get; set; }
        [ForeignKey("ClientId")]
        public Clients Client { get; set; }

        [Required, MaxLength(50)]
        public string QuotationNumber { get; set; }

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public decimal TaxAmount { get; set; }
        [Required]
        public decimal Tax { get; set; }
        [Required]
        public decimal Discount { get; set; }
        [Required]
        public string Status { get; set; } = "Pending";

        public DateTime QuotationDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string Notes { get; set; }
        public string? BillFile { get; set; }
        public DateTime CreatedOn { get; set; } = DateTime.Now;
        public ICollection<QuotationItems> QuotationItems { get; set; }

       // public ICollection<InvoiceItems> InvoiceItems { get; set; }
    }

    public class QuotationViewModel
    {
        public int QuotationId { get; set; }

        [Required, MaxLength(50)]
        public string QuotationNumber { get; set; }

        [Required]
        public int ClientId { get; set; }

        [Required]
        public DateTime QuotationDate { get; set; }

        [Required]
        public DateTime ExpiryDate { get; set; }

        public string Notes { get; set; }
        public decimal Amount { get; set; }
        public decimal TaxAmount { get; set; }

        public decimal Tax { get; set; }

        public decimal Discount { get; set; }

        public string Status { get; set; }
        public bool CreateChallan { get; set; }

        public string? DriverName { get; set; }

        public string? VehicleNumber { get; set; }

        public string? Remarks { get; set; }

        public List<QuotationItems> QuotationItems { get; set; } = new List<QuotationItems>();

        public List<InvoiceItems> InvoiceItems { get; set; } = new List<InvoiceItems>();
    }
}

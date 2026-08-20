using System.ComponentModel.DataAnnotations;

namespace BillSathi.Models
{
    public class InvoicePayments
    {
        [Key]
        public int PaymentId { get; set; }

        public int InvoiceId { get; set; }

        public int UserId { get; set; }

        public decimal Amount { get; set; }

        public string? PaymentType { get; set; }

        public string? ReferenceNo { get; set; }

        public string? PdfFile { get; set; }

        public DateTime PaymentDate { get; set; }

        public DateTime CreatedOn { get; set; }
    }
}

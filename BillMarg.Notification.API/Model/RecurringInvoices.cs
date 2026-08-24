using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BillMarg.Notification.API.Models
{
    public class RecurringInvoices
    {
        [Key]
        public int RecurringInvoiceId { get; set; }

        public int InvoiceId { get; set; }

        [ForeignKey("InvoiceId")]
        public Invoices Invoice { get; set; }

        [Required]
        public string RecurrenceType { get; set; }

        public DateTime NextInvoiceDate { get; set; }

        public DateTime? EndDate { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedOn { get; set; } = DateTime.Now;
    }
}
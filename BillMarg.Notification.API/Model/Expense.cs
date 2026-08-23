using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BillMarg.Notification.API.Models
{
    [Table("Expense")]
    public class Expense
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey(nameof(UserId))]
        public int UserId { get; set; }

        public int ExpenseCategoryId { get; set; }

        public DateTime ExpenseDate { get; set; }

        public decimal Amount { get; set; }

        public string Description { get; set; }

        public string ? BillFile { get; set; }

        public string PaymentMode { get; set; }

        public int? CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; }

        [ForeignKey(nameof(ExpenseCategoryId))]
        public virtual ExpenseCategory ExpenseCategory { get; set; }
        public string? OriginalFileName { get; set; }

        public long? FileSize { get; set; }

        public string? ContentType { get; set; }

    }
}
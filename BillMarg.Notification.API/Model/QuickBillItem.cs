using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BillMarg.Notification.API.Models
{
    public class QuickBillItem
    {
        [Key]
        public int QuickBillItemId { get; set; }

        public int QuickBillId { get; set; }

        public int ProductId { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public decimal Quantity { get; set; }

        public decimal SellingPrice { get; set; }

        public decimal GSTPercentage { get; set; }

        public decimal GSTAmount { get; set; }

        public decimal TotalAmount { get; set; }

        [ForeignKey(nameof(QuickBillId))]
        public QuickBill QuickBill { get; set; } = null!;
    }
}
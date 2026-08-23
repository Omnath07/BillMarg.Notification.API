using System;
using System.ComponentModel.DataAnnotations;

namespace BillMarg.Notification.API.Models
{
    public class UserNotificationSettings
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }

        // Reports
        public bool DailyBusinessReport { get; set; }

        public bool WeeklyBusinessReport { get; set; }

        public bool MonthlyBusinessReport { get; set; }

        // Alerts
        public bool LowStockAlert { get; set; }

        public bool PendingPaymentAlert { get; set; }

        public bool OverduePaymentAlert { get; set; }

        public bool RecurringInvoiceReminder { get; set; }

        public bool PurchaseStockAlert { get; set; }

        // Master switch
        public bool EmailEnabled { get; set; } = true;

        public DateTime CreatedDate { get; set; }

        public DateTime? UpdatedDate { get; set; }
    }
}
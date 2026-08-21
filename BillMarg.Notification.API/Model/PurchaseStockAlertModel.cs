using System;
using System.Collections.Generic;

namespace BillMarg.Notification.API.Models
{
    public class PurchaseStockAlertModel
    {
        public int UserId { get; set; }

        public string UserName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public DateTime ReportDate { get; set; }

        public int TotalPurchases { get; set; }

        public decimal TotalPurchaseAmount { get; set; }

        public List<PurchaseStockAlertItem> Purchases { get; set; }
            = new List<PurchaseStockAlertItem>();
    }

    public class PurchaseStockAlertItem
    {
        public string PurchaseNumber { get; set; } = string.Empty;

        public string SupplierName { get; set; } = string.Empty;

        public string SupplierInvoiceNumber { get; set; } = string.Empty;

        public DateTime PurchaseDate { get; set; }

        public decimal GrandTotal { get; set; }

        public string PurchaseStatus { get; set; } = string.Empty;

        public bool StockAdded { get; set; }
    }
}
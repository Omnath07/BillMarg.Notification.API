namespace BillMarg.Notification.API.Interface
{
    public interface IDailyStockReportService
    {
        // ==========================================
        // LOW STOCK ALERT
        // ==========================================

        Task SendDailyLowStockAlertsAsync();

        Task<bool> SendLowStockAlertToUserAsync(
            int userId);


        // ==========================================
        // DAILY STOCK INVENTORY REPORT
        // ==========================================

        Task SendDailyStockInventoryReportsAsync();

        Task<bool> SendDailyStockInventoryReportToUserAsync(
            int userId);
    }
}
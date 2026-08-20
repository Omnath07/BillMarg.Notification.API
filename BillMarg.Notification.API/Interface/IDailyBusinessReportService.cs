namespace BillMarg.Notification.API.Interface
{
    public interface IDailyBusinessReportService
    {
        Task SendDailyBusinessReportsAsync();

        Task<bool> SendDailyBusinessReportToUserAsync(int userId);
    }
}
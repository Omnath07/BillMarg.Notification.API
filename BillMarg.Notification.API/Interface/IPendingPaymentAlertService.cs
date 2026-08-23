namespace BillMarg.Notification.API.Interface
{
    public interface IPendingPaymentAlertService
    {
        Task SendPendingPaymentAlertsAsync(
            CancellationToken cancellationToken = default);

        Task<bool> SendPendingPaymentAlertToUserAsync(
            int userId,
            CancellationToken cancellationToken = default);
    }
}
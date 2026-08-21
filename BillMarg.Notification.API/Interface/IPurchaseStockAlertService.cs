using System.Threading;
using System.Threading.Tasks;

namespace BillMarg.Notification.API.Services
{
    public interface IPurchaseStockAlertService
    {
        Task SendPurchaseStockAlertsAsync(
            CancellationToken cancellationToken = default);
    }
}
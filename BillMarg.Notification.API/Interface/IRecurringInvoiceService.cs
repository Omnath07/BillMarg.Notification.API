using System.Threading;
using System.Threading.Tasks;

namespace BillMarg.Notification.API.Interface
{
    public interface IRecurringInvoiceService
    {
        Task ProcessRecurringInvoicesAsync(
            CancellationToken cancellationToken = default);

        Task<bool> ProcessScheduledInvoiceAsync(
            int invoiceId,
            CancellationToken cancellationToken = default);
    }
}
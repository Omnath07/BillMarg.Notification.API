using System.Threading.Tasks;

namespace BillMarg.Notification.API.Services
{
    public interface IOverdueInvoiceEmailService
    {
        Task<OverdueInvoiceEmailResult> SendOverdueInvoiceEmailsAsync();
    }

    public class OverdueInvoiceEmailResult
    {
        public int ClientsProcessed { get; set; }
        public int EmailsSent { get; set; }
        public int InvoicesIncluded { get; set; }
        public int FailedEmails { get; set; }
    }
}
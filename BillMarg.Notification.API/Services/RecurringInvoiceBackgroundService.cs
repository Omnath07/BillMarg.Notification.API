using BillMarg.Notification.API.Interface;

namespace BillMarg.Notification.API.Services
{
    public class RecurringInvoiceBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<RecurringInvoiceBackgroundService> _logger;

        public RecurringInvoiceBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<RecurringInvoiceBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "Recurring Invoice Background Service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope =
                        _scopeFactory.CreateScope();

                    var recurringInvoiceService =
                        scope.ServiceProvider
                            .GetRequiredService<IRecurringInvoiceService>();

                    await recurringInvoiceService
                        .ProcessRecurringInvoicesAsync(
                            stoppingToken);

                    _logger.LogInformation(
                        "Recurring invoices processed successfully. Time: {Time}",
                        DateTime.Now);
                }
                catch (OperationCanceledException)
                {
                    // Application is shutting down
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error occurred while processing recurring invoices.");
                }

                // Check every 1 hour
                await Task.Delay(
                    TimeSpan.FromHours(1),
                    stoppingToken);
            }

            _logger.LogInformation(
                "Recurring Invoice Background Service stopped.");
        }
    }
}
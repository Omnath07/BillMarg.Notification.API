using BillMarg.Notification.API.Interface;
using Microsoft.Extensions.DependencyInjection;

namespace BillMarg.Notification.API.Services
{
    public class DailyReportBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DailyReportBackgroundService> _logger;

        public DailyReportBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<DailyReportBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "====================================================");

            _logger.LogInformation(
                "BillMarg Daily Report Background Service STARTED.");

            _logger.LogInformation(
                "====================================================");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // =================================================
                    // INDIA TIME ZONE
                    // =================================================

                    TimeZoneInfo indiaTimeZone;

                    try
                    {
                        indiaTimeZone =
                            TimeZoneInfo.FindSystemTimeZoneById(
                                "Asia/Kolkata");
                    }
                    catch
                    {
                        indiaTimeZone =
                            TimeZoneInfo.FindSystemTimeZoneById(
                                "India Standard Time");
                    }


                    // =================================================
                    // CURRENT INDIA TIME
                    // =================================================

                    DateTime indiaNow =
                        TimeZoneInfo.ConvertTimeFromUtc(
                            DateTime.UtcNow,
                            indiaTimeZone);

                    _logger.LogInformation(
                        "Current India Time: {IndiaNow:dd-MM-yyyy HH:mm:ss}",
                        indiaNow);


                    // =================================================
                    // NEXT RUN TIME
                    // =================================================
                    // CHANGE THESE TWO VALUES WHEN REQUIRED
                    //
                    // 22 = 10 PM
                    // 05 = 5 minutes
                    //
                    // Current schedule = 10:05 PM IST
                    // =================================================

                    // Define window
                    int startHour = 16;   // 10 AM
                    int endHour = 23;   // 11 PM

                    // Current India time

                    // If current time < startHour → run at startHour
                    // If current time > endHour → schedule next day at startHour
                    DateTime nextRun;

                    if (indiaNow.Hour < startHour)
                    {
                        nextRun = indiaNow.Date.AddHours(startHour);
                    }
                    else if (indiaNow.Hour >= endHour)
                    {
                        nextRun = indiaNow.Date.AddDays(1).AddHours(startHour);
                    }
                    else
                    {
                        // 👉 Already inside window → allow manual trigger
                        nextRun = indiaNow;
                    }

                    // =================================================
                    // IF TODAY'S TIME PASSED
                    // RUN TOMORROW
                    // =================================================

                    if (indiaNow >= nextRun)
                    {
                        nextRun = nextRun.AddDays(1);
                    }


                    // =================================================
                    // CALCULATE DELAY
                    // =================================================

                    TimeSpan delay =
                        nextRun - indiaNow;

                    _logger.LogInformation(
                        "Next BillMarg Daily Reports scheduled at: {NextRun:dd-MM-yyyy HH:mm:ss} IST",
                        nextRun);

                    _logger.LogInformation(
                        "Waiting for: {Delay}",
                        delay);


                    // =================================================
                    // WAIT
                    // =================================================

                    await Task.Delay(
                        delay,
                        stoppingToken);


                    if (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }


                    // =================================================
                    // REPORT 1
                    // DAILY BUSINESS REPORT
                    // =================================================

                    await RunBusinessReportsAsync(
                        stoppingToken);


                    // =================================================
                    // REPORT 2
                    // LOW STOCK ALERT
                    // =================================================

                    await RunLowStockAlertsAsync(
                        stoppingToken);


                    // =================================================
                    // REPORT 3
                    // DAILY STOCK INVENTORY
                    // =================================================

                    await RunDailyStockInventoryAsync(
                        stoppingToken);



                    await RunPurchaseStockAlertsAsync(
                    stoppingToken);

                    await RunPendingPaymentAlertsAsync(
                stoppingToken);

                    // =================================================
                    // COMPLETED
                    // =================================================

                    _logger.LogInformation(
                        "====================================================");

                    _logger.LogInformation(
                        "ALL BILLMARG DAILY REPORTS COMPLETED.");

                    _logger.LogInformation(
                        "Completed at India Time: {IndiaNow:dd-MM-yyyy HH:mm:ss}",
                        GetIndiaTime());

                    _logger.LogInformation(
                        "====================================================");
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Unexpected error in DailyReportBackgroundService.");

                    try
                    {
                        await Task.Delay(
                            TimeSpan.FromMinutes(5),
                            stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }

            _logger.LogInformation(
                "BillMarg Daily Report Background Service STOPPED.");
        }


        // =============================================================
        // 1. BUSINESS REPORT
        // =============================================================

        private async Task RunBusinessReportsAsync(
            CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation(
                    "----------------------------------------------------");

                _logger.LogInformation(
                    "STARTING DAILY BUSINESS REPORTS.");

                _logger.LogInformation(
                    "----------------------------------------------------");


                using IServiceScope scope =
                    _scopeFactory.CreateScope();


                var service =
                    scope.ServiceProvider
                        .GetRequiredService<
                            IDailyBusinessReportService>();


                await service.SendDailyBusinessReportsAsync();


                _logger.LogInformation(
                    "DAILY BUSINESS REPORTS COMPLETED.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "DAILY BUSINESS REPORTS FAILED.");

                // IMPORTANT:
                // Do NOT throw.
                // Allow next report to execute.
            }
        }


        // =============================================================
        // 2. LOW STOCK ALERT
        // =============================================================

        private async Task RunLowStockAlertsAsync(
            CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation(
                    "----------------------------------------------------");

                _logger.LogInformation(
                    "STARTING LOW STOCK ALERTS.");

                _logger.LogInformation(
                    "----------------------------------------------------");


                using IServiceScope scope =
                    _scopeFactory.CreateScope();


                var service =
                    scope.ServiceProvider
                        .GetRequiredService<
                            IDailyStockReportService>();


                await service.SendDailyLowStockAlertsAsync();


                _logger.LogInformation(
                    "LOW STOCK ALERTS COMPLETED.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "LOW STOCK ALERTS FAILED.");

                // Do NOT throw.
                // Inventory report will still run.
            }
        }


        // =============================================================
        // 3. DAILY STOCK INVENTORY
        // =============================================================

        private async Task RunDailyStockInventoryAsync(
            CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation(
                    "----------------------------------------------------");

                _logger.LogInformation(
                    "STARTING DAILY STOCK INVENTORY REPORTS.");

                _logger.LogInformation(
                    "----------------------------------------------------");


                using IServiceScope scope =
                    _scopeFactory.CreateScope();


                var service =
                    scope.ServiceProvider
                        .GetRequiredService<
                            IDailyStockReportService>();


                await service.SendDailyStockInventoryReportsAsync();


                _logger.LogInformation(
                    "DAILY STOCK INVENTORY REPORTS COMPLETED.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "DAILY STOCK INVENTORY REPORTS FAILED.");

                // Do NOT throw.
            }
        }

        // =============================================================
        // 4. PURCHASE STOCK ALERT
        // =============================================================

        public async Task RunPurchaseStockAlertsAsync(
            CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation(
                    "----------------------------------------------------");

                _logger.LogInformation(
                    "STARTING PURCHASE STOCK ALERTS.");

                _logger.LogInformation(
                    "----------------------------------------------------");

                using IServiceScope scope =
                    _scopeFactory.CreateScope();

                var service =
                    scope.ServiceProvider
                        .GetRequiredService<
                            IPurchaseStockAlertService>();

                await service.SendPurchaseStockAlertsAsync(
                    stoppingToken);

                _logger.LogInformation(
                    "PURCHASE STOCK ALERTS COMPLETED.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "PURCHASE STOCK ALERTS FAILED.");

                // Do NOT throw.
                // Other reports should continue.
            }
        }

        // =============================================================
        // 5. PENDING PAYMENT ALERT
        // =============================================================

        private async Task RunPendingPaymentAlertsAsync(
            CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation(
                    "----------------------------------------------------");

                _logger.LogInformation(
                    "STARTING PENDING PAYMENT ALERTS.");

                _logger.LogInformation(
                    "----------------------------------------------------");


                using IServiceScope scope =
                    _scopeFactory.CreateScope();


                var service =
                    scope.ServiceProvider
                        .GetRequiredService<
                            IPendingPaymentAlertService>();


                await service.SendPendingPaymentAlertsAsync(
                    stoppingToken);


                _logger.LogInformation(
                    "PENDING PAYMENT ALERTS COMPLETED.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "PENDING PAYMENT ALERTS FAILED.");

                // Do NOT throw.
                // Other reports should continue.
            }
        }

        // =============================================================
        // INDIA TIME
        // =============================================================

        private DateTime GetIndiaTime()
        {
            TimeZoneInfo indiaTimeZone;

            try
            {
                indiaTimeZone =
                    TimeZoneInfo.FindSystemTimeZoneById(
                        "Asia/Kolkata");
            }
            catch
            {
                indiaTimeZone =
                    TimeZoneInfo.FindSystemTimeZoneById(
                        "India Standard Time");
            }

            return TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                indiaTimeZone);
        }
    }
}
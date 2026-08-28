using BillMarg.Notification.API.Interface;
using BillMarg.Notification.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace BillMarg.Notification.API.Controllers
{
    [ApiController]
    [Route("api/daily-report")]
    public class DailyReportController : ControllerBase
    {
        private readonly IDailyStockReportService _stockReportService;
        private readonly IPurchaseStockAlertService _purchaseAlertService;
        private readonly IPendingPaymentAlertService _paymentAlertService;
        private readonly IDailyBusinessReportService _reportService;

        public DailyReportController(
            IDailyBusinessReportService reportService, IDailyStockReportService stockReportService,
            IPurchaseStockAlertService purchaseAlertService,
            IPendingPaymentAlertService paymentAlertService)
        {
            _reportService = reportService;
            _stockReportService = stockReportService;
            _purchaseAlertService = purchaseAlertService;
            _paymentAlertService = paymentAlertService;
        }

      

        // =========================================================
        // MANUAL TEST
        // =========================================================

        [HttpPost("send")]
        public async Task<IActionResult> SendReports()
        {
            await _reportService
                .SendDailyBusinessReportsAsync();

            return Ok(new
            {
                success = true,
                message =
                    "Daily business reports processed successfully."
            });
        }


        // =========================================================
        // TEST ONE USER
        // =========================================================

        [HttpPost("send/{userId:int}")]
        public async Task<IActionResult> SendToUser(
            int userId)
        {
            bool result =
                await _reportService
                    .SendDailyBusinessReportToUserAsync(userId);


            if (!result)
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "Daily report could not be sent."
                });
            }


            return Ok(new
            {
                success = true,
                message =
                    $"Daily report sent successfully to UserId {userId}."
            });
        }



        [HttpPost("lowstock")]
        public async Task<IActionResult> SendLowStockAlerts()
        {
            await _stockReportService.SendDailyLowStockAlertsAsync();
            return Ok(new { success = true, message = "Low stock alerts sent successfully." });
        }

        // =========================================================
        // 3. DAILY STOCK INVENTORY
        // =========================================================
        [HttpPost("inventory")]
        public async Task<IActionResult> SendStockInventoryReports()
        {
            await _stockReportService.SendDailyStockInventoryReportsAsync();
            return Ok(new { success = true, message = "Daily stock inventory reports sent successfully." });
        }

        // =========================================================
        // 4. PURCHASE STOCK ALERTS
        // =========================================================
        [HttpPost("purchasealerts")]
        public async Task<IActionResult> SendPurchaseStockAlerts(CancellationToken cancellationToken)
        {
            await _purchaseAlertService.SendPurchaseStockAlertsAsync(cancellationToken);
            return Ok(new { success = true, message = "Purchase stock alerts sent successfully." });
        }

        // =========================================================
        // 5. PENDING PAYMENT ALERTS
        // =========================================================
        [HttpPost("pendingpayments")]
        public async Task<IActionResult> SendPendingPaymentAlerts(CancellationToken cancellationToken)
        {
            await _paymentAlertService.SendPendingPaymentAlertsAsync(cancellationToken);
            return Ok(new { success = true, message = "Pending payment alerts sent successfully." });
        }

    }
}
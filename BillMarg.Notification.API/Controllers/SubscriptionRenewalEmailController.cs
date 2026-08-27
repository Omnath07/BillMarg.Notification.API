using BillMarg.Notification.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace BillMarg.Notification.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SubscriptionRenewalEmailController
        : ControllerBase
    {
        private readonly ISubscriptionRenewalEmailService _service;

        public SubscriptionRenewalEmailController(
            ISubscriptionRenewalEmailService service)
        {
            _service = service;
        }

        // ============================================================
        // MANUAL DAILY TRIGGER
        // ============================================================

        [HttpPost("send")]
        public async Task<IActionResult>
            SendSubscriptionRenewalEmails()
        {
            try
            {
                var result =
                    await _service
                        .SendSubscriptionRenewalEmailsAsync();

                return Ok(new
                {
                    success = true,

                    message =
                        "Subscription renewal email process completed successfully.",

                    usersChecked =
                        result.UsersChecked,

                    emailsSent =
                        result.EmailsSent,

                    skippedUsers =
                        result.SkippedUsers,

                    failedEmails =
                        result.FailedEmails,

                    expiringInFiveDays =
                        result.ExpiringInFiveDays,

                    expiringInFourDays =
                        result.ExpiringInFourDays,

                    expiringInThreeDays =
                        result.ExpiringInThreeDays,

                    expiringInTwoDays =
                        result.ExpiringInTwoDays,

                    expiringTomorrow =
                        result.ExpiringTomorrow,

                    expiringToday =
                        result.ExpiringToday,

                    expiredOneToFifteenDays =
                        result.ExpiredOneToFifteenDays,

                    processedOn =
                        DateTime.Now
                });
            }
            catch (Exception ex)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        success = false,

                        message =
                            "Unable to process subscription renewal emails.",

                        error = ex.Message,

                        processedOn =
                            DateTime.Now
                    });
            }
        }
    }
}
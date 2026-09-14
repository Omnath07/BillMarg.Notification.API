using BillMarg.Notification.API.Interface;
using Microsoft.AspNetCore.Mvc;

namespace BillMarg.Notification.API.Controllers
{
    [ApiController]
    [Route("api/daily-report")]
    public class DailyReportController : ControllerBase
    {
        private readonly IDailyBusinessReportService _reportService;

        public DailyReportController(
            IDailyBusinessReportService reportService)
        {
            _reportService = reportService;
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
    }
}
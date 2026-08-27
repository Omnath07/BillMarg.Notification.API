using BillMarg.Notification.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace BillMarg.Notification.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OverdueInvoiceEmailController : ControllerBase
    {
        private readonly IOverdueInvoiceEmailService _service;

        public OverdueInvoiceEmailController(
            IOverdueInvoiceEmailService service)
        {
            _service = service;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendOverdueInvoiceEmails()
        {
            try
            {
                var result =
                    await _service.SendOverdueInvoiceEmailsAsync();

                return Ok(new
                {
                    success = true,

                    message =
                        "Overdue invoice emails processed successfully.",

                    clientsProcessed =
                        result.ClientsProcessed,

                    emailsSent =
                        result.EmailsSent,

                    invoicesIncluded =
                        result.InvoicesIncluded,

                    failedEmails =
                        result.FailedEmails,

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
                            "Failed to process overdue invoice emails.",
                        error = ex.Message
                    });
            }
        }
    }
}
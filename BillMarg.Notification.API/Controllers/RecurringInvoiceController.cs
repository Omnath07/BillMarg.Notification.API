using BillMarg.Notification.API.Interface;
using Microsoft.AspNetCore.Mvc;

namespace BillMarg.Notification.API.Controllers
{
    [ApiController]
    [Route("api/recurring-invoice")]
    public class RecurringInvoiceController : ControllerBase
    {
        private readonly IRecurringInvoiceService _recurringInvoiceService;

        public RecurringInvoiceController(
            IRecurringInvoiceService recurringInvoiceService)
        {
            _recurringInvoiceService = recurringInvoiceService;
        }


        // =========================================================
        // PROCESS ALL DUE RECURRING INVOICES
        // =========================================================

        [HttpPost("process")]
        public async Task<IActionResult> ProcessAll()
        {
            try
            {
                await _recurringInvoiceService
                    .ProcessRecurringInvoicesAsync();

                return Ok(new
                {
                    success = true,
                    message =
                        "All due recurring invoices processed successfully."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message =
                        "Error while processing recurring invoices.",
                    error = ex.Message
                });
            }
        }


        // =========================================================
        // PROCESS ONE RECURRING INVOICE
        // =========================================================

        [HttpPost("process/{invoiceId:int}")]
        public async Task<IActionResult> ProcessInvoice(
            int invoiceId)
        {
            try
            {
                bool result =
                    await _recurringInvoiceService
                        .ProcessScheduledInvoiceAsync(invoiceId);

                if (!result)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message =
                            $"Recurring Invoice {invoiceId} " +
                            "could not be processed. " +
                            "Invoice may not exist, may not be recurring, " +
                            "or may be cancelled."
                    });
                }

                return Ok(new
                {
                    success = true,
                    message =
                        $"Recurring Invoice {invoiceId} " +
                        "processed successfully."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message =
                        $"Error processing InvoiceId {invoiceId}.",
                    error = ex.Message
                });
            }
        }
    }
}
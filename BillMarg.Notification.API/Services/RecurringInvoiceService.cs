using BillMarg.Notification.API.Data;
using BillMarg.Notification.API.Interface;
using BillMarg.Notification.API.Models;
using Microsoft.EntityFrameworkCore;

namespace BillMarg.Notification.API.Services
{
    public class RecurringInvoiceService : IRecurringInvoiceService
    {
        private readonly AppDbContext _context;

        public RecurringInvoiceService(AppDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // PROCESS ALL DUE RECURRING INVOICES
        // =========================================================
        public async Task ProcessRecurringInvoicesAsync(
            CancellationToken cancellationToken = default)
        {
            var today = DateTime.Today;

            var scheduledInvoices = await _context.Invoices
                .Where(x =>
                    x.IsRecurring &&
                    x.Status != "Cancel" &&
                    x.InvoiceDate.Date <= today)
                .OrderBy(x => x.InvoiceDate)
                .ToListAsync(cancellationToken);

            foreach (var invoice in scheduledInvoices)
            {
                try
                {
                    await ProcessScheduledInvoiceAsync(
                        invoice.InvoiceId,
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"Recurring invoice failed. " +
                        $"InvoiceId: {invoice.InvoiceId}, " +
                        $"InvoiceNumber: {invoice.InvoiceNumber}, " +
                        $"Error: {ex.Message}");
                }
            }
        }


        // =========================================================
        // PROCESS ONE SCHEDULED INVOICE
        // =========================================================
        public async Task<bool> ProcessScheduledInvoiceAsync(
            int invoiceId,
            CancellationToken cancellationToken = default)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    cancellationToken);

            try
            {
                // =================================================
                // GET SCHEDULED INVOICE
                // =================================================

                var scheduledInvoice =
                    await _context.Invoices
                        .FirstOrDefaultAsync(
                            x =>
                                x.InvoiceId == invoiceId &&
                                x.IsRecurring &&
                                x.Status != "Cancel",
                            cancellationToken);

                if (scheduledInvoice == null)
                    return false;


                // =================================================
                // GET INVOICE ITEMS
                // =================================================

                var invoiceItems =
                    await _context.InvoiceItems
                        .Where(x =>
                            x.InvoiceId ==
                            scheduledInvoice.InvoiceId)
                        .AsNoTracking()
                        .ToListAsync(cancellationToken);

                if (!invoiceItems.Any())
                {
                    throw new Exception(
                        $"No invoice items found for InvoiceId " +
                        $"{scheduledInvoice.InvoiceId}");
                }


                // =================================================
                // STOCK CHECK
                // =================================================

                foreach (var item in invoiceItems)
                {
                    // Service/non-product item
                    if (!item.ProductId.HasValue)
                        continue;

                    var product =
                        await _context.Products
                            .FirstOrDefaultAsync(
                                x =>
                                    x.ProductId ==
                                    item.ProductId.Value &&
                                    x.UserId ==
                                    scheduledInvoice.UserId,
                                cancellationToken);

                    if (product == null)
                    {
                        throw new Exception(
                            $"Product not found. " +
                            $"ProductId: {item.ProductId}");
                    }

                    if (product.CurrentStock < item.Quantity)
                    {
                        throw new Exception(
                            $"Insufficient stock for product: " +
                            $"{product.ProductName}");
                    }
                }


                // =================================================
                // DEDUCT STOCK
                // =================================================

                foreach (var item in invoiceItems)
                {
                    if (!item.ProductId.HasValue)
                        continue;

                    var product =
                        await _context.Products
                            .FirstOrDefaultAsync(
                                x =>
                                    x.ProductId ==
                                    item.ProductId.Value &&
                                    x.UserId ==
                                    scheduledInvoice.UserId,
                                cancellationToken);

                    if (product == null)
                        continue;

                    product.CurrentStock -= item.Quantity;

                    _context.StockTransactions.Add(
                        new StockTransaction
                        {
                            ProductId =
                                product.ProductId,

                            UserId =
                                scheduledInvoice.UserId,

                            TransactionType = "OUT",

                            Quantity =
                                item.Quantity,

                            ReferenceType = "Invoice",

                            ReferenceId =
                                scheduledInvoice.InvoiceId,

                            TransactionDate =
                                DateTime.Now,

                            Remarks =
                                $"Stock OUT against Recurring Invoice " +
                                $"{scheduledInvoice.InvoiceNumber}"
                        });
                }


                // =================================================
                // CURRENT SCHEDULED INVOICE
                // BECOMES NORMAL INVOICE
                // =================================================

                scheduledInvoice.IsRecurring = false;

                scheduledInvoice.Status = "Invoice";


                // =================================================
                // CALCULATE NEXT INVOICE DATE
                // =================================================

                var nextInvoiceDate =
                    CalculateNextInvoiceDate(
                        scheduledInvoice.InvoiceDate,
                        scheduledInvoice.RecurrenceType);


                // =================================================
                // GENERATE NEXT INVOICE NUMBER
                // =================================================

                var nextInvoiceNumber =
                    await GenerateInvoiceNumberAsync(
                        cancellationToken);


                // =================================================
                // CREATE NEXT SCHEDULED INVOICE
                // =================================================

                var nextInvoice = new Invoices
                {
                    UserId =
                        scheduledInvoice.UserId,

                    ClientId =
                        scheduledInvoice.ClientId,

                    InvoiceNumber =
                        nextInvoiceNumber,

                    Amount =
                        scheduledInvoice.Amount,

                    Status = "Scheduled",

                    InvoiceDate =
                        nextInvoiceDate,

                    DueDate =
                        CalculateDueDate(
                            nextInvoiceDate,
                            scheduledInvoice.InvoiceDate,
                            scheduledInvoice.DueDate),

                    Notes =
                        scheduledInvoice.Notes,

                    Tax =
                        scheduledInvoice.Tax,

                    Discount =
                        scheduledInvoice.Discount,

                    IsRecurring = true,

                    RecurrenceType =
                        scheduledInvoice.RecurrenceType,

                    PdfPath = null,

                    QuotationId = null,

                    CreateChallan = false,

                    CreatedOn = DateTime.Now
                };

                _context.Invoices.Add(nextInvoice);

                await _context.SaveChangesAsync(
                    cancellationToken);


                // =================================================
                // COPY INVOICE ITEMS
                // =================================================

                foreach (var sourceItem in invoiceItems)
                {
                    _context.InvoiceItems.Add(
                        new InvoiceItems
                        {
                            InvoiceId =
                                nextInvoice.InvoiceId,

                            ProductId =
                                sourceItem.ProductId,

                            Description =
                                sourceItem.Description,

                            Quantity =
                                sourceItem.Quantity,

                            ItemCode =
                                sourceItem.ItemCode,

                            UnitPrice =
                                sourceItem.UnitPrice,

                            Total =
                                sourceItem.Quantity *
                                sourceItem.UnitPrice,

                            CreatedOn =
                                DateTime.Now
                        });
                }


                await _context.SaveChangesAsync(
                    cancellationToken);


                // =================================================
                // COMMIT
                // =================================================

                await transaction.CommitAsync(
                    cancellationToken);


                Console.WriteLine(
                    $"Recurring invoice processed successfully. " +
                    $"Current Invoice: {scheduledInvoice.InvoiceNumber}, " +
                    $"Next Invoice: {nextInvoice.InvoiceNumber}, " +
                    $"Next Date: {nextInvoiceDate:dd-MM-yyyy}");


                return true;
            }
            catch
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                throw;
            }
        }


        // =========================================================
        // CALCULATE NEXT INVOICE DATE
        // =========================================================

        private DateTime CalculateNextInvoiceDate(
            DateTime currentDate,
            string? recurrenceType)
        {
            return recurrenceType?
                .Trim()
                .ToLower() switch
            {
                "daily" =>
                    currentDate.AddDays(1),

                "weekly" =>
                    currentDate.AddDays(7),

                "monthly" =>
                    currentDate.AddMonths(1),

                "yearly" =>
                    currentDate.AddYears(1),

                _ =>
                    currentDate.AddMonths(1)
            };
        }


        // =========================================================
        // CALCULATE DUE DATE
        // =========================================================

        private DateTime CalculateDueDate(
            DateTime newInvoiceDate,
            DateTime originalInvoiceDate,
            DateTime originalDueDate)
        {
            var dueDays =
                (
                    originalDueDate.Date -
                    originalInvoiceDate.Date
                ).Days;

            return newInvoiceDate.AddDays(dueDays);
        }


        // =========================================================
        // GENERATE INVOICE NUMBER
        // =========================================================

        private async Task<string> GenerateInvoiceNumberAsync(
            CancellationToken cancellationToken)
        {
            string invoiceNumber;

            do
            {
                invoiceNumber =
                    $"INV-{DateTime.Now:yyyyMMddHHmmss}-{Random.Shared.Next(100, 999)}";

            }
            while (
                await _context.Invoices.AnyAsync(
                    x =>
                        x.InvoiceNumber ==
                        invoiceNumber,
                    cancellationToken));

            return invoiceNumber;
        }
    }
}
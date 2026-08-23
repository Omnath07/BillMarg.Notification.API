
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BillMarg.Notification.API.Models
{
    public class Invoices
    {
        [Key]
        public int InvoiceId { get; set; }

        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public Users Users { get; set; }

        public int? ClientId { get; set; }
        [ForeignKey("ClientId")]
        public Clients Client { get; set; }

        [Required, MaxLength(50)]
        public string InvoiceNumber { get; set; }

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public string Status { get; set; } = "Pending";

        public DateTime InvoiceDate { get; set; }

        public DateTime DueDate { get; set; }

        public string? Notes { get; set; }

        public decimal Tax { get; set; }

        public decimal Discount { get; set; }

        public bool IsRecurring { get; set; }
        public string? PdfPath { get; set; }


        public int? QuotationId { get; set; }
        public bool CreateChallan { get; set; }
        public DateTime CreatedOn { get; set; } = DateTime.Now;
        [NotMapped]
        public decimal PaidAmount { get; set; }

        [NotMapped]
        public decimal PendingAmount
        {
            get
            {
                return Amount - PaidAmount;
            }
        }


        public ICollection<InvoiceItems> InvoiceItems { get; set; }

        public ICollection<InvoiceTimeItems> InvoiceTimeItems { get; set; } = new List<InvoiceTimeItems>();
    }
    public class InvoiceViewModel
    {
        public int InvoiceId { get; set; }

        [Required, MaxLength(50)]
        public string InvoiceNumber { get; set; }

        [Required]
        public int ClientId { get; set; }

        [Required]
        public DateTime InvoiceDate { get; set; }

        [Required]
        public DateTime DueDate { get; set; }

        public string Notes { get; set; }
        public decimal Amount { get; set; }

        public decimal Tax { get; set; }

        public decimal Discount { get; set; }

        public string Status { get; set; }

        public bool IsRecurring { get; set; }

        public string RecurrenceType { get; set; }
        public bool CreateChallan { get; set; }

        public string? DriverName { get; set; }

        public string? VehicleNumber { get; set; }

        public string? Remarks { get; set; }

        public decimal? PaidAmount { get; set; }

        public string? PaymentType { get; set; }

        public string? ReferenceNo { get; set; }

        public DateTime? PaymentDate { get; set; }


        public List<QuotationItems> QuotationItems { get; set; } = new List<QuotationItems>();

        public List<InvoiceItems> InvoiceItems { get; set; } = new List<InvoiceItems>();

        public ICollection<InvoiceTimeItems> InvoiceTimeItems { get; set; } = new List<InvoiceTimeItems>();
    }
    public class InvoiceTimeViewModel
    {
        public int InvoiceId { get; set; }
        public string InvoiceNumber { get; set; }
        public int ClientId { get; set; }
        public DateTime InvoiceDate { get; set; }
        public DateTime DueDate { get; set; }
        public string Notes { get; set; }
        public decimal Amount { get; set; }
        public decimal Tax { get; set; }
        public decimal Discount { get; set; }
        public bool IsRecurring { get; set; }
        public string RecurrenceType { get; set; }
        public string InvoiceType { get; set; } = "Time"; // Default to Time-Based Invoice
   
        public List<TimeEntryViewModel> TimeEntries { get; set; } = new List<TimeEntryViewModel>();
    }

    public class TimeEntryViewModel
    {
        public string Description { get; set; }
        public decimal HoursWorked { get; set; }
        public decimal HourlyRate { get; set; }
        public decimal Total => HoursWorked * HourlyRate; // Calculated property
    }

    public class InvoiceTimeItems
    {
        [Key]
        public int ItemId { get; set; }

        public int InvoiceId { get; set; }
        [ForeignKey("InvoiceId")]
        public Invoices Invoice { get; set; }

        public string Description { get; set; }

        public decimal? HoursWorked { get; set; }
        public decimal? HourlyRate { get; set; }

        public decimal Total { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.Now;
    }

        public class PendingInvoiceReportViewModel
        {
            public int InvoiceId { get; set; }

            public string InvoiceNumber { get; set; }

            public string ClientName { get; set; }

            public DateTime InvoiceDate { get; set; }

            public DateTime DueDate { get; set; }

            public decimal InvoiceAmount { get; set; }

            public decimal PaidAmount { get; set; }

            public decimal PendingAmount { get; set; }

            public string Status { get; set; }

            public int OverDueDays { get; set; }
    }

        public class InvoicePaymentViewModel
    {
            public Invoices Invoice { get; set; }

            public List<InvoicePayments> Payments { get; set; }
                = new List<InvoicePayments>();

            public decimal TotalPaid
            {
                get
                {
                    return Payments.Sum(x => x.Amount);
                }
            }

            public decimal DueAmount
            {
                get
                {
                    return Invoice != null
                        ? Invoice.Amount - TotalPaid
                        : 0;
                }
            }
        }
    }




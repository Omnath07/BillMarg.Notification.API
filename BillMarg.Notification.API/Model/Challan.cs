
using BillMarg.Notification.API.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("Challans")]
public class Challan
{
    [Key]
    public int ChallanId { get; set; }

    public int InvoiceId { get; set; }

    public int UserId { get; set; }

    public string? ChallanNumber { get; set; }

    public DateTime? ChallanDate { get; set; }

    public string? Status { get; set; }

    public string? VehicleNumber { get; set; }

    public string? DriverName { get; set; }

    public string? Remarks { get; set; }

    public string? PdfPath { get; set; }

    public DateTime CreatedOn { get; set; }

    public virtual Invoices Invoice { get; set; }
}
public class ChallanViewModel
{
    public int QuotationId { get; set; }

    public bool CreateChallan { get; set; }

    public string? DriverName { get; set; }

    public string? VehicleNumber { get; set; }

    public string? Remarks { get; set; }
}

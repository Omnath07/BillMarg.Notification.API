
using BillMarg.Notification.API.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


public class StockTransaction
{

    [Key]
    public int StockTransactionId { get; set; }


    public int ProductId { get; set; }


    public int UserId { get; set; }


    public string TransactionType { get; set; }
    // IN / OUT


    public decimal Quantity { get; set; }


    public string ReferenceType { get; set; }


    public int? ReferenceId { get; set; }


    public DateTime TransactionDate { get; set; }


    public string Remarks { get; set; }



    public Product Product { get; set; }


}


public class StockReportViewModel
{
    public List<LowStockItem> LowStockProducts { get; set; }

    public List<StockMovementItem> StockMovements { get; set; }

    public int TotalProducts { get; set; }

    public int LowStockCount { get; set; }

    public int OutOfStockCount { get; set; }

    public decimal StockIn { get; set; }

    public decimal StockOut { get; set; }

    public int UserId { get; set; }
    public int Year { get; set; }
    public int? SelectedMonth { get; set; }
    public decimal TotalOpeningStock { get; set; }
    public decimal TotalStockAdded { get; set; }
    public decimal TotalStockOut { get; set; }
    public decimal TotalClosingStock { get; set; }
    public decimal TotalCurrentStock { get; set; }

}

public class LowStockItem
{
    public int ProductId { get; set; }

    public string ProductName { get; set; }

    public string ProductCode { get; set; }

    public decimal CurrentStock { get; set; }

    public decimal MinimumStock { get; set; }
}

public class StockMovementItem
{
    public DateTime Date { get; set; }

    public string ProductName { get; set; }

    public string TransactionType { get; set; }

    public decimal Quantity { get; set; }

    public string ReferenceType { get; set; }

    public string Remarks { get; set; }
}
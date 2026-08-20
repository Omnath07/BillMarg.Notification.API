using System.ComponentModel.DataAnnotations;

namespace BillSathi.Models
{

    public class Product
    {

        [Key]
        public int ProductId { get; set; }


        public int UserId { get; set; }


        [Required]
        public string ProductName { get; set; }


        public string? ProductCode { get; set; }


        public string? Description { get; set; }


        public string? Unit { get; set; }


        public decimal PurchasePrice { get; set; }


        public decimal SellingPrice { get; set; }


        public decimal CurrentStock { get; set; }


        public decimal MinimumStock { get; set; }


        public bool IsActive { get; set; }


        public DateTime CreatedDate { get; set; }



        public int? CategoryId { get; set; }




    }
}
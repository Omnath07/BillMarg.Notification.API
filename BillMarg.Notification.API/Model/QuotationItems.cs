using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BillSathi.Models
{
    public class QuotationItems
    {
        [Key]
        public int ItemId { get; set; }

        [Required]
        public int QuotationId { get; set; }

        [ForeignKey("QuotationId")]
        public Quotations Quotation { get; set; }

        [Required]
        [MaxLength(500)]
        public string Description { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than zero")]
        public int Quantity { get; set; }

        public int? ProductId { get; set; }   // Must exist

        [ForeignKey("ProductId")]
        public Product Product { get; set; }
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        public string? ItemCode { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)] 
        public decimal Total { get; private set; } 

        public DateTime CreatedOn { get; set; } = DateTime.Now;
    }


}

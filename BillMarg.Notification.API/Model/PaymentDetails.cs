using BillSathi.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BillSathi.Models
{
    public class PaymentDetails
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey("UserDetails")]
        public int UserId { get; set; }

        [Required]
        [StringLength(255)]
        public string PaymentId { get; set; }

        [Required]
        [StringLength(255)]
        public string OrderId { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Amount { get; set; }

        public decimal StorageMB { get; set; }
        
        public string MyPlan { get; set; }

        public int Duration { get; set; }


        [Required]
        public DateTime PaymentDate { get; set; }

    }

       
    }




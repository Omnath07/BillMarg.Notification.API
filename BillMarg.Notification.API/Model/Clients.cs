using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BillMarg.Notification.API.Models
{
    public class Clients
    {
        [Key]
        public int ClientId { get; set; }

        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public Users Users { get; set; }

        [Required, MaxLength(255)]
        public string Name { get; set; }   // Required, keep non-nullable

        [EmailAddress, MaxLength(255)]
        public string? Email { get; set; } // Nullable

        [MaxLength(20)]
        public string? Phone { get; set; } // Nullable

        public string? Address { get; set; } // Nullable

        public string? GSTTINNo { get; set; } // Already nullable
    }

}

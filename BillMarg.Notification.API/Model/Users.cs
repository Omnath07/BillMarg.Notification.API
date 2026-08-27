using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Numerics;
using System.ComponentModel.DataAnnotations;

namespace BillMarg.Notification.API.Models
{
    public class Users
    {
        [Key]
        public int UserId { get; set; }

        [Required, MaxLength(255)]
        public string FullName { get; set; }

        [Required, EmailAddress, MaxLength(255)]
        public string Email { get; set; }

        [Required, MaxLength(255)]
        public string Password { get; set; }

        [MaxLength(20)]
        [Required]
        [RegularExpression(@"^[6-9]\d{9}$",
            ErrorMessage = "Enter a valid 10-digit mobile number.")]
        public string Phone { get; set; }

        [MaxLength(255)]
        public string BusinessName { get; set; }
        public int? ParentUserId { get; set; }
        public string? Address { get; set; }

        [MaxLength(255)]
        public string? LoginToken { get; set; }
        public string Logo { get; set; }  // Path or URL

        // Password Reset
        public string? PasswordResetToken { get; set; }

        public DateTime? PasswordResetTokenExpiry { get; set; }


        public DateTime CreatedOn { get; set; } = DateTime.Now;

        public bool IsActive { get; set; }

      
        //// Relationships
        //public ICollection<Clients> Clients { get; set; }
        //public ICollection<Invoices> Invoices { get; set; }
        //public ICollection<Quaternion> Quotations { get; set; }
        //public ICollection<Customization> Customizations { get; set; }
    }


    public class UserPlan
    {
        [Key]
        public int UserId { get; set; }

        [Required, MaxLength(255)]
        public string FullName { get; set; }

        [Required, EmailAddress, MaxLength(255)]
        public string? Email { get; set; }

        [Required, MaxLength(255)]
        public string Password { get; set; }

        [MaxLength(20)]
        public string Phone { get; set; }

        [MaxLength(255)]
        public string? BusinessName { get; set; }
       
        public string Logo { get; set; }  // Path or URL

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        public bool IsActive { get; set; }

        public string MyPlan { get; set; }

        public decimal Amount { get; set; }

        public int Duration { get; set; }
      
    }

    public class UserProfileViewModel
    {

        public int UserId { get; set; }
        public string FullName { get; set; }

        [EmailAddress]
        public string Email { get; set; }

        [Required, MaxLength(20)]
        public string Phone { get; set; }

        [MaxLength(255)]
        public string BusinessName { get; set; }

        public string? Address { get; set; }

        public string Plan { get; set; }
        public DateTime? NextRenewalDate { get; set; }
    }

        public class ResetPasswordViewModel
        {
            public int UserId { get; set; }

            public string? FullName { get; set; }

            public string? Email { get; set; }

            [Required]
            [DataType(DataType.Password)]
            [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
            public string NewPassword { get; set; }

            [Required]
            [DataType(DataType.Password)]
            [Compare("NewPassword",
                ErrorMessage = "New Password and Confirm Password do not match.")]
            public string ConfirmPassword { get; set; }

            public string Token { get; set; }
        }
    }



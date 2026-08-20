using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BillSathi.Models
{
    [Table("ExpenseCategory")]
    public class ExpenseCategory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string CategoryName { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedDate { get; set; }

        public virtual ICollection<Expense> Expenses { get; set; }
            = new List<Expense>();
    }
}
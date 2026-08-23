using BillMarg.Notification.API.Models;
using Microsoft.EntityFrameworkCore;

namespace BillMarg.Notification.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // =====================================================
        // USERS
        // =====================================================

        public DbSet<Users> Users { get; set; }

        public DbSet<UserPlan> UserPlans { get; set; }

        // =====================================================
        // CLIENTS
        // =====================================================

        public DbSet<Clients> Clients { get; set; }

        // =====================================================
        // PRODUCTS
        // =====================================================

        public DbSet<Product> Products { get; set; }

        // =====================================================
        // QUOTATIONS
        // =====================================================

        public DbSet<Quotations> Quotations { get; set; }

        public DbSet<QuotationItems> QuotationItems { get; set; }

        // =====================================================
        // INVOICES
        // =====================================================

        public DbSet<Invoices> Invoices { get; set; }

        public DbSet<InvoiceItems> InvoiceItems { get; set; }

        public DbSet<InvoiceTimeItems> InvoiceTimeItems { get; set; }

        // =====================================================
        // PAYMENTS
        // =====================================================

        public DbSet<InvoicePayments> InvoicePayments { get; set; }

        public DbSet<PaymentDetails> PaymentDetails { get; set; }

        // =====================================================
        // CHALLANS
        // =====================================================

        public DbSet<Challan> Challans { get; set; }

        // =====================================================
        // EXPENSE
        // =====================================================

        public DbSet<Expense> Expenses { get; set; }

        public DbSet<ExpenseCategory> ExpenseCategories { get; set; }

        // =====================================================
        // PURCHASE
        // =====================================================

        public DbSet<Purchase> Purchases { get; set; }
        public DbSet<QuickBill> QuickBills { get; set; }
        

        // =====================================================
        // NOTIFICATION
        // =====================================================

        public DbSet<UserNotificationSettings>
            UserNotificationSettings
        { get; set; }


        // =====================================================
        // MODEL CONFIGURATION
        // =====================================================

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


            // =================================================
            // USERS
            // =================================================

            modelBuilder.Entity<Users>()
                .HasKey(x => x.UserId);


            // =================================================
            // CLIENT -> USER
            // =================================================

            modelBuilder.Entity<Clients>()
                .HasOne(x => x.Users)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);


            // =================================================
            // QUOTATION -> USER
            // =================================================

            modelBuilder.Entity<Quotations>()
                .HasOne(x => x.Users)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);


            // =================================================
            // QUOTATION -> CLIENT
            // =================================================

            modelBuilder.Entity<Quotations>()
                .HasOne(x => x.Client)
                .WithMany()
                .HasForeignKey(x => x.ClientId)
                .OnDelete(DeleteBehavior.Restrict);


            // =================================================
            // QUOTATION ITEMS -> QUOTATION
            // =================================================

            modelBuilder.Entity<QuotationItems>()
                .HasOne(x => x.Quotation)
                .WithMany(x => x.QuotationItems)
                .HasForeignKey(x => x.QuotationId)
                .OnDelete(DeleteBehavior.Cascade);


            // =================================================
            // QUOTATION ITEM -> PRODUCT
            // =================================================

            modelBuilder.Entity<QuotationItems>()
                .HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);


            // =================================================
            // INVOICE -> USER
            // =================================================

            modelBuilder.Entity<Invoices>()
                .HasOne(x => x.Users)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);


            // =================================================
            // INVOICE -> CLIENT
            // =================================================

            modelBuilder.Entity<Invoices>()
                .HasOne(x => x.Client)
                .WithMany()
                .HasForeignKey(x => x.ClientId)
                .OnDelete(DeleteBehavior.Restrict);


            // =================================================
            // INVOICE ITEMS -> INVOICE
            // =================================================

            modelBuilder.Entity<InvoiceItems>()
                .HasOne(x => x.Invoice)
                .WithMany(x => x.InvoiceItems)
                .HasForeignKey(x => x.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);


            // =================================================
            // INVOICE ITEM -> PRODUCT
            // =================================================

            modelBuilder.Entity<InvoiceItems>()
                .HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);


            // =================================================
            // TIME INVOICE ITEMS -> INVOICE
            // =================================================

            modelBuilder.Entity<InvoiceTimeItems>()
                .HasOne(x => x.Invoice)
                .WithMany(x => x.InvoiceTimeItems)
                .HasForeignKey(x => x.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);


            // =================================================
            // CHALLAN -> INVOICE
            // =================================================

            modelBuilder.Entity<Challan>()
                .HasOne(x => x.Invoice)
                .WithMany()
                .HasForeignKey(x => x.InvoiceId)
                .OnDelete(DeleteBehavior.Restrict);


            // =================================================
            // EXPENSE -> USER
            // =================================================

            modelBuilder.Entity<Expense>()
                .HasOne<Users>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);


            // =================================================
            // EXPENSE -> CATEGORY
            // =================================================

            modelBuilder.Entity<Expense>()
                .HasOne(x => x.ExpenseCategory)
                .WithMany(x => x.Expenses)
                .HasForeignKey(x => x.ExpenseCategoryId)
                .OnDelete(DeleteBehavior.Restrict);


            // =================================================
            // INVOICE PAYMENT -> INVOICE
            // =================================================

            modelBuilder.Entity<InvoicePayments>()
                .HasOne<Invoices>()
                .WithMany()
                .HasForeignKey(x => x.InvoiceId)
                .OnDelete(DeleteBehavior.Restrict);


            // =================================================
            // INVOICE PAYMENT -> USER
            // =================================================

            modelBuilder.Entity<InvoicePayments>()
                .HasOne<Users>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);


            // =================================================
            // NOTIFICATION SETTINGS -> USER
            // =================================================

            modelBuilder.Entity<UserNotificationSettings>()
                .HasIndex(x => x.UserId)
                .IsUnique();


            // =================================================
            // NOTIFICATION SETTINGS
            // =================================================

            modelBuilder.Entity<UserNotificationSettings>()
                .Property(x => x.EmailEnabled)
                .HasDefaultValue(true);


            // =================================================
            // MONEY / DECIMAL PRECISION
            // =================================================

            modelBuilder.Entity<Invoices>()
                .Property(x => x.Amount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Invoices>()
                .Property(x => x.Tax)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Invoices>()
                .Property(x => x.Discount)
                .HasColumnType("decimal(18,2)");


            modelBuilder.Entity<InvoiceItems>()
                .Property(x => x.UnitPrice)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<InvoiceItems>()
                .Property(x => x.Total)
                .HasColumnType("decimal(18,2)");


            modelBuilder.Entity<QuotationItems>()
                .Property(x => x.UnitPrice)
                .HasColumnType("decimal(18,2)");


            modelBuilder.Entity<InvoiceTimeItems>()
                .Property(x => x.HoursWorked)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<InvoiceTimeItems>()
                .Property(x => x.HourlyRate)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<InvoiceTimeItems>()
                .Property(x => x.Total)
                .HasColumnType("decimal(18,2)");


            modelBuilder.Entity<Expense>()
                .Property(x => x.Amount)
                .HasColumnType("decimal(18,2)");


            modelBuilder.Entity<Product>()
                .Property(x => x.PurchasePrice)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Product>()
                .Property(x => x.SellingPrice)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Product>()
                .Property(x => x.CurrentStock)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Product>()
                .Property(x => x.MinimumStock)
                .HasColumnType("decimal(18,2)");


            modelBuilder.Entity<PaymentDetails>()
                .Property(x => x.Amount)
                .HasColumnType("decimal(10,2)");
        }
    }
}
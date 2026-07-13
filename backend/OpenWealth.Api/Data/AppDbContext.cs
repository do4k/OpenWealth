using Microsoft.EntityFrameworkCore;
using OpenWealth.Api.Models;

namespace OpenWealth.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<IncomeDetails> IncomeDetails => Set<IncomeDetails>();
    public DbSet<TaxSettings> TaxSettings => Set<TaxSettings>();
    public DbSet<StudentLoan> StudentLoans => Set<StudentLoan>();
    public DbSet<StudentLoanPlanSetting> StudentLoanPlanSettings => Set<StudentLoanPlanSetting>();
    public DbSet<Property> Properties => Set<Property>();
    public DbSet<Mortgage> Mortgages => Set<Mortgage>();
    public DbSet<SavingsAccount> SavingsAccounts => Set<SavingsAccount>();
    public DbSet<Investment> Investments => Set<Investment>();
    public DbSet<ShareSettings> ShareSettings => Set<ShareSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.HasOne(u => u.Income).WithOne().HasForeignKey<IncomeDetails>(i => i.UserId);
            e.HasOne(u => u.TaxSettings).WithOne().HasForeignKey<TaxSettings>(t => t.UserId);
            e.HasOne(u => u.ShareSettings).WithOne().HasForeignKey<ShareSettings>(s => s.UserId);
            e.HasMany(u => u.StudentLoanPlanSettings).WithOne().HasForeignKey(s => s.UserId);
            e.HasMany(u => u.StudentLoans).WithOne().HasForeignKey(s => s.UserId);
            e.HasMany(u => u.Properties).WithOne().HasForeignKey(p => p.UserId);
            e.HasMany(u => u.Mortgages).WithOne().HasForeignKey(m => m.UserId);
            e.HasMany(u => u.SavingsAccounts).WithOne().HasForeignKey(s => s.UserId);
            e.HasMany(u => u.Investments).WithOne().HasForeignKey(i => i.UserId);
        });

        modelBuilder.Entity<StudentLoanPlanSetting>()
            .HasIndex(s => new { s.UserId, s.Plan }).IsUnique();

        modelBuilder.Entity<ShareSettings>()
            .HasIndex(s => s.Slug).IsUnique();

        modelBuilder.Entity<Property>()
            .HasMany(p => p.Mortgages).WithOne().HasForeignKey(m => m.PropertyId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

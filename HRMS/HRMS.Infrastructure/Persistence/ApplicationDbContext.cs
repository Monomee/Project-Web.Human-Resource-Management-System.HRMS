using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using HRMS.Domain.Entities;
using HRMS.Application.Interfaces;

namespace HRMS.Infrastructure.Persistence;

public partial class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext()
    {
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Account> Accounts { get; set; }

    public virtual DbSet<AttendanceLog> AttendanceLogs { get; set; }

    public virtual DbSet<Department> Departments { get; set; }

    public virtual DbSet<EmploymentContract> EmploymentContracts { get; set; }

    public virtual DbSet<LeaveBalance> LeaveBalances { get; set; }

    public virtual DbSet<Payslip> Payslips { get; set; }

    public virtual DbSet<Position> Positions { get; set; }

    public virtual DbSet<Request> Requests { get; set; }

    public virtual DbSet<RequestType> RequestTypes { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<TimesheetPeriod> TimesheetPeriods { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public DbSet<EmployeeRequest> EmployeeRequests => Set<EmployeeRequest>();
    public DbSet<OvertimeRecord> OvertimeRecords => Set<OvertimeRecord>();
    public DbSet<TimesheetAdjustment> TimesheetAdjustments => Set<TimesheetAdjustment>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Accounts__3214EC07E25E7AC5");

            entity.HasIndex(e => e.UserId, "UQ__Accounts__1788CC4D7740304A").IsUnique();

            entity.HasIndex(e => e.Username, "UQ__Accounts__536C85E44841A678").IsUnique();

            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Status).HasDefaultValue(true);
            entity.Property(e => e.Username)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.User).WithOne(p => p.Account)
                .HasForeignKey<Account>(d => d.UserId)
                .HasConstraintName("FK_Accounts_Users");

            entity.HasMany(d => d.Roles).WithMany(p => p.Accounts)
                .UsingEntity<Dictionary<string, object>>(
                    "AccountRole",
                    r => r.HasOne<Role>().WithMany()
                        .HasForeignKey("RoleId")
                        .HasConstraintName("FK_AccountRoles_Roles"),
                    l => l.HasOne<Account>().WithMany()
                        .HasForeignKey("AccountId")
                        .HasConstraintName("FK_AccountRoles_Accounts"),
                    j =>
                    {
                        j.HasKey("AccountId", "RoleId");
                        j.ToTable("AccountRoles");
                    });
        });

        modelBuilder.Entity<AttendanceLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Attendan__3214EC07BDED11DB");

            entity.HasIndex(e => new { e.UserId, e.PeriodId }, "IX_AttendanceLogs_UserPeriod");

            entity.Property(e => e.CheckType)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CheckedAt).HasColumnType("datetime");
            entity.Property(e => e.Source)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Excel");

            entity.HasOne(d => d.Period).WithMany(p => p.AttendanceLogs)
                .HasForeignKey(d => d.PeriodId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AttendanceLogs_Periods");

            entity.HasOne(d => d.User).WithMany(p => p.AttendanceLogs)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AttendanceLogs_Users");
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Departme__3214EC07DA26E043");

            entity.HasIndex(e => e.Code, "UQ__Departme__A25C5AA7166DB0CF").IsUnique();

            entity.Property(e => e.Code)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Name).HasMaxLength(100);

            entity.HasOne(d => d.HeadAccount).WithMany(p => p.Departments)
                .HasForeignKey(d => d.HeadAccountId)
                .HasConstraintName("FK_Departments_Accounts");
        });

        modelBuilder.Entity<EmploymentContract>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Employme__3214EC0792E1F3FD");

            entity.HasIndex(e => e.ContractNo, "UQ__Employme__C908F4B8F5F4B0D7").IsUnique();

            entity.Property(e => e.BaseSalary).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ContractNo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ContractType)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Active");

            entity.HasOne(d => d.User).WithMany(p => p.EmploymentContracts)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_EmploymentContracts_Users");
        });

        modelBuilder.Entity<LeaveBalance>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__LeaveBal__3214EC0767DE4A60");

            entity.Property(e => e.RemainingDays).HasDefaultValue(12);
            entity.Property(e => e.TotalDays).HasDefaultValue(12);

            entity.HasOne(d => d.User).WithMany(p => p.LeaveBalances)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_LeaveBalances_Users");
        });

        modelBuilder.Entity<Payslip>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Payslips__3214EC07CB0D3031");

            entity.Property(e => e.Allowances).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.BaseSalary).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.GrossAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.InsuranceDeduction).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.NetAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.OtSalary).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Draft");
            entity.Property(e => e.TaxDeduction).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Period).WithMany(p => p.Payslips)
                .HasForeignKey(d => d.PeriodId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payslips_Periods");

            entity.HasOne(d => d.User).WithMany(p => p.Payslips)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payslips_Users");
        });

        modelBuilder.Entity<Position>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Position__3214EC07816BF78B");

            entity.HasIndex(e => e.Code, "UQ__Position__A25C5AA7BAB037E6").IsUnique();

            entity.Property(e => e.Code)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<Request>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Requests__3214EC07B5493308");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.EndDate).HasColumnType("datetime");
            entity.Property(e => e.Reason).HasMaxLength(255);
            entity.Property(e => e.StartDate).HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Pending");
            entity.Property(e => e.Title).HasMaxLength(100);
            entity.Property(e => e.Value).HasColumnType("decimal(5, 1)");

            entity.HasIndex(e => new { e.CurrentApproverAccountId, e.Status }, "IX_Requests_Approver");
            entity.HasIndex(e => e.CreatedByAccountId, "IX_Requests_Creator");

            entity.HasOne(d => d.CreatedByAccount).WithMany(p => p.RequestCreatedByAccounts)
                .HasForeignKey(d => d.CreatedByAccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Requests_Creator");

            entity.HasOne(d => d.CurrentApproverAccount).WithMany(p => p.RequestCurrentApproverAccounts)
                .HasForeignKey(d => d.CurrentApproverAccountId)
                .HasConstraintName("FK_Requests_Approver");

            entity.HasOne(d => d.RequestType).WithMany(p => p.Requests)
                .HasForeignKey(d => d.RequestTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Requests_RequestTypes");
        });

        modelBuilder.Entity<RequestType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__RequestT__3214EC077FC0F6D8");

            entity.HasIndex(e => e.Code, "UQ__RequestT__A25C5AA7A2B20EF0").IsUnique();

            entity.Property(e => e.Code)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Name).HasMaxLength(50);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Roles__3214EC07DCE0F482");

            entity.HasIndex(e => e.Name, "UQ__Roles__737584F6ABDA03A5").IsUnique();

            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TimesheetPeriod>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Timeshee__3214EC0785725B6A");

            entity.Property(e => e.Name).HasMaxLength(50);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Users__3214EC07B63ACFFB");

            entity.HasIndex(e => e.EmployeeCode, "UQ__Users__1F6425482D013EC6").IsUnique();

            entity.HasIndex(e => e.EmailCompany, "UQ__Users__7596B7B594605A4B").IsUnique();

            entity.Property(e => e.EmailCompany)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.EmployeeCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.Phone)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.Status).HasDefaultValue(true);

            entity.HasOne(d => d.Department).WithMany(p => p.Users)
                .HasForeignKey(d => d.DepartmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Users_Departments");

            entity.HasOne(d => d.Position).WithMany(p => p.Users)
                .HasForeignKey(d => d.PositionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Users_Positions");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using SalesMarketing.Models;

namespace SalesMarketing.Data;

public partial class SalesMarketingDbContext : DbContext
{
    public SalesMarketingDbContext(DbContextOptions<SalesMarketingDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Appuser> Appusers { get; set; }

    public virtual DbSet<Marketingteam> Marketingteams { get; set; }

    public virtual DbSet<Marketingteamdetail> Marketingteamdetails { get; set; }

    public virtual DbSet<Salesman> Salesmen { get; set; }
    public virtual DbSet<Salesmanremovalrequest> Salesmanremovalrequests { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Appuser>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("appuser_pkey");

            entity.ToTable("appuser");

            entity.HasIndex(e => e.Username, "uq_appuser_username").IsUnique();

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.Adminrole).HasColumnName("adminrole");
            entity.Property(e => e.Password)
                .HasMaxLength(255)
                .HasColumnName("password");
            entity.Property(e => e.Username)
                .HasMaxLength(50)
                .HasColumnName("username");
        });

        modelBuilder.Entity<Marketingteam>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("marketingteam_pkey");

            entity.ToTable("marketingteam");

            entity.HasIndex(e => e.Teamcode, "marketingteam_teamcode_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Createddate)
                .HasDefaultValueSql("now()")
                .HasColumnName("createddate");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasColumnName("description");
            entity.Property(e => e.Isarchived)
                .HasDefaultValue(false)
                .HasColumnName("isarchived");
            entity.Property(e => e.Modifieddate).HasColumnName("modifieddate");
            entity.Property(e => e.Status)
                .HasDefaultValue((short)1)
                .HasColumnName("status");
            entity.Property(e => e.Teamcode)
                .HasMaxLength(20)
                .HasColumnName("teamcode");
            entity.Property(e => e.Teamleaderid).HasColumnName("teamleaderid");
            entity.Property(e => e.Teamname)
                .HasMaxLength(100)
                .HasColumnName("teamname");

            entity.HasOne(d => d.Teamleader).WithMany(p => p.Marketingteams)
                .HasForeignKey(d => d.Teamleaderid)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_marketingteam_teamleader");
        });

        modelBuilder.Entity<Marketingteamdetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("marketingteamdetail_pkey");

            entity.ToTable("marketingteamdetail");

            entity.HasIndex(e => new { e.Marketingteamid, e.Salesmanid }, "uq_marketingteamdetail_team_salesman").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Isarchived)
                .HasDefaultValue(false)
                .HasColumnName("isarchived");
            entity.Property(e => e.Marketingteamid).HasColumnName("marketingteamid");
            entity.Property(e => e.Role).HasColumnName("role");
            entity.Property(e => e.Salesmanid).HasColumnName("salesmanid");
            entity.Property(e => e.Status)
                .HasDefaultValue((short)1)
                .HasColumnName("status");
            entity.Property(e => e.Targetamount)
                .HasPrecision(18, 2)
                .HasColumnName("targetamount");

            entity.HasOne(d => d.Marketingteam).WithMany(p => p.Marketingteamdetails)
                .HasForeignKey(d => d.Marketingteamid)
                .HasConstraintName("fk_teamdetail_team");

            entity.HasOne(d => d.Salesman).WithMany(p => p.Marketingteamdetails)
                .HasForeignKey(d => d.Salesmanid)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_teamdetail_salesman");
        });

        modelBuilder.Entity<Salesman>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("salesman_pkey");

            entity.ToTable("salesman");

            entity.HasIndex(e => e.Salesmancode, "salesman_salesmancode_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Createddate)
                .HasDefaultValueSql("now()")
                .HasColumnName("createddate");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasColumnName("email");
            entity.Property(e => e.Isarchived)
                .HasDefaultValue(false)
                .HasColumnName("isarchived");
            entity.Property(e => e.Joiningdate).HasColumnName("joiningdate");
            entity.Property(e => e.Mobileno)
                .HasMaxLength(20)
                .HasColumnName("mobileno");
            entity.Property(e => e.Modifieddate).HasColumnName("modifieddate");
            entity.Property(e => e.Salesmancode)
                .HasMaxLength(20)
                .HasColumnName("salesmancode");
            entity.Property(e => e.Salesmanname)
                .HasMaxLength(100)
                .HasColumnName("salesmanname");
            entity.Property(e => e.Status)
                .HasDefaultValue((short)1)
                .HasColumnName("status");
        });


        modelBuilder.Entity<Salesmanremovalrequest>(entity =>
        {
            entity.HasKey(e => e.Id)
                .HasName("salesmanremovalrequest_pkey");

            entity.ToTable("salesmanremovalrequest");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");

            entity.Property(e => e.Salesmanid)
                .HasColumnName("salesmanid");

            entity.Property(e => e.Marketingteamid)
                .HasColumnName("marketingteamid");

            entity.Property(e => e.Requestedby)
                .HasColumnName("requestedby");

            entity.Property(e => e.Requesteddate)
                .HasDefaultValueSql("now()")
                .HasColumnName("requesteddate");

            entity.Property(e => e.Status)
                .HasDefaultValue((short)1)
                .HasColumnName("status");

            entity.Property(e => e.Reviewedby)
                .HasColumnName("reviewedby");

            entity.Property(e => e.Revieweddate)
                .HasColumnName("revieweddate");

            entity.Property(e => e.Rejectionreason)
                .HasMaxLength(500)
                .HasColumnName("rejectionreason");

            entity.HasOne<Salesman>()
                .WithMany()
                .HasForeignKey(e => e.Salesmanid)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_removalrequest_salesman");

            entity.HasOne<Marketingteam>()
                .WithMany()
                .HasForeignKey(e => e.Marketingteamid)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_removalrequest_team");

            entity.HasOne<Appuser>()
                .WithMany()
                .HasForeignKey(e => e.Requestedby)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_removalrequest_requestedby");

            entity.HasOne<Appuser>()
                .WithMany()
                .HasForeignKey(e => e.Reviewedby)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_removalrequest_reviewedby");
        });


        OnModelCreatingPartial(modelBuilder);
    }
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

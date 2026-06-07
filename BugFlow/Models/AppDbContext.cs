using Microsoft.EntityFrameworkCore;

namespace BugFlow.Models;

public class AppDbContext:DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
    public DbSet<Scope> Scopes => Set<Scope>();
    public DbSet<Vulnerability> Vulnerabilities => Set<Vulnerability>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Project>()
            .HasIndex(x => x.Code)
            .IsUnique();

        modelBuilder.Entity<ProjectMember>()
            .HasKey(x => new { x.ProjectId, x.UserId });

        modelBuilder.Entity<ProjectMember>()
            .HasOne(pm => pm.Project)
            .WithMany(p => p.Members)
            .HasForeignKey(pm => pm.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProjectMember>()
            .HasOne(pm => pm.User)
            .WithMany(u => u.Projects)
            .HasForeignKey(pm => pm.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Vulnerability>()
            .Property(v => v.Cvss)
            .HasPrecision(3, 1);

        modelBuilder.Entity<Vulnerability>()
            .HasOne(v => v.Creator)
            .WithMany()
            .HasForeignKey(v => v.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Vulnerability>()
            .HasOne(v => v.Project)
            .WithMany(p => p.Vulnerabilities)
            .HasForeignKey(v => v.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }

}
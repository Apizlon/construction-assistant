using Microsoft.EntityFrameworkCore;
using ProjectCalculationService.Application.Models;

namespace ProjectCalculationService.DataAccess;

public class ProjectCalculationDbContext : DbContext
{
    public ProjectCalculationDbContext(DbContextOptions<ProjectCalculationDbContext> options) : base(options)
    {
    }

    public DbSet<Project> Projects { get; set; } = null!;
    public DbSet<ProjectStepAnswer> ProjectStepAnswers { get; set; } = null!;
    public DbSet<SharedProject> SharedProjects { get; set; } = null!;
    public DbSet<SharedProjectViewer> SharedProjectViewers { get; set; } = null!;
    public DbSet<ViewerComment> ViewerComments { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Project>(entity =>
        {
            entity.ToTable("projects");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(256);

            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(64);

            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            entity.HasIndex(e => e.OwnerUserId);
        });

        modelBuilder.Entity<ProjectStepAnswer>(entity =>
        {
            entity.ToTable("project_step_answers");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.StepCode).IsRequired().HasMaxLength(256);

            entity.Property(e => e.AnswerType)
                .HasConversion<string>()
                .HasMaxLength(64);

            entity.Property(e => e.Source)
                .HasConversion<string>()
                .HasMaxLength(64);

            entity.Property(e => e.SelectedOptionCode).HasMaxLength(256);

            entity.Property(e => e.ValueJson)
                .HasColumnType("jsonb");

            entity.Property(e => e.UpdatedAt).IsRequired();

            entity.HasIndex(e => new { e.ProjectId, e.StepCode }).IsUnique();

            entity.HasOne<Project>()
                .WithMany()
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SharedProject>(entity =>
        {
            entity.ToTable("shared_projects");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(64);
            entity.Property(e => e.SharedAt).IsRequired();
            entity.Property(e => e.IsActive).IsRequired();

            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => new { e.ProjectId, e.IsActive });

            entity.HasOne<Project>()
                .WithMany()
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SharedProjectViewer>(entity =>
        {
            entity.ToTable("shared_project_viewers");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.IsActive).IsRequired();

            entity.HasIndex(e => new { e.UserId, e.ShareId }).IsUnique();
            entity.HasIndex(e => new { e.UserId, e.IsActive });

            entity.HasOne<SharedProject>()
                .WithMany()
                .HasForeignKey(e => e.ShareId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ViewerComment>(entity =>
        {
            entity.ToTable("viewer_comments");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
            entity.Property(e => e.CommentText).IsRequired().HasMaxLength(4000);
            entity.Property(e => e.CommentDate).IsRequired();

            entity.HasIndex(e => new { e.ProjectId, e.CommentDate });

            entity.HasOne<Project>()
                .WithMany()
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}


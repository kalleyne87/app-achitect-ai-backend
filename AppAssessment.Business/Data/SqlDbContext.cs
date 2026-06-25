using AppAssessment.DomainObjects.DBOs;
using Microsoft.EntityFrameworkCore;

namespace AppAssessment.Business.Data;

public class SqlDbContext : DbContext
{
    public SqlDbContext(DbContextOptions<SqlDbContext> options) : base(options) { }
    
    public DbSet<Assessment> Assessments { get; set; }
    public DbSet<AssessmentSession> AssessmentSessions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Assessment ────────────────────────────────────────────────
        modelBuilder.Entity<Assessment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Requirements).IsRequired();
            entity.Property(e => e.ExecutiveSummary).IsRequired();
            entity.Property(e => e.RecommendedServicesJson).HasDefaultValue("[]");
            entity.Property(e => e.RisksJson).HasDefaultValue("[]");
            entity.Property(e => e.TradeoffsJson).HasDefaultValue("[]");
            entity.Property(e => e.RoadmapJson).HasDefaultValue("[]");
            entity.Property(e => e.CreatedDateTime).IsRequired();
        });

        // ── AssessmentSession ─────────────────────────────────────────
        modelBuilder.Entity<AssessmentSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OriginalRequest).IsRequired();
            entity.Property(e => e.CollectedQuestionsAndAnswersJson).HasDefaultValue("[]");
            entity.Property(e => e.CurrentQuestionsJson).HasDefaultValue("[]");
            entity.Property(e => e.Status).HasDefaultValue("NeedsMoreInformation");
            entity.Property(e => e.CreatedDateTime).IsRequired();
            entity.Property(e => e.UpdatedDateTime).IsRequired(false);
        });
    }
}
using CloudMigrate.DomainObjects.DBOs;
using Microsoft.EntityFrameworkCore;

namespace CloudMigrate.Business.Data;

public class SqlDbContext : DbContext
{
    public SqlDbContext(DbContextOptions<SqlDbContext> options) : base(options)
    {
    }

    public DbSet<Assessment> Assessments { get; set; }
}

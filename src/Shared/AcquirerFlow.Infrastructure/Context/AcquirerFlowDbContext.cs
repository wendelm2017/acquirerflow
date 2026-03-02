using AcquirerFlow.Authorization.Domain.Entities;
using AcquirerFlow.Capture.Domain.Entities;
using AcquirerFlow.Settlement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AcquirerFlow.Infrastructure.Context;

public class AcquirerFlowDbContext : DbContext
{
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<CapturedTransaction> CapturedTransactions => Set<CapturedTransaction>();
    public DbSet<SettlementBatch> SettlementBatches => Set<SettlementBatch>();

    public AcquirerFlowDbContext(DbContextOptions<AcquirerFlowDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AcquirerFlowDbContext).Assembly);
    }
}

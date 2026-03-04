#!/bin/bash
# =======================================================
# AcquirerFlow - Reconciliation Service - MASTER SETUP
# =======================================================
# Execute este script inteiro no terminal do Codespace.
# Ele cria: Domain, Application, Worker/API, EF Config,
#           Migration, Testes Unitários, e roda tudo.
# =======================================================
set -e

echo "╔══════════════════════════════════════════════╗"
echo "║  RECONCILIATION SERVICE - Setup Completo     ║"
echo "╚══════════════════════════════════════════════╝"

# ==========================
# STEP 1: Criar projetos
# ==========================
echo ""
echo "▶ [1/9] Criando projetos..."

dotnet new classlib -n AcquirerFlow.Reconciliation.Domain \
  -o src/Services/AcquirerFlow.Reconciliation/AcquirerFlow.Reconciliation.Domain
rm src/Services/AcquirerFlow.Reconciliation/AcquirerFlow.Reconciliation.Domain/Class1.cs

dotnet new classlib -n AcquirerFlow.Reconciliation.Application \
  -o src/Services/AcquirerFlow.Reconciliation/AcquirerFlow.Reconciliation.Application
rm src/Services/AcquirerFlow.Reconciliation/AcquirerFlow.Reconciliation.Application/Class1.cs

dotnet new webapi -n AcquirerFlow.Reconciliation.Worker \
  -o src/Services/AcquirerFlow.Reconciliation/AcquirerFlow.Reconciliation.Worker --no-openapi
rm -f src/Services/AcquirerFlow.Reconciliation/AcquirerFlow.Reconciliation.Worker/Controllers/*.cs 2>/dev/null

dotnet sln src/AcquirerFlow.slnx add \
  src/Services/AcquirerFlow.Reconciliation/AcquirerFlow.Reconciliation.Domain \
  src/Services/AcquirerFlow.Reconciliation/AcquirerFlow.Reconciliation.Application \
  src/Services/AcquirerFlow.Reconciliation/AcquirerFlow.Reconciliation.Worker

dotnet add src/Services/AcquirerFlow.Reconciliation/AcquirerFlow.Reconciliation.Application \
  reference src/Services/AcquirerFlow.Reconciliation/AcquirerFlow.Reconciliation.Domain

dotnet add src/Services/AcquirerFlow.Reconciliation/AcquirerFlow.Reconciliation.Worker reference \
  src/Services/AcquirerFlow.Reconciliation/AcquirerFlow.Reconciliation.Application \
  src/Services/AcquirerFlow.Reconciliation/AcquirerFlow.Reconciliation.Domain \
  src/Shared/AcquirerFlow.Infrastructure \
  src/Shared/AcquirerFlow.Contracts

dotnet add src/Shared/AcquirerFlow.Infrastructure reference \
  src/Services/AcquirerFlow.Reconciliation/AcquirerFlow.Reconciliation.Domain

dotnet add src/Services/AcquirerFlow.Reconciliation/AcquirerFlow.Reconciliation.Worker \
  package Swashbuckle.AspNetCore
dotnet add src/Services/AcquirerFlow.Reconciliation/AcquirerFlow.Reconciliation.Worker \
  package Microsoft.EntityFrameworkCore.SqlServer

echo "✓ Projetos criados"

# ==========================
# STEP 2: Domain Layer
# ==========================
echo ""
echo "▶ [2/9] Domain Layer..."

mkdir -p src/Services/AcquirerFlow.Reconciliation/AcquirerFlow.Reconciliation.Domain/{Entities,ValueObjects,Ports/Out}

cat > src/Services/AcquirerFlow.Reconciliation/AcquirerFlow.Reconciliation.Domain/ValueObjects/DiscrepancyType.cs << 'EOF'
namespace AcquirerFlow.Reconciliation.Domain.ValueObjects;

/// <summary>
/// Value Object representing the type of reconciliation discrepancy found.
/// </summary>
public sealed record DiscrepancyType
{
    public string Value { get; init; }

    private DiscrepancyType() { Value = string.Empty; }
    private DiscrepancyType(string value) => Value = value;

    public static DiscrepancyType MissingCapture => new("MISSING_CAPTURE");
    public static DiscrepancyType MissingSettlement => new("MISSING_SETTLEMENT");
    public static DiscrepancyType AmountMismatch => new("AMOUNT_MISMATCH");
    public static DiscrepancyType OrphanCapture => new("ORPHAN_CAPTURE");
    public static DiscrepancyType OrphanSettlement => new("ORPHAN_SETTLEMENT");
    public static DiscrepancyType DeclinedButCaptured => new("DECLINED_BUT_CAPTURED");

    public override string ToString() => Value;
}
EOF

cat > src/Services/AcquirerFlow.Reconciliation/AcquirerFlow.Reconciliation.Domain/Entities/ReconciliationEntry.cs << 'EOF'
using AcquirerFlow.Reconciliation.Domain.ValueObjects;

namespace AcquirerFlow.Reconciliation.Domain.Entities;

public sealed class ReconciliationEntry
{
    public Guid Id { get; private set; }
    public Guid ReportId { get; private set; }
    public Guid? TransactionId { get; private set; }
    public Guid? CaptureId { get; private set; }
    public Guid? SettlementBatchId { get; private set; }
    public Guid MerchantId { get; private set; }
    public DiscrepancyType Discrepancy { get; private set; } = null!;
    public decimal? ExpectedAmount { get; private set; }
    public decimal? ActualAmount { get; private set; }
    public string Details { get; private set; } = string.Empty;
    public DateTime DetectedAt { get; private set; }

    private ReconciliationEntry() { } // EF Core

    public static ReconciliationEntry Create(
        Guid reportId, Guid merchantId, DiscrepancyType discrepancy, string details,
        Guid? transactionId = null, Guid? captureId = null, Guid? settlementBatchId = null,
        decimal? expectedAmount = null, decimal? actualAmount = null)
    {
        return new ReconciliationEntry
        {
            Id = Guid.NewGuid(),
            ReportId = reportId,
            MerchantId = merchantId,
            Discrepancy = discrepancy,
            Details = details,
            TransactionId = transactionId,
            CaptureId = captureId,
            SettlementBatchId = settlementBatchId,
            ExpectedAmount = expectedAmount,
            ActualAmount = actualAmount,
            DetectedAt = DateTime.UtcNow
        };
    }
}
EOF

cat > src/Services/AcquirerFlow.Reconciliation/AcquirerFlow.Reconciliation.Domain/Entities/ReconciliationReport.cs << 'EOF'
namespace AcquirerFlow.Reconciliation.Domain.Entities;

public sealed class ReconciliationReport
{
    public Guid Id { get; private set; }
    public DateTime ReferenceDate { get; private set; }
    public string Status { get; private set; } = string.Empty;
    public int TotalTransactions { get; private set; }
    public int TotalCaptures { get; private set; }
    public int TotalSettlements { get; private set; }
    public int DiscrepancyCount { get; private set; }
    public decimal TotalAuthorizedAmount { get; private set; }
    public decimal TotalCapturedAmount { get; private set; }
    public decimal TotalSettledGrossAmount { get; private set; }
    public decimal TotalSettledNetAmount { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    private readonly List<ReconciliationEntry> _entries = new();
    public IReadOnlyList<ReconciliationEntry> Entries => _entries.AsReadOnly();

    private ReconciliationReport() { } // EF Core

    public static ReconciliationReport Create(DateTime referenceDate)
    {
        return new ReconciliationReport
        {
            Id = Guid.NewGuid(),
            ReferenceDate = referenceDate,
            Status = "RUNNING",
            CreatedAt = DateTime.UtcNow
        };
    }

    public void SetTotals(
        int totalTransactions, int totalCaptures, int totalSettlements,
        decimal totalAuthorizedAmount, decimal totalCapturedAmount,
        decimal totalSettledGrossAmount, decimal totalSettledNetAmount)
    {
        TotalTransactions = totalTransactions;
        TotalCaptures = totalCaptures;
        TotalSettlements = totalSettlements;
        TotalAuthorizedAmount = totalAuthorizedAmount;
        TotalCapturedAmount = totalCapturedAmount;
        TotalSettledGrossAmount = totalSettledGrossAmount;
        TotalSettledNetAmount = totalSettledNetAmount;
    }

    public void AddEntry(ReconciliationEntry entry)
    {
        if (Status != "RUNNING")
            throw new InvalidOperationException("Cannot add entries to a completed report");
        _entries.Add(entry);
    }

    public void Complete()
    {
        DiscrepancyCount = _entries.Count;
        Status = _entries.Count == 0 ? "RECONCILED" : "DISCREPANCIES_FOUND";
        CompletedAt = DateTime.UtcNow;
    }
}
EOF

cat > src/Services/AcquirerFlow.Reconciliation/AcquirerFlow.Reconciliation.Domain/Ports/Out/IReconciliationRepository.cs << 'EOF'
using AcquirerFlow.Reconciliation.Domain.Entities;

namespace AcquirerFlow.Reconciliation.Domain.Ports.Out;

public interface IReconciliationRepository
{
    Task SaveAsync(ReconciliationReport report);
    Task<ReconciliationReport?> GetByIdAsync(Guid id);
    Task<List<ReconciliationReport>> GetAllAsync(int page = 1, int size = 20);
    Task<int> CountAsync();
}
EOF

cat > src/Services/AcquirerFlow.Reconciliation/AcquirerFlow.Reconciliation.Domain/Ports/Out/ITransactionDataSource.cs << 'EOF'
namespace AcquirerFlow.Reconciliation.Domain.Ports.Out;

public interface ITransactionDataSource
{
    Task<List<TransactionSnapshot>> GetAuthorizedTransactionsAsync(DateTime referenceDate);
    Task<List<CaptureSnapshot>> GetCapturesAsync(DateTime referenceDate);
    Task<List<SettlementSnapshot>> GetSettlementBatchesAsync(DateTime referenceDate);
}

public record TransactionSnapshot(
    Guid Id, Guid MerchantId, string Status, decimal Amount, string Currency, DateTime CreatedAt);

public record CaptureSnapshot(
    Guid Id, Guid OriginalTransactionId, Guid MerchantId, decimal CapturedAmount, string Currency, DateTime CapturedAt);

public record SettlementSnapshot(
    Guid Id, Guid MerchantId, int TransactionCount, decimal GrossAmount, decimal NetAmount, string Status, DateTime ReferenceDate);
EOF

echo "✓ Domain layer"

# ==========================
# STEP 3: Application Layer
# ==========================
echo ""
echo "▶ [3/9] Application Layer..."

mkdir -p src/Services/AcquirerFlow.Reconciliation/AcquirerFlow.Reconciliation.Application/{Services,DTOs}

cat > src/Services/AcquirerFlow.Reconciliation/AcquirerFlow.Reconciliation.Application/DTOs/ReconciliationDtos.cs << 'EOF'
namespace AcquirerFlow.Reconciliation.Application.DTOs;

public record ReconciliationReportDto(
    Guid Id, DateTime ReferenceDate, string Status,
    int TotalTransactions, int TotalCaptures, int TotalSettlements, int DiscrepancyCount,
    decimal TotalAuthorizedAmount, decimal TotalCapturedAmount,
    decimal TotalSettledGrossAmount, decimal TotalSettledNetAmount,
    DateTime CreatedAt, DateTime? CompletedAt,
    List<ReconciliationEntryDto> Entries);

public record ReconciliationEntryDto(
    Guid Id, Guid? TransactionId, Guid? CaptureId, Guid? SettlementBatchId,
    Guid MerchantId, string Discrepancy,
    decimal? ExpectedAmount, decimal? ActualAmount,
    string Details, DateTime DetectedAt);

public record ReconciliationSummaryDto(
    Guid Id, DateTime ReferenceDate, string Status,
    int TotalTransactions, int TotalCaptures, int TotalSettlements,
    int DiscrepancyCount, DateTime CreatedAt);
EOF

cat > src/Services/AcquirerFlow.Reconciliation/AcquirerFlow.Reconciliation.Application/Services/ReconciliationService.cs << 'EOF'
using AcquirerFlow.Reconciliation.Application.DTOs;
using AcquirerFlow.Reconciliation.Domain.Entities;
using AcquirerFlow.Reconciliation.Domain.Ports.Out;
using AcquirerFlow.Reconciliation.Domain.ValueObjects;

namespace AcquirerFlow.Reconciliation.Application.Services;

public class ReconciliationService
{
    private readonly IReconciliationRepository _repository;
    private readonly ITransactionDataSource _dataSource;

    public ReconciliationService(IReconciliationRepository repository, ITransactionDataSource dataSource)
    {
        _repository = repository;
        _dataSource = dataSource;
    }

    public async Task<ReconciliationReportDto> RunReconciliationAsync(DateTime? referenceDate = null)
    {
        var refDate = referenceDate ?? DateTime.UtcNow.Date;
        var report = ReconciliationReport.Create(refDate);

        // Load snapshots from all 3 data sources
        var transactions = await _dataSource.GetAuthorizedTransactionsAsync(refDate);
        var captures = await _dataSource.GetCapturesAsync(refDate);
        var settlements = await _dataSource.GetSettlementBatchesAsync(refDate);

        // Lookup structures
        var capturesByTxId = captures.ToDictionary(c => c.OriginalTransactionId, c => c);
        var capturedTxIds = new HashSet<Guid>(captures.Select(c => c.OriginalTransactionId));
        var txById = transactions.ToDictionary(t => t.Id, t => t);

        // Set totals
        report.SetTotals(
            transactions.Count, captures.Count, settlements.Count,
            transactions.Where(t => t.Status == "AUTHORIZED").Sum(t => t.Amount),
            captures.Sum(c => c.CapturedAmount),
            settlements.Sum(s => s.GrossAmount),
            settlements.Sum(s => s.NetAmount));

        // CHECK 1: Authorized but not captured
        foreach (var tx in transactions.Where(t => t.Status == "AUTHORIZED"))
        {
            if (!capturedTxIds.Contains(tx.Id))
            {
                report.AddEntry(ReconciliationEntry.Create(
                    report.Id, tx.MerchantId, DiscrepancyType.MissingCapture,
                    $"Transaction {tx.Id} authorized for {tx.Currency} {tx.Amount:N2} but not captured",
                    transactionId: tx.Id, expectedAmount: tx.Amount));
            }
        }

        // CHECK 2: Amount mismatch authorization vs capture
        foreach (var capture in captures)
        {
            if (txById.TryGetValue(capture.OriginalTransactionId, out var tx) && tx.Amount != capture.CapturedAmount)
            {
                report.AddEntry(ReconciliationEntry.Create(
                    report.Id, capture.MerchantId, DiscrepancyType.AmountMismatch,
                    $"Tx {tx.Id}: authorized {tx.Currency} {tx.Amount:N2}, captured {capture.CapturedAmount:N2}",
                    transactionId: tx.Id, captureId: capture.Id,
                    expectedAmount: tx.Amount, actualAmount: capture.CapturedAmount));
            }
        }

        // CHECK 3: Orphan captures (no matching authorization)
        foreach (var capture in captures)
        {
            if (!txById.ContainsKey(capture.OriginalTransactionId))
            {
                report.AddEntry(ReconciliationEntry.Create(
                    report.Id, capture.MerchantId, DiscrepancyType.OrphanCapture,
                    $"Capture {capture.Id} references missing transaction {capture.OriginalTransactionId}",
                    captureId: capture.Id, actualAmount: capture.CapturedAmount));
            }
        }

        // CHECK 4: Declined but captured
        foreach (var tx in transactions.Where(t => t.Status == "DECLINED"))
        {
            if (capturedTxIds.Contains(tx.Id))
            {
                report.AddEntry(ReconciliationEntry.Create(
                    report.Id, tx.MerchantId, DiscrepancyType.DeclinedButCaptured,
                    $"Transaction {tx.Id} was DECLINED but appears in captures",
                    transactionId: tx.Id, captureId: capturesByTxId[tx.Id].Id));
            }
        }

        // CHECK 5: Captured total vs settled gross total per merchant
        var capturedByMerchant = captures.GroupBy(c => c.MerchantId)
            .ToDictionary(g => g.Key, g => g.Sum(c => c.CapturedAmount));
        var settledByMerchant = settlements.GroupBy(s => s.MerchantId)
            .ToDictionary(g => g.Key, g => g.Sum(s => s.GrossAmount));

        foreach (var (merchantId, capturedTotal) in capturedByMerchant)
        {
            settledByMerchant.TryGetValue(merchantId, out var settledTotal);
            if (Math.Abs(capturedTotal - settledTotal) > 0.01m)
            {
                report.AddEntry(ReconciliationEntry.Create(
                    report.Id, merchantId, DiscrepancyType.MissingSettlement,
                    $"Merchant {merchantId}: captured {capturedTotal:N2} vs settled {settledTotal:N2} (diff: {capturedTotal - settledTotal:N2})",
                    expectedAmount: capturedTotal, actualAmount: settledTotal));
            }
        }

        report.Complete();
        await _repository.SaveAsync(report);
        return MapToDto(report);
    }

    public async Task<ReconciliationReportDto?> GetReportAsync(Guid id)
    {
        var report = await _repository.GetByIdAsync(id);
        return report is null ? null : MapToDto(report);
    }

    public async Task<object> GetReportsAsync(int page = 1, int size = 20)
    {
        var reports = await _repository.GetAllAsync(page, size);
        var total = await _repository.CountAsync();
        return new
        {
            total, page, size,
            items = reports.Select(r => new ReconciliationSummaryDto(
                r.Id, r.ReferenceDate, r.Status,
                r.TotalTransactions, r.TotalCaptures, r.TotalSettlements,
                r.DiscrepancyCount, r.CreatedAt)).ToList()
        };
    }

    private static ReconciliationReportDto MapToDto(ReconciliationReport r) =>
        new(r.Id, r.ReferenceDate, r.Status,
            r.TotalTransactions, r.TotalCaptures, r.TotalSettlements, r.DiscrepancyCount,
            r.TotalAuthorizedAmount, r.TotalCapturedAmount,
            r.TotalSettledGrossAmount, r.TotalSettledNetAmount,
            r.CreatedAt, r.CompletedAt,
            r.Entries.Select(e => new ReconciliationEntryDto(
                e.Id, e.TransactionId, e.CaptureId, e.SettlementBatchId,
                e.MerchantId, e.Discrepancy.Value,
                e.ExpectedAmount, e.ActualAmount,
                e.Details, e.DetectedAt)).ToList());
}
EOF

echo "✓ Application layer"

# ==========================
# STEP 4: Infrastructure
# ==========================
echo ""
echo "▶ [4/9] Infrastructure (EF Config + Repositories)..."

cat > src/Shared/AcquirerFlow.Infrastructure/Configurations/ReconciliationReportConfiguration.cs << 'EOF'
using AcquirerFlow.Reconciliation.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcquirerFlow.Infrastructure.Configurations;

public class ReconciliationReportConfiguration : IEntityTypeConfiguration<ReconciliationReport>
{
    public void Configure(EntityTypeBuilder<ReconciliationReport> builder)
    {
        builder.ToTable("ReconciliationReports");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.ReferenceDate);
        builder.Property(r => r.Status).HasMaxLength(30);
        builder.Property(r => r.TotalTransactions);
        builder.Property(r => r.TotalCaptures);
        builder.Property(r => r.TotalSettlements);
        builder.Property(r => r.DiscrepancyCount);
        builder.Property(r => r.TotalAuthorizedAmount).HasPrecision(18, 2);
        builder.Property(r => r.TotalCapturedAmount).HasPrecision(18, 2);
        builder.Property(r => r.TotalSettledGrossAmount).HasPrecision(18, 2);
        builder.Property(r => r.TotalSettledNetAmount).HasPrecision(18, 2);
        builder.Property(r => r.CreatedAt);
        builder.Property(r => r.CompletedAt);

        builder.HasIndex(r => r.ReferenceDate);

        builder.HasMany(r => r.Entries)
            .WithOne()
            .HasForeignKey(e => e.ReportId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(ReconciliationReport.Entries))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
EOF

cat > src/Shared/AcquirerFlow.Infrastructure/Configurations/ReconciliationEntryConfiguration.cs << 'EOF'
using AcquirerFlow.Reconciliation.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcquirerFlow.Infrastructure.Configurations;

public class ReconciliationEntryConfiguration : IEntityTypeConfiguration<ReconciliationEntry>
{
    public void Configure(EntityTypeBuilder<ReconciliationEntry> builder)
    {
        builder.ToTable("ReconciliationEntries");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.ReportId);
        builder.Property(e => e.TransactionId);
        builder.Property(e => e.CaptureId);
        builder.Property(e => e.SettlementBatchId);
        builder.Property(e => e.MerchantId);
        builder.Property(e => e.Details).HasMaxLength(500);
        builder.Property(e => e.ExpectedAmount).HasPrecision(18, 2);
        builder.Property(e => e.ActualAmount).HasPrecision(18, 2);
        builder.Property(e => e.DetectedAt);

        builder.OwnsOne(e => e.Discrepancy, d =>
        {
            d.Property(x => x.Value).HasColumnName("DiscrepancyType").HasMaxLength(50).IsRequired();
        });

        builder.HasIndex(e => e.ReportId);
        builder.HasIndex(e => e.MerchantId);
    }
}
EOF

cat > src/Shared/AcquirerFlow.Infrastructure/Repositories/EfReconciliationRepository.cs << 'EOF'
using AcquirerFlow.Infrastructure.Context;
using AcquirerFlow.Reconciliation.Domain.Entities;
using AcquirerFlow.Reconciliation.Domain.Ports.Out;
using Microsoft.EntityFrameworkCore;

namespace AcquirerFlow.Infrastructure.Repositories;

public class EfReconciliationRepository : IReconciliationRepository
{
    private readonly AcquirerFlowDbContext _context;

    public EfReconciliationRepository(AcquirerFlowDbContext context) => _context = context;

    public async Task SaveAsync(ReconciliationReport report)
    {
        _context.ReconciliationReports.Add(report);
        await _context.SaveChangesAsync();
    }

    public async Task<ReconciliationReport?> GetByIdAsync(Guid id) =>
        await _context.ReconciliationReports
            .Include(r => r.Entries)
            .FirstOrDefaultAsync(r => r.Id == id);

    public async Task<List<ReconciliationReport>> GetAllAsync(int page = 1, int size = 20) =>
        await _context.ReconciliationReports
            .AsNoTracking()
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync();

    public async Task<int> CountAsync() => await _context.ReconciliationReports.CountAsync();
}
EOF

cat > src/Shared/AcquirerFlow.Infrastructure/Repositories/EfTransactionDataSource.cs << 'EOF'
using AcquirerFlow.Infrastructure.Context;
using AcquirerFlow.Reconciliation.Domain.Ports.Out;
using Microsoft.EntityFrameworkCore;

namespace AcquirerFlow.Infrastructure.Repositories;

public class EfTransactionDataSource : ITransactionDataSource
{
    private readonly AcquirerFlowDbContext _context;

    public EfTransactionDataSource(AcquirerFlowDbContext context) => _context = context;

    public async Task<List<TransactionSnapshot>> GetAuthorizedTransactionsAsync(DateTime referenceDate) =>
        await _context.Transactions
            .AsNoTracking()
            .Where(t => t.CreatedAt.Date <= referenceDate.Date)
            .Select(t => new TransactionSnapshot(
                t.Id, t.MerchantId, t.Status.Value,
                t.Amount.Value, t.Amount.Currency, t.CreatedAt))
            .ToListAsync();

    public async Task<List<CaptureSnapshot>> GetCapturesAsync(DateTime referenceDate) =>
        await _context.CapturedTransactions
            .AsNoTracking()
            .Where(c => c.CapturedAt.Date <= referenceDate.Date)
            .Select(c => new CaptureSnapshot(
                c.Id, c.OriginalTransactionId, c.MerchantId,
                c.CapturedAmount, c.Currency, c.CapturedAt))
            .ToListAsync();

    public async Task<List<SettlementSnapshot>> GetSettlementBatchesAsync(DateTime referenceDate) =>
        await _context.SettlementBatches
            .AsNoTracking()
            .Where(s => s.ReferenceDate.Date <= referenceDate.Date)
            .Select(s => new SettlementSnapshot(
                s.Id, s.MerchantId, s.TransactionCount,
                s.Fees.GrossAmount, s.Fees.NetAmount,
                s.Status, s.ReferenceDate))
            .ToListAsync();
}
EOF

echo "✓ Infrastructure"

# ==========================
# STEP 5: Update DbContext + DI
# ==========================
echo ""
echo "▶ [5/9] Updating DbContext + DI..."

cat > src/Shared/AcquirerFlow.Infrastructure/Context/AcquirerFlowDbContext.cs << 'EOF'
using AcquirerFlow.Authorization.Domain.Entities;
using AcquirerFlow.Capture.Domain.Entities;
using AcquirerFlow.Reconciliation.Domain.Entities;
using AcquirerFlow.Settlement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AcquirerFlow.Infrastructure.Context;

public class AcquirerFlowDbContext : DbContext
{
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<CapturedTransaction> CapturedTransactions => Set<CapturedTransaction>();
    public DbSet<SettlementBatch> SettlementBatches => Set<SettlementBatch>();
    public DbSet<ReconciliationReport> ReconciliationReports => Set<ReconciliationReport>();
    public DbSet<ReconciliationEntry> ReconciliationEntries => Set<ReconciliationEntry>();

    public AcquirerFlowDbContext(DbContextOptions<AcquirerFlowDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AcquirerFlowDbContext).Assembly);
    }
}
EOF

cat > src/Shared/AcquirerFlow.Infrastructure/InfrastructureServiceExtensions.cs << 'EOF'
using AcquirerFlow.Authorization.Domain.Ports.Out;
using AcquirerFlow.Capture.Domain.Ports.Out;
using AcquirerFlow.Infrastructure.Context;
using AcquirerFlow.Infrastructure.Repositories;
using AcquirerFlow.Reconciliation.Domain.Ports.Out;
using AcquirerFlow.Settlement.Domain.Ports.Out;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AcquirerFlow.Infrastructure;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddAcquirerFlowInfrastructure(
        this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AcquirerFlowDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<ITransactionRepository, EfTransactionRepository>();
        services.AddScoped<ICaptureRepository, EfCaptureRepository>();
        services.AddScoped<ISettlementRepository, EfSettlementRepository>();
        services.AddScoped<IReconciliationRepository, EfReconciliationRepository>();
        services.AddScoped<ITransactionDataSource, EfTransactionDataSource>();

        return services;
    }
}
EOF

echo "✓ DbContext + DI"

# ==========================
# STEP 6: Worker (API + Background)
# ==========================
echo ""
echo "▶ [6/9] Worker (API + Background)..."

mkdir -p src/Services/AcquirerFlow.Reconciliation/AcquirerFlow.Reconciliation.Worker/{Controllers,Properties}

cat > src/Services/AcquirerFlow.Reconciliation/AcquirerFlow.Reconciliation.Worker/Controllers/ReconciliationController.cs << 'EOF'
using AcquirerFlow.Reconciliation.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace AcquirerFlow.Reconciliation.Worker.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReconciliationController : ControllerBase
{
    private readonly ReconciliationService _service;

    public ReconciliationController(ReconciliationService service) => _service = service;

    [HttpPost("run")]
    public async Task<IActionResult> RunReconciliation([FromQuery] DateTime? referenceDate)
    {
        var report = await _service.RunReconciliationAsync(referenceDate);
        return CreatedAtAction(nameof(GetReport), new { id = report.Id }, report);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetReport(Guid id)
    {
        var report = await _service.GetReportAsync(id);
        return report is null ? NotFound() : Ok(report);
    }

    [HttpGet]
    public async Task<IActionResult> GetReports([FromQuery] int page = 1, [FromQuery] int size = 20)
    {
        return Ok(await _service.GetReportsAsync(page, size));
    }
}
EOF

cat > src/Services/AcquirerFlow.Reconciliation/AcquirerFlow.Reconciliation.Worker/ReconciliationBackgroundWorker.cs << 'EOF'
using AcquirerFlow.Reconciliation.Application.Services;

namespace AcquirerFlow.Reconciliation.Worker;

public class ReconciliationBackgroundWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReconciliationBackgroundWorker> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);

    public ReconciliationBackgroundWorker(IServiceScopeFactory scopeFactory, ILogger<ReconciliationBackgroundWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[RECONCILIATION] Worker started. Running every {Interval}.", _interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<ReconciliationService>();
                var report = await service.RunReconciliationAsync();

                if (report.DiscrepancyCount > 0)
                    _logger.LogWarning("[RECONCILIATION] {Count} discrepancies found for {Date}. Report: {Id}",
                        report.DiscrepancyCount, report.ReferenceDate, report.Id);
                else
                    _logger.LogInformation("[RECONCILIATION] All reconciled for {Date}. Tx:{Tx} Cap:{Cap} Stl:{Stl}",
                        report.ReferenceDate, report.TotalTransactions, report.TotalCaptures, report.TotalSettlements);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RECONCILIATION] Error during reconciliation run");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
}
EOF

cat > src/Services/AcquirerFlow.Reconciliation/AcquirerFlow.Reconciliation.Worker/Program.cs << 'EOF'
using AcquirerFlow.Infrastructure;
using AcquirerFlow.Reconciliation.Application.Services;
using AcquirerFlow.Reconciliation.Worker;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "AcquirerFlow Reconciliation API",
        Version = "v1",
        Description = "Reconciliation Service - Cross-references Authorization, Capture, and Settlement data",
        Contact = new() { Name = "Wendel Machado", Email = "wendelm2017@gmail.com" }
    });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=localhost,1433;Database=AcquirerFlowDb;User Id=sa;Password=AcquirerFlow@2024;TrustServerCertificate=True";
builder.Services.AddAcquirerFlowInfrastructure(connectionString);
builder.Services.AddScoped<ReconciliationService>();
builder.Services.AddHostedService<ReconciliationBackgroundWorker>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "AcquirerFlow Reconciliation v1");
    c.RoutePrefix = string.Empty;
});
app.MapControllers();
app.Run();
EOF

cat > src/Services/AcquirerFlow.Reconciliation/AcquirerFlow.Reconciliation.Worker/Properties/launchSettings.json << 'EOF'
{
  "profiles": {
    "AcquirerFlow.Reconciliation.Worker": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": false,
      "applicationUrl": "http://localhost:5222",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
EOF

echo "✓ Worker"

# ==========================
# STEP 7: Build
# ==========================
echo ""
echo "▶ [7/9] Building..."
dotnet build src/AcquirerFlow.slnx 2>&1 | tail -5

# ==========================
# STEP 8: Migration
# ==========================
echo ""
echo "▶ [8/9] Creating migration..."
dotnet ef migrations add AddReconciliation \
  --project src/Shared/AcquirerFlow.Infrastructure \
  --startup-project src/Services/AcquirerFlow.Authorization/AcquirerFlow.Authorization.Adapters.Driving \
  2>&1 | tail -5

dotnet ef database update \
  --project src/Shared/AcquirerFlow.Infrastructure \
  --startup-project src/Services/AcquirerFlow.Authorization/AcquirerFlow.Authorization.Adapters.Driving \
  2>&1 | tail -5

echo ""
echo "=== SQL Server Tables ==="
docker exec acquirerflow-sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "AcquirerFlow@2024" -C -d AcquirerFlowDb \
  -Q "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE' ORDER BY TABLE_NAME"

echo "✓ Migration"

# ==========================
# STEP 9: Unit Tests
# ==========================
echo ""
echo "▶ [9/9] Creating and running tests..."

dotnet new xunit -n AcquirerFlow.Reconciliation.UnitTests -o tests/AcquirerFlow.Reconciliation.UnitTests
dotnet sln src/AcquirerFlow.slnx add tests/AcquirerFlow.Reconciliation.UnitTests
dotnet add tests/AcquirerFlow.Reconciliation.UnitTests reference \
  src/Services/AcquirerFlow.Reconciliation/AcquirerFlow.Reconciliation.Domain \
  src/Services/AcquirerFlow.Reconciliation/AcquirerFlow.Reconciliation.Application
dotnet add tests/AcquirerFlow.Reconciliation.UnitTests package FluentAssertions --version 8.8.0
dotnet add tests/AcquirerFlow.Reconciliation.UnitTests package Moq --version 4.20.72
rm tests/AcquirerFlow.Reconciliation.UnitTests/UnitTest1.cs

cat > tests/AcquirerFlow.Reconciliation.UnitTests/ReconciliationReportTests.cs << 'EOF'
using AcquirerFlow.Reconciliation.Domain.Entities;
using AcquirerFlow.Reconciliation.Domain.ValueObjects;
using FluentAssertions;

namespace AcquirerFlow.Reconciliation.UnitTests;

public class ReconciliationReportTests
{
    private readonly DateTime _refDate = new(2026, 3, 2);

    [Fact]
    public void Create_ShouldSetDefaults()
    {
        var report = ReconciliationReport.Create(_refDate);
        report.Id.Should().NotBeEmpty();
        report.ReferenceDate.Should().Be(_refDate);
        report.Status.Should().Be("RUNNING");
        report.Entries.Should().BeEmpty();
    }

    [Fact]
    public void AddEntry_ShouldAccumulate()
    {
        var report = ReconciliationReport.Create(_refDate);
        report.AddEntry(ReconciliationEntry.Create(
            report.Id, Guid.NewGuid(), DiscrepancyType.MissingCapture, "Test"));
        report.Entries.Should().HaveCount(1);
    }

    [Fact]
    public void Complete_NoDiscrepancies_ShouldBeReconciled()
    {
        var report = ReconciliationReport.Create(_refDate);
        report.SetTotals(10, 10, 2, 5000m, 5000m, 5000m, 4875m);
        report.Complete();
        report.Status.Should().Be("RECONCILED");
        report.DiscrepancyCount.Should().Be(0);
        report.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void Complete_WithDiscrepancies_ShouldReportThem()
    {
        var report = ReconciliationReport.Create(_refDate);
        report.AddEntry(ReconciliationEntry.Create(report.Id, Guid.NewGuid(), DiscrepancyType.MissingCapture, "Missing"));
        report.AddEntry(ReconciliationEntry.Create(report.Id, Guid.NewGuid(), DiscrepancyType.AmountMismatch, "Mismatch",
            expectedAmount: 500m, actualAmount: 400m));
        report.Complete();
        report.Status.Should().Be("DISCREPANCIES_FOUND");
        report.DiscrepancyCount.Should().Be(2);
    }

    [Fact]
    public void AddEntry_ToCompletedReport_ShouldThrow()
    {
        var report = ReconciliationReport.Create(_refDate);
        report.Complete();
        var act = () => report.AddEntry(ReconciliationEntry.Create(
            report.Id, Guid.NewGuid(), DiscrepancyType.MissingCapture, "Test"));
        act.Should().Throw<InvalidOperationException>();
    }
}
EOF

cat > tests/AcquirerFlow.Reconciliation.UnitTests/DiscrepancyTypeTests.cs << 'EOF'
using AcquirerFlow.Reconciliation.Domain.ValueObjects;
using FluentAssertions;

namespace AcquirerFlow.Reconciliation.UnitTests;

public class DiscrepancyTypeTests
{
    [Fact]
    public void AllTypes_ShouldBeDistinct()
    {
        var types = new[]
        {
            DiscrepancyType.MissingCapture, DiscrepancyType.MissingSettlement,
            DiscrepancyType.AmountMismatch, DiscrepancyType.OrphanCapture,
            DiscrepancyType.OrphanSettlement, DiscrepancyType.DeclinedButCaptured
        };
        types.Select(t => t.Value).Distinct().Count().Should().Be(6);
    }

    [Fact]
    public void SameType_ShouldBeEqual()
    {
        DiscrepancyType.MissingCapture.Should().Be(DiscrepancyType.MissingCapture);
    }

    [Fact]
    public void DifferentTypes_ShouldNotBeEqual()
    {
        DiscrepancyType.MissingCapture.Should().NotBe(DiscrepancyType.AmountMismatch);
    }
}
EOF

cat > tests/AcquirerFlow.Reconciliation.UnitTests/ReconciliationServiceTests.cs << 'EOF'
using AcquirerFlow.Reconciliation.Application.Services;
using AcquirerFlow.Reconciliation.Domain.Entities;
using AcquirerFlow.Reconciliation.Domain.Ports.Out;
using FluentAssertions;
using Moq;

namespace AcquirerFlow.Reconciliation.UnitTests;

public class ReconciliationServiceTests
{
    private readonly Mock<IReconciliationRepository> _repoMock = new();
    private readonly Mock<ITransactionDataSource> _dataSourceMock = new();
    private readonly ReconciliationService _service;
    private readonly DateTime _refDate = new(2026, 3, 2);
    private readonly Guid _merchant1 = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");

    public ReconciliationServiceTests()
    {
        _service = new ReconciliationService(_repoMock.Object, _dataSourceMock.Object);
    }

    private void Setup(TransactionSnapshot[]? txs = null, CaptureSnapshot[]? caps = null, SettlementSnapshot[]? stls = null)
    {
        _dataSourceMock.Setup(d => d.GetAuthorizedTransactionsAsync(It.IsAny<DateTime>()))
            .ReturnsAsync((txs ?? Array.Empty<TransactionSnapshot>()).ToList());
        _dataSourceMock.Setup(d => d.GetCapturesAsync(It.IsAny<DateTime>()))
            .ReturnsAsync((caps ?? Array.Empty<CaptureSnapshot>()).ToList());
        _dataSourceMock.Setup(d => d.GetSettlementBatchesAsync(It.IsAny<DateTime>()))
            .ReturnsAsync((stls ?? Array.Empty<SettlementSnapshot>()).ToList());
    }

    [Fact]
    public async Task Run_AllReconciled_ShouldReturnZeroDiscrepancies()
    {
        var txId = Guid.NewGuid();
        Setup(
            txs: [new(txId, _merchant1, "AUTHORIZED", 1000m, "BRL", _refDate)],
            caps: [new(Guid.NewGuid(), txId, _merchant1, 1000m, "BRL", _refDate)],
            stls: [new(Guid.NewGuid(), _merchant1, 1, 1000m, 975m, "PROCESSED", _refDate)]);

        var report = await _service.RunReconciliationAsync(_refDate);
        report.Status.Should().Be("RECONCILED");
        report.DiscrepancyCount.Should().Be(0);
        _repoMock.Verify(r => r.SaveAsync(It.IsAny<ReconciliationReport>()), Times.Once);
    }

    [Fact]
    public async Task Run_MissingCapture_ShouldDetect()
    {
        var txId = Guid.NewGuid();
        Setup(txs: [new(txId, _merchant1, "AUTHORIZED", 500m, "BRL", _refDate)]);

        var report = await _service.RunReconciliationAsync(_refDate);
        report.Status.Should().Be("DISCREPANCIES_FOUND");
        report.Entries.Should().ContainSingle(e => e.Discrepancy == "MISSING_CAPTURE");
    }

    [Fact]
    public async Task Run_AmountMismatch_ShouldDetect()
    {
        var txId = Guid.NewGuid();
        Setup(
            txs: [new(txId, _merchant1, "AUTHORIZED", 1000m, "BRL", _refDate)],
            caps: [new(Guid.NewGuid(), txId, _merchant1, 900m, "BRL", _refDate)],
            stls: [new(Guid.NewGuid(), _merchant1, 1, 900m, 877.5m, "PROCESSED", _refDate)]);

        var report = await _service.RunReconciliationAsync(_refDate);
        report.Entries.Should().Contain(e => e.Discrepancy == "AMOUNT_MISMATCH");
        var entry = report.Entries.First(e => e.Discrepancy == "AMOUNT_MISMATCH");
        entry.ExpectedAmount.Should().Be(1000m);
        entry.ActualAmount.Should().Be(900m);
    }

    [Fact]
    public async Task Run_OrphanCapture_ShouldDetect()
    {
        Setup(caps: [new(Guid.NewGuid(), Guid.NewGuid(), _merchant1, 500m, "BRL", _refDate)]);

        var report = await _service.RunReconciliationAsync(_refDate);
        report.Entries.Should().Contain(e => e.Discrepancy == "ORPHAN_CAPTURE");
    }

    [Fact]
    public async Task Run_DeclinedButCaptured_ShouldDetect()
    {
        var txId = Guid.NewGuid();
        Setup(
            txs: [new(txId, _merchant1, "DECLINED", 500m, "BRL", _refDate)],
            caps: [new(Guid.NewGuid(), txId, _merchant1, 500m, "BRL", _refDate)]);

        var report = await _service.RunReconciliationAsync(_refDate);
        report.Entries.Should().Contain(e => e.Discrepancy == "DECLINED_BUT_CAPTURED");
    }

    [Fact]
    public async Task Run_SettlementMismatch_ShouldDetect()
    {
        var tx1 = Guid.NewGuid();
        var tx2 = Guid.NewGuid();
        Setup(
            txs: [new(tx1, _merchant1, "AUTHORIZED", 1000m, "BRL", _refDate),
                  new(tx2, _merchant1, "AUTHORIZED", 2000m, "BRL", _refDate)],
            caps: [new(Guid.NewGuid(), tx1, _merchant1, 1000m, "BRL", _refDate),
                   new(Guid.NewGuid(), tx2, _merchant1, 2000m, "BRL", _refDate)],
            stls: [new(Guid.NewGuid(), _merchant1, 2, 2500m, 2437.5m, "PROCESSED", _refDate)]);

        var report = await _service.RunReconciliationAsync(_refDate);
        report.Entries.Should().Contain(e => e.Discrepancy == "MISSING_SETTLEMENT");
        var entry = report.Entries.First(e => e.Discrepancy == "MISSING_SETTLEMENT");
        entry.ExpectedAmount.Should().Be(3000m);
        entry.ActualAmount.Should().Be(2500m);
    }

    [Fact]
    public async Task Run_MultipleDiscrepancies_ShouldDetectAll()
    {
        var tx1 = Guid.NewGuid();
        var tx2 = Guid.NewGuid();
        var tx3 = Guid.NewGuid();
        Setup(
            txs: [new(tx1, _merchant1, "AUTHORIZED", 1000m, "BRL", _refDate),
                  new(tx2, _merchant1, "AUTHORIZED", 500m, "BRL", _refDate),
                  new(tx3, _merchant1, "DECLINED", 300m, "BRL", _refDate)],
            caps: [new(Guid.NewGuid(), tx2, _merchant1, 400m, "BRL", _refDate),
                   new(Guid.NewGuid(), tx3, _merchant1, 300m, "BRL", _refDate),
                   new(Guid.NewGuid(), Guid.NewGuid(), _merchant1, 200m, "BRL", _refDate)]);

        var report = await _service.RunReconciliationAsync(_refDate);
        report.DiscrepancyCount.Should().BeGreaterThanOrEqualTo(4);
        report.Entries.Should().Contain(e => e.Discrepancy == "MISSING_CAPTURE");
        report.Entries.Should().Contain(e => e.Discrepancy == "AMOUNT_MISMATCH");
        report.Entries.Should().Contain(e => e.Discrepancy == "ORPHAN_CAPTURE");
        report.Entries.Should().Contain(e => e.Discrepancy == "DECLINED_BUT_CAPTURED");
    }

    [Fact]
    public async Task Run_EmptyDatabase_ShouldReturnReconciled()
    {
        Setup();
        var report = await _service.RunReconciliationAsync(_refDate);
        report.Status.Should().Be("RECONCILED");
        report.DiscrepancyCount.Should().Be(0);
    }

    [Fact]
    public async Task Run_Totals_ShouldBeCorrect()
    {
        var tx1 = Guid.NewGuid();
        var tx2 = Guid.NewGuid();
        Setup(
            txs: [new(tx1, _merchant1, "AUTHORIZED", 1000m, "BRL", _refDate),
                  new(tx2, _merchant1, "AUTHORIZED", 2000m, "BRL", _refDate)],
            caps: [new(Guid.NewGuid(), tx1, _merchant1, 1000m, "BRL", _refDate),
                   new(Guid.NewGuid(), tx2, _merchant1, 2000m, "BRL", _refDate)],
            stls: [new(Guid.NewGuid(), _merchant1, 2, 3000m, 2925m, "PROCESSED", _refDate)]);

        var report = await _service.RunReconciliationAsync(_refDate);
        report.TotalAuthorizedAmount.Should().Be(3000m);
        report.TotalCapturedAmount.Should().Be(3000m);
        report.TotalSettledGrossAmount.Should().Be(3000m);
        report.TotalSettledNetAmount.Should().Be(2925m);
    }
}
EOF

echo ""
echo "=== Running ALL tests ==="
dotnet test src/AcquirerFlow.slnx --verbosity minimal 2>&1 | tail -15

echo ""
echo "╔══════════════════════════════════════════════╗"
echo "║           SETUP COMPLETE!                    ║"
echo "╠══════════════════════════════════════════════╣"
echo "║  To start Reconciliation:                    ║"
echo "║  dotnet run --project src/Services/          ║"
echo "║    AcquirerFlow.Reconciliation/              ║"
echo "║    AcquirerFlow.Reconciliation.Worker        ║"
echo "║  Swagger: http://localhost:5222              ║"
echo "║                                              ║"
echo "║  POST /api/reconciliation/run                ║"
echo "║  GET  /api/reconciliation                    ║"
echo "║  GET  /api/reconciliation/{id}               ║"
echo "╚══════════════════════════════════════════════╝"

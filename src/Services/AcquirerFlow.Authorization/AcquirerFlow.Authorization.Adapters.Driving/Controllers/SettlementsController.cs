using AcquirerFlow.Infrastructure.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcquirerFlow.Authorization.Adapters.Driving.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SettlementsController : ControllerBase
{
    private readonly AcquirerFlowDbContext _db;
    public SettlementsController(AcquirerFlowDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int size = 20)
    {
        var total = await _db.SettlementBatches.CountAsync();
        var items = await _db.SettlementBatches
            .OrderByDescending(b => b.ReferenceDate)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(b => new
            {
                b.Id, b.MerchantId, b.ReferenceDate, b.SettlementDate,
                b.TransactionCount, b.Status,
                GrossAmount = b.Fees.GrossAmount,
                MdrAmount = b.Fees.MdrAmount,
                NetAmount = b.Fees.NetAmount
            })
            .ToListAsync();

        return Ok(new { total, page, size, items });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var b = await _db.SettlementBatches.FirstOrDefaultAsync(x => x.Id == id);
        if (b is null) return NotFound();

        return Ok(new
        {
            b.Id, b.MerchantId, b.ReferenceDate, b.SettlementDate,
            b.TransactionCount, b.Status,
            Fees = new
            {
                b.Fees.GrossAmount, b.Fees.MdrAmount, b.Fees.MdrRate,
                b.Fees.InterchangeAmount, b.Fees.InterchangeRate,
                b.Fees.SchemeFeeAmount, b.Fees.SchemeFeeRate,
                b.Fees.AcquirerFeeAmount, b.Fees.NetAmount
            }
        });
    }
}

using AcquirerFlow.Infrastructure.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcquirerFlow.Authorization.Adapters.Driving.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransactionsController : ControllerBase
{
    private readonly AcquirerFlowDbContext _db;
    public TransactionsController(AcquirerFlowDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int size = 20)
    {
        var total = await _db.Transactions.CountAsync();
        var items = await _db.Transactions
            .AsNoTracking()
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync();

        var result = items.Select(t => new
        {
            t.Id, t.ExternalId, t.MerchantId,
            Card = t.Card.MaskedDisplay,
            Amount = t.Amount.Amount,
            Currency = t.Amount.Currency,
            Status = t.Status.Value,
            t.Type, t.Installments, t.AuthorizationCode, t.CreatedAt
        });

        return Ok(new { total, page, size, items = result });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var t = await _db.Transactions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (t is null) return NotFound();

        return Ok(new
        {
            t.Id, t.ExternalId, t.MerchantId,
            Card = new { t.Card.MaskedDisplay, t.Card.Brand },
            Amount = new { t.Amount.Amount, t.Amount.Currency },
            Status = t.Status.Value,
            t.Type, t.Installments, t.AuthorizationCode,
            t.DeclinedReason, t.CreatedAt, t.AuthorizedAt, t.CapturedAt
        });
    }

    [HttpGet("merchant/{merchantId:guid}")]
    public async Task<IActionResult> GetByMerchant(Guid merchantId)
    {
        var items = await _db.Transactions
            .AsNoTracking()
            .Where(t => t.MerchantId == merchantId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        var result = items.Select(t => new
        {
            t.Id, t.ExternalId,
            Amount = t.Amount.Amount,
            Status = t.Status.Value,
            t.Type, t.CreatedAt
        });

        return Ok(result);
    }
}

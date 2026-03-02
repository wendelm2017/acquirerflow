using AcquirerFlow.Infrastructure.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcquirerFlow.Authorization.Adapters.Driving.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CapturesController : ControllerBase
{
    private readonly AcquirerFlowDbContext _db;
    public CapturesController(AcquirerFlowDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int size = 20)
    {
        var total = await _db.CapturedTransactions.CountAsync();
        var items = await _db.CapturedTransactions
            .OrderByDescending(t => t.CapturedAt)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(t => new
            {
                t.Id, t.OriginalTransactionId, t.MerchantId,
                t.CardBrand, t.CapturedAmount, t.Currency,
                t.Type, t.Installments, t.Status, t.CapturedAt
            })
            .ToListAsync();

        return Ok(new { total, page, size, items });
    }
}

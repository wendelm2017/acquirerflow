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

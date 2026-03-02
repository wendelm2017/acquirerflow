using AcquirerFlow.Authorization.Application.DTOs;
using AcquirerFlow.Authorization.Application.Services;
using AcquirerFlow.Authorization.Domain;
using Microsoft.AspNetCore.Mvc;

namespace AcquirerFlow.Authorization.Adapters.Driving.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthorizationController : ControllerBase
{
    private readonly AuthorizationAppService _service;

    public AuthorizationController(AuthorizationAppService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Authorize([FromBody] AuthorizationRequestDto request)
    {
        try
        {
            var result = await _service.AuthorizeAsync(request);

            return result.Status == "DECLINED"
                ? Ok(result)
                : CreatedAtAction(nameof(Authorize), new { id = result.TransactionId }, result);
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

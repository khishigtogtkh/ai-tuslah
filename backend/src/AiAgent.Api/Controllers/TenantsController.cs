using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using AiAgent.Application.Tenants;
using AiAgent.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiAgent.Api.Controllers;

public sealed record CreateTenantRequest(
    [Required(ErrorMessage = "Компанийн нэрээ оруулна уу."), StringLength(200, ErrorMessage = "Компанийн нэр 200 тэмдэгтээс хэтрэхгүй байна.")] string Name,
    [Required(ErrorMessage = "Имэйл хаягаа оруулна уу."), EmailAddress(ErrorMessage = "Зөв имэйл хаяг оруулна уу."), StringLength(254, ErrorMessage = "Имэйл хаяг 254 тэмдэгтээс хэтрэхгүй байна.")] string ContactEmail,
    [StringLength(32, ErrorMessage = "Утас 32 тэмдэгтээс хэтрэхгүй байна.")] string? Phone);

[ApiController]
[Authorize(Policy = PlatformRoles.Admin)]
[Route("api/platform/tenants")]
public sealed class TenantsController(TenantService tenants) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 25, CancellationToken ct = default)
    {
        if ((long)(page - 1) * pageSize > int.MaxValue)
            return Problem(statusCode: 400, title: "Хуудасны дугаар хэт их байна.");
        return Ok(await tenants.ListAsync(page, pageSize, ct));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Detail(Guid id, CancellationToken ct)
    {
        var tenant = await tenants.GetAsync(id, ct);
        return tenant is null ? Problem(statusCode: 404, title: "Tenant олдсонгүй.") : Ok(tenant);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateTenantRequest request, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actor))
            return Problem(statusCode: 401);
        var tenant = await tenants.CreateAsync(request.Name, request.ContactEmail, request.Phone, actor, ct);
        return CreatedAtAction(nameof(Detail), new { id = tenant.Id }, tenant);
    }
}

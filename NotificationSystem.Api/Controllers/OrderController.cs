using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationSystem.Api.Models.Dtos;
using NotificationSystem.Api.Models.Entities;
using NotificationSystem.Api.Services.Abstractions;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrderController(IOrderService svc) : ControllerBase
{
    private readonly IOrderService _svc = svc;

    [HttpPost]
    public async Task<ActionResult<Order>> CreateOrder([FromBody] CreateOrderDto dto, CancellationToken ct)
    {
        var created = await _svc.CreateOrderAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Order>> GetById(Guid id, CancellationToken ct)
    {
        var order = await _svc.GetByIdAsync(id, ct);
        if (order == null) return NotFound();
        return Ok(order);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Order>>> GetAll(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 50,
    CancellationToken ct = default)
    {
        if (page < 1 || pageSize < 1 || pageSize > 200)
            return BadRequest("page size must be between 1 and 200");

        var list = await _svc.GetAllAsync(page, pageSize, ct);
        return Ok(list);
    }
}
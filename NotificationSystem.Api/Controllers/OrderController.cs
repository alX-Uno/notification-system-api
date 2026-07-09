using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationSystem.Api.Models.Dtos;
using NotificationSystem.Api.Models.Entities;
using NotificationSystem.Api.Services.Abstractions;
using NotificationSystem.Api.Services.Helpers;

namespace NotificationSystem.Api.Controllers
{
    [ApiController]
    [Route("api/orders")]
    [Authorize]
    public class OrderController(IOrderService svc) : ControllerBase
    {
        private readonly IOrderService _svc = svc;

        [HttpPost]
        public async Task<ActionResult<OrderDto>> CreateOrder([FromBody] CreateOrderDto dto, CancellationToken ct)
        {
            var created = await _svc.CreateOrderAsync(dto, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created.AsDto());
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<OrderDetailsDto>> GetById(Guid id, CancellationToken ct)
        {
            var order = await _svc.GetByIdAsync(id, ct);
            if (order == null) return NotFound();
            return Ok(order.AsDetailsDto());
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrderDto>>> GetAll(
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
}
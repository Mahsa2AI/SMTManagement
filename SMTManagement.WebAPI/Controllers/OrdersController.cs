using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMTManagement.Contracts.Interfaces.Services;
using SMTManagement.Contracts.Models.DTOs;

namespace SMTManagement.WebAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/orders")]
public sealed class OrdersController : ControllerBase
{
    private readonly IProductionService _production;
    private readonly ILogger<OrdersController> _logger;
    private readonly IOrderService _service;

    public OrdersController(
        IOrderService service,
        IProductionService production,
        ILogger<OrdersController> logger)
    {
        _service = service;
        _production = production;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        CancellationToken ct)
    {
        return Ok(await _service.GetAllAsync(search, ct));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(
        int id, CancellationToken ct)
    {
        var result = await _service.GetByIdAsync(id, ct);

        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
    [FromBody] CreateOrderRequest request,
    CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Name is required.");

        var id = await _service.CreateAsync(request, ct);

        _logger.LogInformation(
            "Order created. OrderId: {OrderId}, OrderName: {OrderName}",
            id,
            request.Name);

        return CreatedAtAction(
            nameof(GetById),
            new { id },
            new { id, message = "Order created successfully." });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
    int id,
    UpdateOrderRequest request,
    CancellationToken ct)
    {
        var updated = await _service.UpdateAsync(id, request, ct);

        if (!updated)
        {
            _logger.LogWarning(
                "Attempted to update nonexistent Order. OrderId: {OrderId}",
                id);

            return NotFound();
        }

        _logger.LogInformation(
            "Order updated. OrderId: {OrderId}",
            id);

        return NoContent();
    }


    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(
        int id,
        CancellationToken ct)
    {
        var deleted = await _service.DeleteAsync(id, ct);

        if (!deleted)
        {
            _logger.LogWarning(
                "Attempted to delete nonexistent Order. OrderId: {OrderId}",
                id);

            return NotFound();
        }

        _logger.LogInformation(
            "Order deleted. OrderId: {OrderId}",
            id);

        return NoContent();
    }


    [HttpPut("{id:int}/boards")]
    public async Task<IActionResult> SetBoards(
        int id,
        [FromBody] UpdateOrderBoardsRequest request,
        CancellationToken ct)
    {
        if (request.BoardIds is null)
            return BadRequest("BoardIds is required.");

        try
        {
            var result = await _service.SetBoardsAsync(
                id, request.BoardIds, ct);

            return result
                ? NoContent()
                : NotFound(new { message = "Order not found." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:int}/production-export")]
    public async Task<IActionResult> Download(
        int id, CancellationToken ct)
    {
        var file = await _production.ExportOrderAsync(id, ct);

        if (file is null)
            return NotFound(new { message = "Order not found." });

        return File(
            file,
            "application/json",
            $"production-order-{id}.json");
    }
}

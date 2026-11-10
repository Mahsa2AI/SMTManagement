using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using SMTManagement.Contracts.Interfaces.Services;
using SMTManagement.Contracts.Models.DTOs;

namespace SMTManagement.WebAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/boards")]
public sealed class BoardsController : ControllerBase
{
    private readonly IBoardService _service;
    private readonly ILogger<BoardsController> _logger;

    public BoardsController(IBoardService service, ILogger<BoardsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search, CancellationToken ct)
    {
        _logger.LogInformation("Retrieving boards. Search: {Search}", search);

        var boards = await _service.GetAllAsync(search, ct);

        _logger.LogInformation("Successfully retrieved boards.");

        return Ok(boards);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(
        int id,
        CancellationToken ct)
    {
        _logger.LogInformation("Retrieving board with ID {BoardId}.", id);

        var board = await _service.GetByIdAsync(id, ct);

        if (board is null)
        {
            _logger.LogWarning("Board with ID {BoardId} was not found.", id);

            return NotFound();
        }

        _logger.LogInformation("Successfully retrieved board with ID {BoardId}.",id);

        return Ok(board);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateBoardRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            _logger.LogWarning("Board creation failed: Name is required.");

            return BadRequest("Name is required.");
        }

        _logger.LogInformation("Creating board with name {BoardName}.", request.Name);

        var id = await _service.CreateAsync(request, ct);

        _logger.LogInformation("Board created successfully with ID {BoardId}.", id);

        return CreatedAtAction(
            nameof(GetById),
            new { id },
            new { id, message = "Board created successfully." });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateBoardRequest request,
        CancellationToken ct)
    {
        _logger.LogInformation("Updating board with ID {BoardId}.", id);

        var result = await _service.UpdateAsync(id, request, ct);

        if (!result)
        {
            _logger.LogWarning("Board update failed. Board with ID {BoardId} was not found.", id);

            return NotFound();
        }

        _logger.LogInformation("Board with ID {BoardId} updated successfully.", id);

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        _logger.LogInformation("Deleting board with ID {BoardId}.", id);

        try
        {
            var result = await _service.DeleteAsync(id, ct);

            if (!result)
            {
                _logger.LogWarning("Board deletion failed. Board with ID {BoardId} was not found.",id);

                return NotFound();
            }

            _logger.LogInformation("Board with ID {BoardId} deleted successfully.",id);

            return NoContent();
        }
        catch (SqliteException ex)
            when (ex.SqliteErrorCode == 19)
        {
            _logger.LogWarning(ex,"Cannot delete board with ID {BoardId} because it is assigned to an order.", id);

            return Conflict(new
            {
                message = "Cannot delete this board because it is assigned to an order."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,"Unexpected error occurred while deleting board with ID {BoardId}.", id);

            throw;
        }
    }

    [HttpPut("{id:int}/components")]
    public async Task<IActionResult> SetComponents(int id, [FromBody] UpdateBoardComponentsRequest request, CancellationToken ct)
    {
        if (request.ComponentIds is null)
        {
            _logger.LogWarning("Setting components failed for board {BoardId}: ComponentIds is null.",id);

            return BadRequest("ComponentIds is required.");
        }

        _logger.LogInformation("Setting {ComponentCount} components for board {BoardId}.", request.ComponentIds.Count, id);

        try
        {
            var result = await _service.SetComponentsAsync(id,request.ComponentIds, ct);

            if (!result)
            {
                _logger.LogWarning("Cannot set components. Board with ID {BoardId} was not found.", id);

                return NotFound(new { message = "Board not found." });
            }

            _logger.LogInformation("Successfully updated components for board {BoardId}.", id);

            return NoContent();
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex,"Invalid component assignment for board {BoardId}.",id);

            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,"Unexpected error occurred while setting components for board {BoardId}.", id);

            throw;
        }
    }
}

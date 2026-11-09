
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using SMTManagement.Contracts.Interfaces.SqliteAdapter;
using SMTManagement.Contracts.Models.DTOs;

namespace SMTManagement.WebAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/components")]
public sealed class ComponentsController : ControllerBase
{
    private readonly IComponentRepository _repository;
    private readonly ILogger<ComponentsController> _logger;

    public ComponentsController(
        IComponentRepository repository,
        ILogger<ComponentsController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "Retrieving components. Search: {Search}",
            search);

        var components = await _repository.GetAllAsync(search, ct);

        _logger.LogInformation(
            "Successfully retrieved components.");

        return Ok(components);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(
        int id,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "Retrieving component with ID {ComponentId}.",
            id);

        var component = await _repository.GetByIdAsync(id, ct);

        if (component is null)
        {
            _logger.LogWarning(
                "Component with ID {ComponentId} was not found.",
                id);

            return NotFound();
        }

        _logger.LogInformation(
            "Successfully retrieved component with ID {ComponentId}.",
            id);

        return Ok(component);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateComponentRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            _logger.LogWarning(
                "Component creation failed: Name is required.");

            return BadRequest("Name is required.");
        }

        _logger.LogInformation(
            "Creating component with name {ComponentName}.",
            request.Name);

        var id = await _repository.CreateAsync(request, ct);

        _logger.LogInformation(
            "Component created successfully with ID {ComponentId}.",
            id);

        return CreatedAtAction(
            nameof(GetById),
            new { id },
            new { id, message = "Component created successfully." });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateComponentRequest request,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "Updating component with ID {ComponentId}.",
            id);

        var result = await _repository.UpdateAsync(id, request, ct);

        if (!result)
        {
            _logger.LogWarning(
                "Component update failed. Component with ID {ComponentId} was not found.",
                id);

            return NotFound();
        }

        _logger.LogInformation(
            "Component with ID {ComponentId} updated successfully.",
            id);

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(
        int id,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "Deleting component with ID {ComponentId}.",
            id);

        try
        {
            var result = await _repository.DeleteAsync(id, ct);

            if (!result)
            {
                _logger.LogWarning(
                    "Component deletion failed. Component with ID {ComponentId} was not found.",
                    id);

                return NotFound();
            }

            _logger.LogInformation(
                "Component with ID {ComponentId} deleted successfully.",
                id);

            return NoContent();
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            _logger.LogWarning(
                ex,
                "Cannot delete component with ID {ComponentId} because it is assigned to a board.",
                id);

            return Conflict(new
            {
                message = "Cannot delete this component because it is assigned to a board."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error occurred while deleting component with ID {ComponentId}.",
                id);

            throw;
        }
    }
}

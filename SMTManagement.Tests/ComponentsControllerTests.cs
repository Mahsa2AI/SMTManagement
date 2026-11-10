using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Moq;
using SMTManagement.Contracts.Interfaces.Services;
using SMTManagement.Contracts.Models;
using SMTManagement.Contracts.Models.DTOs;
using SMTManagement.WebAPI.Controllers;
using Xunit;

namespace SMTManagement.Tests;

public class ComponentsControllerTests
{
    private readonly Mock<IComponentService> _service;
    private readonly Mock<ILogger<ComponentsController>> _logger;
    private readonly ComponentsController _controller;

    public ComponentsControllerTests()
    {
        _service = new Mock<IComponentService>();
        _logger = new Mock<ILogger<ComponentsController>>();

        _controller = new ComponentsController(
            _service.Object,
            _logger.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsComponents()
    {
        var components = new List<Component>
        {
            new()
            {
                Id = 1,
                Name = "Resistor",
                Quantity = 10
            }
        };

        _service
            .Setup(x => x.GetAllAsync(
                null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(components);

        var result = await _controller.GetAll(
            null, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);

        Assert.Equal(components, ok.Value);
    }

    [Fact]
    public async Task GetById_WhenExists_ReturnsComponent()
    {
        var component = new Component
        {
            Id = 1,
            Name = "Resistor",
            Quantity = 10
        };

        _service
            .Setup(x => x.GetByIdAsync(
                1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(component);

        var result = await _controller.GetById(
            1, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);

        Assert.Equal(component, ok.Value);
    }

    [Fact]
    public async Task GetById_WhenMissing_ReturnsNotFound()
    {
        _service
            .Setup(x => x.GetByIdAsync(
                100, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Component?)null);

        var result = await _controller.GetById(
            100, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Create_WhenValid_ReturnsCreated()
    {
        var request = new CreateComponentRequest(
            "Resistor",
            "10K resistor",
            10);

        _service
            .Setup(x => x.CreateAsync(
                request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result = await _controller.Create(
            request, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);

        Assert.Equal(1, created.RouteValues!["id"]);
    }

    [Fact]
    public async Task Update_WhenExists_ReturnsNoContent()
    {
        var request = new UpdateComponentRequest(
            "Updated Resistor",
            "Updated description",
            20);

        _service
            .Setup(x => x.UpdateAsync(
                1, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _controller.Update(
            1, request, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_WhenExists_ReturnsNoContent()
    {
        _service
            .Setup(x => x.DeleteAsync(
                1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _controller.Delete(
            1, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_WhenReferenced_ReturnsConflict()
    {
        // SQLite error code 19 = constraint violation.
        var exception = new SqliteException(
            "FOREIGN KEY constraint failed", 19);

        _service
            .Setup(x => x.DeleteAsync(
                1, It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        var result = await _controller.Delete(
            1, CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result);
    }
}


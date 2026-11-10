using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using SMTManagement.Contracts.Interfaces.Services;
using SMTManagement.Contracts.Models;
using SMTManagement.Contracts.Models.DTOs;
using SMTManagement.WebAPI.Controllers;
using Xunit;

namespace SMTManagement.Tests;

public class OrdersControllerTests
{
    private readonly Mock<IOrderService> _service;
    private readonly Mock<IProductionService> _production;
    private readonly Mock<ILogger<OrdersController>> _logger;
    private readonly OrdersController _controller;

    public OrdersControllerTests()
    {
        _service = new Mock<IOrderService>();
        _production = new Mock<IProductionService>();
        _logger = new Mock<ILogger<OrdersController>>();

        _controller = new OrdersController(
            _service.Object,
            _production.Object,
            _logger.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithOrders()
    {
        // Arrange
        var orders = new List<Order>
        {
            new()
            {
                Id = 1,
                Name = "Order 001",
                OrderDate = DateTime.UtcNow
            }
        };

        _service
            .Setup(x => x.GetAllAsync(
                null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        // Act
        var result = await _controller.GetAll(
            null, CancellationToken.None);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);

        Assert.Equal(orders, ok.Value);
    }

    [Fact]
    public async Task GetById_WhenOrderExists_ReturnsOk()
    {
        // Arrange
        var order = new Order
        {
            Id = 1,
            Name = "Order 001",
            OrderDate = DateTime.UtcNow
        };

        _service
            .Setup(x => x.GetByIdAsync(
                1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act
        var result = await _controller.GetById(
            1, CancellationToken.None);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);

        Assert.Equal(order, ok.Value);
    }

    [Fact]
    public async Task GetById_WhenOrderDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        _service
            .Setup(x => x.GetByIdAsync(
                100, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        // Act
        var result = await _controller.GetById(
            100, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Create_WhenValidRequest_ReturnsCreated()
    {
        // Arrange
        var request = new CreateOrderRequest(
            "Order 001",
            "Test order",
            DateTime.UtcNow);

        _service
            .Setup(x => x.CreateAsync(
                request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _controller.Create(
            request, CancellationToken.None);

        // Assert
        var created = Assert.IsType<CreatedAtActionResult>(result);

        Assert.Equal(1, created.RouteValues!["id"]);
    }

    [Fact]
    public async Task Update_WhenOrderExists_ReturnsNoContent()
    {
        // Arrange
        var request = new UpdateOrderRequest(
            "Updated Order",
            "Updated description",
            DateTime.UtcNow);

        _service
            .Setup(x => x.UpdateAsync(
                1, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Update(
            1, request, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Update_WhenOrderDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var request = new UpdateOrderRequest(
            "Updated Order",
            null,
            DateTime.UtcNow);

        _service
            .Setup(x => x.UpdateAsync(
                100, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Update(
            100, request, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_WhenOrderExists_ReturnsNoContent()
    {
        // Arrange
        _service
            .Setup(x => x.DeleteAsync(
                1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(
            1, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task SetBoards_WhenValid_ReturnsNoContent()
    {
        // Arrange
        var request = new UpdateOrderBoardsRequest([1, 2, 3]);

        _service
            .Setup(x => x.SetBoardsAsync(
                1,
                request.BoardIds,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.SetBoards(
            1, request, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task SetBoards_WhenBoardDoesNotExist_ReturnsBadRequest()
    {
        // Arrange
        var request = new UpdateOrderBoardsRequest([100]);

        _service
            .Setup(x => x.SetBoardsAsync(
                1,
                request.BoardIds,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException(
                "Board 100 does not exist."));

        // Act
        var result = await _controller.SetBoards(
            1, request, CancellationToken.None);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);

        Assert.NotNull(badRequest.Value);
    }

    [Fact]
    public async Task Download_WhenOrderExists_ReturnsJsonFile()
    {
        // Arrange
        var order = new Order
        {
            Id = 1,
            Name = "Order 001",
            OrderDate = DateTime.UtcNow
        };

        _production
            .Setup(x => x.ExportOrderAsync(
                1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.SerializeToUtf8Bytes(new { Order = order }));

        // Act
        var result = await _controller.Download(
            1, CancellationToken.None);

        // Assert
        var file = Assert.IsType<FileContentResult>(result);

        Assert.Equal("application/json", file.ContentType);
        Assert.Equal("production-order-1.json", file.FileDownloadName);
        Assert.NotEmpty(file.FileContents);
    }
}


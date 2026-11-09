
using Microsoft.Extensions.Logging;
using Moq;
using SMTManagement.Contracts.Interfaces.Services;
using SMTManagement.Contracts.Interfaces.SqliteAdapter;
using SMTManagement.Contracts.Models;
using SMTManagement.Services;
using System.Text.Json;
using Xunit;

namespace SMTManagement.Tests.UnitTests.Services;

public class ProductionServiceTests
{
    private readonly Mock<IOrderRepository> _orderRepository;
    private readonly Mock<ILogger<IProductionService>> _logger;
    private readonly IProductionService _service;

    public ProductionServiceTests()
    {
        _orderRepository = new Mock<IOrderRepository>();
        _logger = new Mock<ILogger<IProductionService>>();

        _service = new ProductionService(
            _orderRepository.Object,
            _logger.Object);
    }

    // ==========================================
    // EXPORT EXISTING ORDER
    // ==========================================

    [Fact]
    public async Task ExportOrderAsync_ReturnsJsonBytes_WhenOrderExists()
    {
        // Arrange
        var order = CreateOrder(1, "Production Order 001");

        _orderRepository
            .Setup(x => x.GetByIdAsync(
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act
        var result = await _service.ExportOrderAsync(
            1,
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        // Verify that the returned bytes contain valid JSON.
        using var json = JsonDocument.Parse(result);

        Assert.Equal(
            JsonValueKind.Object,
            json.RootElement.ValueKind);
    }

    // ==========================================
    // ORDER NOT FOUND
    // ==========================================

    [Fact]
    public async Task ExportOrderAsync_ReturnsNull_WhenOrderDoesNotExist()
    {
        // Arrange
        _orderRepository
            .Setup(x => x.GetByIdAsync(
                100,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        // Act
        var result = await _service.ExportOrderAsync(
            100,
            CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    // ==========================================
    // JSON STRUCTURE
    // ==========================================

    [Fact]
    public async Task ExportOrderAsync_ContainsExportIdExportedAtAndOrder()
    {
        // Arrange
        var order = CreateOrder(1, "Order 001");

        _orderRepository
            .Setup(x => x.GetByIdAsync(
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act
        var result = await _service.ExportOrderAsync(
            1,
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);

        using var json = JsonDocument.Parse(result);

        var root = json.RootElement;

        Assert.True(root.TryGetProperty(
            "ExportId", out var exportId));

        Assert.True(root.TryGetProperty(
            "ExportedAt", out var exportedAt));

        Assert.True(root.TryGetProperty(
            "Order", out var exportedOrder));

        Assert.True(Guid.TryParse(
            exportId.GetString(), out _));

        Assert.True(exportedAt.ValueKind ==
                    JsonValueKind.String);

        Assert.Equal(1, exportedOrder.GetProperty("Id").GetInt32());

        Assert.Equal(
            "Order 001",
            exportedOrder.GetProperty("Name").GetString());
    }

    // ==========================================
    // EXPORT ID
    // ==========================================

    [Fact]
    public async Task ExportOrderAsync_GeneratesUniqueExportIds()
    {
        // Arrange
        var order = CreateOrder(1, "Order 001");

        _orderRepository
            .Setup(x => x.GetByIdAsync(
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act
        var firstResult = await _service.ExportOrderAsync(
            1,
            CancellationToken.None);

        var secondResult = await _service.ExportOrderAsync(
            1,
            CancellationToken.None);

        // Assert
        Assert.NotNull(firstResult);
        Assert.NotNull(secondResult);

        using var firstJson = JsonDocument.Parse(firstResult);
        using var secondJson = JsonDocument.Parse(secondResult);

        var firstId = firstJson.RootElement
            .GetProperty("ExportId")
            .GetString();

        var secondId = secondJson.RootElement
            .GetProperty("ExportId")
            .GetString();

        Assert.NotEqual(firstId, secondId);
    }

    // ==========================================
    // REPOSITORY INTERACTION
    // ==========================================

    [Fact]
    public async Task ExportOrderAsync_CallsRepositoryOnce()
    {
        // Arrange
        var order = CreateOrder(1, "Order 001");

        _orderRepository
            .Setup(x => x.GetByIdAsync(
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act
        await _service.ExportOrderAsync(
            1,
            CancellationToken.None);

        // Assert
        _orderRepository.Verify(
            x => x.GetByIdAsync(
                1,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExportOrderAsync_PassesCorrectOrderId()
    {
        // Arrange
        const int orderId = 25;

        _orderRepository
            .Setup(x => x.GetByIdAsync(
                orderId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateOrder(
                orderId,
                "Order 025"));

        // Act
        await _service.ExportOrderAsync(
            orderId,
            CancellationToken.None);

        // Assert
        _orderRepository.Verify(
            x => x.GetByIdAsync(
                orderId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ==========================================
    // CANCELLATION TOKEN
    // ==========================================

    [Fact]
    public async Task ExportOrderAsync_PassesCancellationToken()
    {
        // Arrange
        using var cancellationSource =
            new CancellationTokenSource();

        var cancellationToken =
            cancellationSource.Token;

        _orderRepository
            .Setup(x => x.GetByIdAsync(
                1,
                cancellationToken))
            .ReturnsAsync(CreateOrder(1, "Order 001"));

        // Act
        await _service.ExportOrderAsync(
            1,
            cancellationToken);

        // Assert
        _orderRepository.Verify(
            x => x.GetByIdAsync(
                1,
                cancellationToken),
            Times.Once);
    }

    // ==========================================
    // ERROR HANDLING
    // ==========================================

    [Fact]
    public async Task ExportOrderAsync_PropagatesRepositoryException()
    {
        // Arrange
        _orderRepository
            .Setup(x => x.GetByIdAsync(
                1,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(
                "Database connection failed."));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ExportOrderAsync(
                1,
                CancellationToken.None));
    }

    [Fact]
    public async Task ExportOrderAsync_PropagatesCancellation()
    {
        // Arrange
        using var cancellationSource =
            new CancellationTokenSource();

        cancellationSource.Cancel();

        _orderRepository
            .Setup(x => x.GetByIdAsync(
                1,
                cancellationSource.Token))
            .ThrowsAsync(new OperationCanceledException(
                cancellationSource.Token));

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _service.ExportOrderAsync(
                1,
                cancellationSource.Token));
    }

    // ==========================================
    // HELPER
    // ==========================================

    private static Order CreateOrder(int id, string name)
    {
        return new Order
        {
            Id = id,
            Name = name,
            Description = "Test production order",
            OrderDate = DateTime.UtcNow,
            Boards = new List<Board>()
        };
    }
}

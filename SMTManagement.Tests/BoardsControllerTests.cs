using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SMTManagement.Contracts.Interfaces.SqliteAdapter;
using SMTManagement.Contracts.Models;
using SMTManagement.Contracts.Models.DTOs;
using SMTManagement.WebAPI.Controllers;
using Xunit;

namespace SMTManagement.Tests;

public class BoardsControllerTests
{
    private readonly Mock<IBoardRepository> _repository;
    private readonly Mock<ILogger<BoardsController>> _loggerMock;
    private readonly BoardsController _controller;

    public BoardsControllerTests()
    {
        _repository = new Mock<IBoardRepository>();
        _loggerMock = new Mock<ILogger<BoardsController>>();

        _controller = new BoardsController(
            _repository.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsBoards()
    {
        // Arrange
        var boards = new List<Board>
        {
            new()
            {
                Id = 1,
                Name = "Main Board",
                Length = 200,
                Width = 100
            }
        };

        _repository
            .Setup(x => x.GetAllAsync(
                null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(boards);

        // Act
        var result = await _controller.GetAll(
            null, CancellationToken.None);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(boards, ok.Value);
    }

    [Fact]
    public async Task GetById_WhenExists_ReturnsBoard()
    {
        // Arrange
        var board = new Board
        {
            Id = 1,
            Name = "Main Board",
            Length = 200,
            Width = 100
        };

        _repository
            .Setup(x => x.GetByIdAsync(
                1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(board);

        // Act
        var result = await _controller.GetById(
            1, CancellationToken.None);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(board, ok.Value);
    }

    [Fact]
    public async Task GetById_WhenMissing_ReturnsNotFound()
    {
        _repository
            .Setup(x => x.GetByIdAsync(
                100, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Board?)null);

        var result = await _controller.GetById(
            100, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Create_WhenValid_ReturnsCreated()
    {
        var request = new CreateBoardRequest(
            "Main Board",
            "Test board",
            200,
            100);

        _repository
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
        var request = new UpdateBoardRequest(
            "Updated Board",
            "Updated",
            250,
            120);

        _repository
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
        _repository
            .Setup(x => x.DeleteAsync(
                1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _controller.Delete(
            1, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task SetComponents_WhenValid_ReturnsNoContent()
    {
        var request = new UpdateBoardComponentsRequest([1, 2, 3]);

        _repository
            .Setup(x => x.SetComponentsAsync(
                1,
                request.ComponentIds,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _controller.SetComponents(
            1, request, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task SetComponents_WhenComponentMissing_ReturnsBadRequest()
    {
        var request = new UpdateBoardComponentsRequest([100]);

        _repository
            .Setup(x => x.SetComponentsAsync(
                1,
                request.ComponentIds,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException(
                "Component 100 does not exist."));

        var result = await _controller.SetComponents(
            1, request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }
}

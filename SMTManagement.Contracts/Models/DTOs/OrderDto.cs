namespace SMTManagement.Contracts.Models.DTOs;

public record CreateOrderDto(
    string Name,
    string? Description,
    DateTime OrderDate);

public record UpdateOrderDto(
    string Name,
    string? Description,
    DateTime OrderDate);

public record UpdateOrderBoardsDto(
    List<int> BoardIds);

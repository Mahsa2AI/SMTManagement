namespace SMTManagement.Contracts.Models.DTOs;

public record CreateComponentDto(
    string Name,
    string? Description,
    int Quantity);

public record UpdateComponentDto(
    string Name,
    string? Description,
    int Quantity);

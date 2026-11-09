namespace SMTManagement.Contracts.Models.DTOs;

public record CreateBoardDto(
    string Name,
    string? Description,
    double Length,
    double Width);

public record UpdateBoardDto(
    string Name,
    string? Description,
    double Length,
    double Width);

public record UpdateBoardComponentsDto(
    List<int> ComponentIds);

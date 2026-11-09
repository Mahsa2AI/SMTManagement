namespace SMTManagement.Contracts.Models.DTOs;

using System.ComponentModel.DataAnnotations;

public sealed record CreateOrderRequest(
    [Required, StringLength(150, MinimumLength = 1)]
    string Name,

    [StringLength(1000)]
    string? Description,

    DateTime OrderDate
);

public sealed record UpdateOrderRequest(
    [Required, StringLength(150, MinimumLength = 1)]
    string Name,

    [StringLength(1000)]
    string? Description,

    DateTime OrderDate
);

public sealed record CreateBoardRequest(
    [Required, StringLength(150, MinimumLength = 1)]
    string Name,

    [StringLength(1000)]
    string? Description,

    [Range(0.01, 1000000)]
    double Length,

    [Range(0.01, 1000000)]
    double Width
);

public sealed record UpdateBoardRequest(
    [Required, StringLength(150, MinimumLength = 1)]
    string Name,

    [StringLength(1000)]
    string? Description,

    [Range(0.01, 1000000)]
    double Length,

    [Range(0.01, 1000000)]
    double Width
);

public sealed record CreateComponentRequest(
    [Required, StringLength(150, MinimumLength = 1)]
    string Name,

    [StringLength(1000)]
    string? Description,

    [Range(0, 100000000)]
    int Quantity
);

public sealed record UpdateComponentRequest(
    [Required, StringLength(150, MinimumLength = 1)]
    string Name,

    [StringLength(1000)]
    string? Description,

    [Range(0, 100000000)]
    int Quantity
);

public sealed record UpdateOrderBoardsRequest(
    List<int> BoardIds
);

public sealed record UpdateBoardComponentsRequest(
    List<int> ComponentIds
);


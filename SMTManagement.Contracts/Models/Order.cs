
namespace SMTManagement.Contracts.Models;

public class Order
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime CreatedAt { get; set; }

    public List<Board> Boards { get; set; } = [];
}

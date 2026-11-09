namespace SMTManagement.Contracts.Models;

public class Board
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public double Length { get; set; }
    public double Width { get; set; }
    public DateTime CreatedAt { get; set; }

    public List<Component> Components { get; set; } = [];
}
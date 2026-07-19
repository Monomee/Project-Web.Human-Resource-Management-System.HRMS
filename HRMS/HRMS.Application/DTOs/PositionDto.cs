namespace HRMS.Application.DTOs;

public class PositionDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int JobLevel { get; set; }
}

namespace MemoRecipe.Application.DTOs.Steps;

public class StepDto
{
    public Guid Id { get; set; }
    public int Order { get; set; }
    public string Instruction { get; set; } = string.Empty;
}

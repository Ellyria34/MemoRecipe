namespace MemoRecipe.Web.Models;

public class StepCreateDto
{
    public int Order { get; set; }
    public string Instruction { get; set; } = string.Empty;
}
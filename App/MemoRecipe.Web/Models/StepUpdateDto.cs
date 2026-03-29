namespace MemoRecipe.Web.Models;

public class StepUpdateDto
{
    public Guid? Id { get; set; }
    public int? Order { get; set; }
    public string? Instruction { get; set; }

}
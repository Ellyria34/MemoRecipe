using MemoRecipe.Domain.Entities.Recipes;

namespace MemoRecipe.Domain.Entities.Steps;

public class Step
{
    public Guid Id { get; set; }

    // Foreign key to the recipe
    public Guid RecipeId { get; set; }
    public Recipe? Recipe { get; set; }

    // Order of the step in the recipe (1, 2, 3…)
    public int Order { get; set; }

    // Description of what must be done in this step
    public string Instruction { get; set; } = string.Empty;
}

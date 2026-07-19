using MemoRecipe.Web.Models;

namespace MemoRecipe.Web.Helpers;

public static class RecipeFormValidator
{
    public static bool IsValid(RecipeFormModel? model)
    {
        if (model is null) return false;

        if (string.IsNullOrWhiteSpace(model.Title)
            || model.Title.Length < 3
            || model.Title.Length > 200)
        {
            return false;
        }

        if (model.Ingredients.All(i => string.IsNullOrWhiteSpace(i.Name)))
        {
            return false;
        }

        if (model.Steps.All(s => string.IsNullOrWhiteSpace(s.Instruction)))
        {
            return false;
        }

        return true;
    }
}

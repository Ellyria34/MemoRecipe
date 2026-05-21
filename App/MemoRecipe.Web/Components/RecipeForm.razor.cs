using Microsoft.AspNetCore.Components;
using MemoRecipe.Web.Models;

namespace MemoRecipe.Web.Components;
public partial class RecipeForm
{
    [Parameter] 
    public RecipeFormModel Recipe {get; set;} = default!;

    [Parameter] 
    public EventCallback<RecipeFormModel> OnSave {get; set;}

    private async Task HandleSave()
    {
        await OnSave.InvokeAsync(Recipe);
    }

    private bool IsFormValid()
    {
        if(string.IsNullOrWhiteSpace(Recipe.Title) || Recipe.Title.Length < 3 || Recipe.Title.Length > 200)
        {
            return false;
        }

        if(Recipe.Ingredients.All(i => string.IsNullOrWhiteSpace(i.Name)))
        {
            return false;
        }

        if(Recipe.Steps.All(s => string.IsNullOrWhiteSpace(s.Instruction)))
        {
            return false;
        }
        
        return true;
    }
}
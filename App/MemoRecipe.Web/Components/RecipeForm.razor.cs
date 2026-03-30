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
}
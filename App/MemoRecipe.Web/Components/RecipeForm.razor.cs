using Microsoft.AspNetCore.Components;
using MemoRecipe.Web.Models;

namespace MemoRecipe.Web.Components;

public partial class RecipeForm
{
    [Parameter]
    public RecipeFormModel Recipe { get; set; } = default!;

    [Parameter]
    public EventCallback OnFormChanged { get; set; }

    private async Task NotifyChange()
    {
        await OnFormChanged.InvokeAsync();
    }
}

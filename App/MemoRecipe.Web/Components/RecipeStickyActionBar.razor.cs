using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace MemoRecipe.Web.Components;

public partial class RecipeStickyActionBar
{
    [Parameter] public string SaveLabel { get; set; } = "Enregistrer";
    [Parameter] public string CancelLabel { get; set; } = "Annuler";
    [Parameter] public string CancelConfirmTitle { get; set; } = "Annuler les modifications ?";
    [Parameter] public string CancelConfirmMessage { get; set; } = "Les modifications non sauvegardées seront perdues.";

    [Parameter] public bool IsValid { get; set; } = true;
    [Parameter] public bool IsSaving { get; set; }

    [Parameter] public EventCallback OnSave { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }

    private MudMessageBox _confirmDialog = default!;

    private async Task HandleSaveClick()
    {
        await OnSave.InvokeAsync();
    }

    private async Task HandleCancelClick()
    {
        bool? result = await _confirmDialog.ShowAsync();
        if (result == true)
        {
            await OnCancel.InvokeAsync();
        }
    }
}

using Microsoft.AspNetCore.Components;
using MemoRecipe.Web.Models;
using MemoRecipe.Web.Services;
using MudBlazor;

namespace MemoRecipe.Web.Pages;

public partial class EditRecipe
{
    [Inject] 
    private IRecipeService RecipeService { get; set; }= default!;

    [Inject] 
    private NavigationManager Navigation { get; set; } = default!;

    [Inject] 
    private ISnackbar Snackbar { get; set; } = default!;

    [Parameter]
    public Guid Id { get; set; }

    private RecipeDto? _recipe;
    private RecipeFormModel? _recipeForm;
    private MudMessageBox _confirmDialog = default!;
    private string? _errorMessage;
    bool _isLoading = false;

    protected override async Task OnInitializedAsync()
    {
        _isLoading = true;
        _errorMessage = null;
        try
        {
            _recipe = await RecipeService.GetRecipeByIdAsync(Id);
            _recipeForm = MapToFormModel (_recipe);
        }
        catch (Exception)
        {
            _errorMessage = "Un problème est survenu lors du chargement de votre recette";
            Navigation.NavigateTo($"/recipes/{Id}");
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task HandleModification(RecipeFormModel _recipeForm)
    {
        _isLoading = true;
        _errorMessage = null;
        
        try
        {
            var updtatedRecipe = MapToRecipeUpdateDto(_recipeForm);
            await RecipeService.UpdateRecipeAsync(Id, updtatedRecipe);
            Snackbar.Add("Recette sauvegardée !", Severity.Success, config => 
            {
                config.VisibleStateDuration = 1500;
                config.ShowCloseIcon = false;
            });
            //TODO: When recipe details page was done redirecte to "/recipes/{newRecipe.Id}""
            Navigation.NavigateTo($"/recipes/{Id}");
        }
        catch (Exception)
        {
            _errorMessage = "Un problème est survenu lors de la sauvegarde de la recette";
        }
        finally
        {
            _isLoading = false;
        }
    }
    private async Task HandleCancel()
    {
        bool? result = await _confirmDialog.ShowAsync();
        if (result != true) return;
        Navigation.NavigateTo($"/recipes/{Id}");
    }


    //Mapper
    private RecipeFormModel MapToFormModel (RecipeDto recipeDto)
    {
        RecipeFormModel recipe = new RecipeFormModel
        {
            Title = recipeDto.Title,
            Description = recipeDto.Description,
            Servings = recipeDto.Servings > 0 ? recipeDto.Servings : 1,
            PrepTimeMinutes = recipeDto.PrepTimeMinutes,
            CookTimeMinutes = recipeDto.CookTimeMinutes,
            IsPublic = recipeDto.IsPublic,
            Ingredients = recipeDto.Ingredients.Select(i => new IngredientFormModel
            {
                Name = i.Name,
                Quantity = i.Quantity,
                Unit = i.Unit
            }).ToList(),
            Steps = recipeDto.Steps.Select((s, index) => new StepFormModel
            {
                Instruction = s.Instruction,
                Order = s.Order
            }).ToList()
        };
        return recipe;
    }

    private RecipeUpdateDto MapToRecipeUpdateDto (RecipeFormModel recipeFormModel)
    {
        RecipeUpdateDto recipeUpdateDto = new RecipeUpdateDto
        {
            Title = recipeFormModel.Title,
            Description = recipeFormModel.Description,
            Servings = recipeFormModel.Servings,
            PrepTimeMinutes = recipeFormModel.PrepTimeMinutes,
            CookTimeMinutes = recipeFormModel.CookTimeMinutes,
            Difficulty = recipeFormModel.Difficulty,
            IsPublic = recipeFormModel.IsPublic,
            Ingredients = recipeFormModel.Ingredients.Select(i => new IngredientUpdateDto
            {
                Name = i.Name,
                Quantity = i.Quantity,
                Unit = i.Unit
            }).ToList(),
            Steps = recipeFormModel.Steps.Select(s => new StepUpdateDto
            {
                Instruction = s.Instruction,
                Order = s.Order
            }).ToList()
        };
        return recipeUpdateDto;
    }

}

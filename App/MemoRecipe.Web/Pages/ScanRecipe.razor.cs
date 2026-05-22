using Microsoft.AspNetCore.Components;
using MemoRecipe.Web.Models;
using MemoRecipe.Web.Services;
using MudBlazor;
using Microsoft.AspNetCore.Components.Forms;

namespace MemoRecipe.Web.Pages;

public partial class ScanRecipe
{  
    [Inject]
    private IRecipeService RecipeService {get; set;} = default!;

    [Inject]
    private NavigationManager Navigation {get; set;} = default!;

    [Inject]
    private ISnackbar Snackbar {get; set;} = default!;

    private ExtractedRecipeDto? _extractedRecipe;
    private RecipeFormModel? _newRecipe;
    private string? _errorMessage;
    private IBrowserFile? _selectedFile;
    bool _isLoading = false;
    bool _isValid = false;
    
    private void UploadFile(IBrowserFile file)
    {
        _selectedFile = file;
    }

    private async Task HandleGeneration()
    {
        if (_selectedFile == null) 
        {
            return;
        }
        _isLoading = true;
        _errorMessage = null;
        
        try
        {
            var stream = _selectedFile.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024);
            _extractedRecipe = await RecipeService.ScanImageAsync(stream, _selectedFile.ContentType, _selectedFile.Name);
            _newRecipe = MapToFormModel(_extractedRecipe);
        }
        catch (Exception)
        {
            _errorMessage = "Un problème est survenu lors du scan de la recette";
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task HandleCreation(RecipeFormModel recipeFormModel)
    {
        var recipeCreateDto = MapToRecipeCreateDto (recipeFormModel);
        _isLoading = true;
        _errorMessage = null;
        
        try
        {
            var newRecipe = await RecipeService.CreateRecipeAsync(recipeCreateDto);
            Snackbar.Add("Recette sauvegardée !", Severity.Success, config => 
            {
                config.VisibleStateDuration = 1500;
                config.ShowCloseIcon = false;
            });
            //TODO: When recipe details page was done redirecte to "/recipes/{newRecipe.Id}""
            Navigation.NavigateTo($"/recipes");
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

    //Mapper
    private RecipeFormModel MapToFormModel (ExtractedRecipeDto extractedRecipeDto)
    {
        RecipeFormModel recipe = new RecipeFormModel
        {
            Title = extractedRecipeDto.Title,
            Servings = extractedRecipeDto.Servings > 0 ? extractedRecipeDto.Servings : 1,
            PrepTimeMinutes = null,
            Ingredients = extractedRecipeDto.Ingredients.Select(i => new IngredientFormModel
            {
                Name = i
            }).ToList(),
            Steps = extractedRecipeDto.Steps.Select((s, index) => new StepFormModel
            {
                Instruction = s,
                Order = index + 1
            }).ToList()
        };
        return recipe;
    }

    private RecipeCreateDto MapToRecipeCreateDto (RecipeFormModel recipeFormModel)
    {
        RecipeCreateDto recipeCreateDto = new RecipeCreateDto
        {
            Title = recipeFormModel.Title,
            Description = recipeFormModel.Description,
            Servings = recipeFormModel.Servings,
            PrepTimeMinutes = recipeFormModel.PrepTimeMinutes,
            CookTimeMinutes = recipeFormModel.CookTimeMinutes,
            Difficulty = recipeFormModel.Difficulty,
            IsPublic = recipeFormModel.IsPublic,
            Ingredients = recipeFormModel.Ingredients.Select(i => new IngredientCreateDto
            {
                Name = i.Name,
                Quantity = i.Quantity ?? 0,
                Unit = i.Unit
            }).ToList(),
            Steps = recipeFormModel.Steps.Select(s => new StepCreateDto
            {
                Instruction = s.Instruction,
                Order = s.Order
            }).ToList()
        };
        return recipeCreateDto;
    }
}
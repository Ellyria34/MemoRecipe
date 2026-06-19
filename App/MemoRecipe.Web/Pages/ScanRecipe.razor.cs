using Microsoft.AspNetCore.Components;
using MemoRecipe.Web.Models;
using MemoRecipe.Web.Services;
using MudBlazor;
using Microsoft.AspNetCore.Components.Forms;
using MemoRecipe.Web.Helpers;

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
            _newRecipe = RecipeMapper.MapExtractedRecipeDtoToFormModel(_extractedRecipe);
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
        var recipeCreateDto = RecipeMapper.MapToRecipeCreateDto (recipeFormModel);
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
}
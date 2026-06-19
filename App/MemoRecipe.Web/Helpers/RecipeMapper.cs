using MemoRecipe.Web.Models;

namespace MemoRecipe.Web.Helpers;

public static class RecipeMapper
{
        public static RecipeFormModel MapExtractedRecipeDtoToFormModel (ExtractedRecipeDto extractedRecipeDto)
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

    public static RecipeCreateDto MapToRecipeCreateDto (RecipeFormModel recipeFormModel)
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

    public static RecipeFormModel MapRecipeDtoToFormModel (RecipeDto recipeDto)
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
            Steps = recipeDto.Steps.OrderBy(s => s.Order).Select((s, index) => new StepFormModel
            {
                Instruction = s.Instruction,
                Order = s.Order
            }).ToList()
        };
        return recipe;
    }

    public static RecipeUpdateDto MapToRecipeUpdateDto (RecipeFormModel recipeFormModel)
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

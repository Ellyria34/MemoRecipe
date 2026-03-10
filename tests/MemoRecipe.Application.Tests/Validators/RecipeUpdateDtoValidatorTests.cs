using MemoRecipe.Application.Validators;
using MemoRecipe.Application.DTOs.Recipes;
using MemoRecipe.Application.DTOs.Steps;
using MemoRecipe.Application.DTOs.Ingredients;
using FluentValidation.TestHelper;

public class RecipeUpdateDtoValidatorTests
{
    private readonly RecipeUpdateDtoValidator _validator = new();

    #region TitleTest
    [Fact]
    public void Should_NotHaveError_When_TitleIsNull()
    {
        var dto = new RecipeUpdateDto { Title = null };
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Should_HaveError_When_TitleIsTooShort()
    {
        var dto = new RecipeUpdateDto { Title = "AB" };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Should_HaveError_When_TitleIsTooLong()
    {
        var dto = new RecipeUpdateDto { Title = new string('A', 201) };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Should_NotHaveError_When_TitleIsValid()
    {
        var dto = new RecipeUpdateDto { Title = "Tarte aux pommes" };
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Title);
    }
    #endregion

    #region DecriptionTests
    [Fact]
    public void Should_NotHaveError_When_DescriptionIsNull()
    {
        var dto = new RecipeUpdateDto { Description = null };
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Should_HaveError_When_DescriptionIsTooLong()
    {
        var dto = new RecipeUpdateDto { Description = new string('A', 2001) };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }
    #endregion

    #region ServingsTests
    [Fact]
    public void Should_NotHaveError_When_ServingsIsNull()
    {
        var dto = new RecipeUpdateDto { Servings = null };
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Servings);
    }

    [Fact]
    public void Should_HaveError_When_ServingsIsTooHigh()
    {
        var dto = new RecipeUpdateDto { Servings = 101 };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Servings);
    }
    #endregion

    #region PrepTimeMinutesTests
    [Fact]
    public void Should_NotHaveError_When_PrepTimeMinutesIsNull()
    {
        var dto = new RecipeUpdateDto { PrepTimeMinutes = null };
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.PrepTimeMinutes);
    }

    [Fact]
    public void Should_HaveError_When_PrepTimeMinutesIsNegative()
    {
        var dto = new RecipeUpdateDto { PrepTimeMinutes = -1 };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.PrepTimeMinutes);
    }

    [Fact]
    public void Should_HaveError_When_PrepTimeMinutesIsTooHigh()
    {
        var dto = new RecipeUpdateDto { PrepTimeMinutes = 1441 };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.PrepTimeMinutes);
    }
    #endregion

    #region CookTimeMinutesTests
    [Fact]
    public void Should_NotHaveError_When_CookTimeMinutesIsNull()
    {
        var dto = new RecipeUpdateDto { CookTimeMinutes = null };
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.CookTimeMinutes);
    }

    [Fact]
    public void Should_HaveError_When_CookTimeMinutesIsNegative()
    {
        var dto = new RecipeUpdateDto { CookTimeMinutes = -1 };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.CookTimeMinutes);
    }

    [Fact]
    public void Should_HaveError_When_CookTimeMinutesIsTooHigh()
    {
        var dto = new RecipeUpdateDto { CookTimeMinutes = 1441 };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.CookTimeMinutes);
    }
    #endregion

    #region IngredientsTests
    [Fact]
    public void Should_NotHaveError_When_IngredientsIsNull()
    {
        var dto = new RecipeUpdateDto { Ingredients = null };
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Ingredients);
    }

    [Fact]
    public void Should_HaveError_When_IngredientsHasTooManyItems()
    {
        var dto = new RecipeUpdateDto { Ingredients = Enumerable.Range(0, 51).Select(i => new IngredientUpdateDto()).ToList() };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Ingredients);
    }

    #endregion

    #region StepsTests
    [Fact]
    public void Should_NotHaveError_When_StepsIsNull()
    {
        var dto = new RecipeUpdateDto { Steps = null };
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Steps);
    }

    [Fact]
    public void Should_HaveError_When_StepsHasTooManyItems()
    {
        var dto = new RecipeUpdateDto { Steps = Enumerable.Range(0, 51).Select(i => new StepUpdateDto()).ToList() };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Steps);
    }
    #endregion
}

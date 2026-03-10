using MemoRecipe.Application.Validators;
using MemoRecipe.Application.DTOs.Recipes;
using MemoRecipe.Application.DTOs.Steps;
using MemoRecipe.Application.DTOs.Ingredients;
using FluentValidation.TestHelper;

public class RecipeCreateDtoValidatorTests
{
    private readonly RecipeCreateDtoValidator _validator = new();

    #region TitleTests
    [Fact]
    public void Should_HaveError_When_TitleIsEmpty()
    {
        var dto = new RecipeCreateDto { Title = "" };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Should_NotHaveError_When_TitleIsValid()
    {
        var dto = new RecipeCreateDto { Title = "Tarte aux pommes" };
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Should_HaveError_When_TitleIsTooShort()
    {
        var dto = new RecipeCreateDto { Title = "AB" };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }


    [Fact]
    public void Should_HaveError_When_TitleIsTooLong()
    {
        var title = new string('A', 201);
        var dto = new RecipeCreateDto { Title = title };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }
    #endregion

    #region DescriptionTests
    [Fact]
    public void Should_NotHaveError_When_DescriptionIsNull()
    {
        var dto = new RecipeCreateDto { Title = "Test", Description = "" };
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Should_HaveError_When_DescriptionIsTooLong()
    {
        var description = new string('A', 2002);
        var dto = new RecipeCreateDto { Title = "Test", Description = description };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }
    #endregion

    #region ServingsTests
    [Fact]
    public void Should_HaveError_When_ServingsIsEmpty()
    {
        var dto = new RecipeCreateDto { Title = "Test", Servings = 0 };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Servings);
    }

    [Fact]
    public void Should_NotHaveError_When_ServingsIsValid()
    {
        var dto = new RecipeCreateDto { Title = "Test", Servings = 30 };
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Servings);
    }

    [Fact]
    public void Should_NotHaveError_When_ServingsIsNull()
    {
        var dto = new RecipeCreateDto { Title = "Test", Servings = null };
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Servings);
    }

    [Fact]
    public void Should_HaveError_When_ServingsIsTooHigh()
    {
        var dto = new RecipeCreateDto { Title = "Test", Servings = 101 };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Servings);
    }

    [Fact]
    public void Should_HaveError_When_ServingsIsNegative()
    {
        var dto = new RecipeCreateDto { Title = "Test", Servings = -1 };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Servings);
    }
    #endregion

    #region PrepTimeMinutesTests
    [Fact]
    public void Should_NotHaveError_When_PrepTimeMinutesIsValid()
    {
        var dto = new RecipeCreateDto { Title = "Test", PrepTimeMinutes = 350 };
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.PrepTimeMinutes);
    }

    [Fact]
    public void Should_NotHaveError_When_PrepTimeMinutesIsZero()
    {
        var dto = new RecipeCreateDto { Title = "Test", PrepTimeMinutes = 0 };
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.PrepTimeMinutes);
    }

    [Fact]
    public void Should_HaveError_When_PrepTimeMinutesIsTooHigh()
    {
        var dto = new RecipeCreateDto { Title = "Test", PrepTimeMinutes = 1500 };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.PrepTimeMinutes);
    }

    [Fact]
    public void Should_HaveError_When_PrepTimeMinutesIsNegative()
    {
        var dto = new RecipeCreateDto { Title = "Test", PrepTimeMinutes = -1 };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.PrepTimeMinutes);
    }

    #endregion

    #region CookTimeMinutesTests
    [Fact]
    public void Should_NotHaveError_When_CookTimeMinutesIsValid()
    {
        var dto = new RecipeCreateDto { Title = "Test", CookTimeMinutes = 350 };
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.CookTimeMinutes);
    }

    [Fact]
    public void Should_NotHaveError_When_CookTimeMinutesIsZero()
    {
        var dto = new RecipeCreateDto { Title = "Test", CookTimeMinutes = 0 };
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.CookTimeMinutes);
    }

    [Fact]
    public void Should_HaveError_When_CookTimeMinutesIsTooHigh()
    {
        var dto = new RecipeCreateDto { Title = "Test", CookTimeMinutes = 1500 };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.CookTimeMinutes);
    }

    [Fact]
    public void Should_HaveError_When_CookTimeMinutesIsNegative()
    {
        var dto = new RecipeCreateDto { Title = "Test", CookTimeMinutes = -1 };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.CookTimeMinutes);
    }
    #endregion

    #region IngredientsTests
    [Fact]
    public void Should_NotHaveError_When_IngredientsEmpty()
    {
        var ingredients = new List<IngredientCreateDto>();
        var dto = new RecipeCreateDto { Title = "Test", Ingredients = ingredients};
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Ingredients);
    }

    [Fact]
    public void Should_NotHaveError_When_IngredientsIsValid()
    {
        var ingredients = Enumerable.Range(0, 45).Select(i => new IngredientCreateDto()).ToList();
        var dto = new RecipeCreateDto { Title = "Test", Ingredients = ingredients};
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Ingredients);
    }

    [Fact]
    public void Should_HaveError_When_IngredientsIsTooHigh()
    {
        var ingredients = Enumerable.Range(0, 51).Select(i => new IngredientCreateDto()).ToList();
        var dto = new RecipeCreateDto { Title = "Test", Ingredients = ingredients};
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Ingredients);
    }
    #endregion

    #region StepsTests
        [Fact]
    public void Should_NotHaveError_When_StepsEmpty()
    {
        var steps = new List<StepCreateDto>();
        var dto = new RecipeCreateDto { Title = "Test", Steps = steps};
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Steps);
    }
    
    [Fact]
    public void Should_NotHaveError_When_StepsIsValid()
    {
        var steps = Enumerable.Range(0, 45).Select(i => new StepCreateDto()).ToList();
        var dto = new RecipeCreateDto { Title = "Test", Steps = steps};
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Steps);
    }

    [Fact]
    public void Should_HaveError_When_StepsIsTooHigh()
    {
        var steps = Enumerable.Range(0, 51).Select(i => new StepCreateDto()).ToList();
        var dto = new RecipeCreateDto { Title = "Test", Steps = steps};
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Steps);
    }
    #endregion
}

using FluentValidation;
using MemoRecipe.Application.DTOs.Recipes;

namespace MemoRecipe.Application.Validators;
public class RecipeUpdateDtoValidator : AbstractValidator<RecipeUpdateDto>
{
    public RecipeUpdateDtoValidator()
    {
        RuleFor(x => x.Title)
            .MinimumLength(3)
            .MaximumLength(200)
            .When(x => x.Title != null);

        RuleFor(x => x.Description)
            .MaximumLength(2000)
            .When(x => x.Description != null);

        RuleFor(x => x.Servings)
            .InclusiveBetween(1, 100)
            .When(x => x.Servings.HasValue);

        RuleFor(x => x.PrepTimeMinutes)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(1440)
            .When(x => x.PrepTimeMinutes.HasValue);

        RuleFor(x => x.CookTimeMinutes)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(1440)
            .When(x => x.CookTimeMinutes.HasValue);

        RuleFor(x => x.Ingredients)
            .Must(list => list?.Count <= 50)
            .WithMessage("Maximum 50 ingrédients.")
            .When(x => x.Ingredients != null);

        RuleFor(x => x.Steps)
            .Must(list => list?.Count <= 50)
            .WithMessage("Maximum 50 étapes.")
            .When(x => x.Steps != null); 
    }
}
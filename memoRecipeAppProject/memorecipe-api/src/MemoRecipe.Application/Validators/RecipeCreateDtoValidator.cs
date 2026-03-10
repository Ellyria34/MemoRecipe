using FluentValidation;
using MemoRecipe.Application.DTOs.Recipes;

namespace MemoRecipe.Application.Validators;
public class RecipeCreateDtoValidator : AbstractValidator<RecipeCreateDto>
{
    public RecipeCreateDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Le titre est obligatoire.")
            .MinimumLength(3)
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .MaximumLength(2000);
        
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
            .Must(list => list.Count <= 50)
            .WithMessage("Maximum 50 ingrédients.");

        RuleFor(x => x.Steps)
            .Must(list => list.Count <= 50)
            .WithMessage("Maximum 50 étapes.");                
    }
}

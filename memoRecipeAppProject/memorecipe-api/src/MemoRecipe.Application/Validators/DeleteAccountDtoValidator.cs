using FluentValidation;
using MemoRecipe.Application.DTOs.Auth;

namespace MemoRecipe.Application.Validators;

public class DeleteAccountDtoValidator : AbstractValidator<DeleteAccountDto>
{
    public DeleteAccountDtoValidator()
    {
        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Le mot de passe est obligatoire.");
    }
}
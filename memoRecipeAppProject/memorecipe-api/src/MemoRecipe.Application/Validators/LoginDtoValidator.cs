using FluentValidation;
using MemoRecipe.Application.DTOs.Auth;

namespace MemoRecipe.Application.Validators;

public class LoginDtoValidator : AbstractValidator<LoginDto>
{
    public LoginDtoValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("L'adresse mail est obligatoire.")
                .EmailAddress()
                .WithMessage("L'adresse mail n'est pas valide.");
            
            RuleFor(x => x.Password)
                .NotEmpty()
                .WithMessage("Le mot de passe est obligatoire.");
        }
}
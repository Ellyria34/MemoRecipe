using FluentValidation;
using MemoRecipe.Application.DTOs.Auth;

namespace MemoRecipe.Application.Validators;

public class RegisterDtoValidator : AbstractValidator<RegisterDto>
{
    public RegisterDtoValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("L'adresse mail est obligatoire.")
                .EmailAddress()
                .WithMessage("L'adresse mail n'est pas valide.");
            
            RuleFor(x => x.Username)
                .NotEmpty()
                .MinimumLength(3)
                .MaximumLength(50);
            
            RuleFor(x => x.Password)
                .NotEmpty()
                .MinimumLength(8)
                .MaximumLength(100)
                .Matches(@"[A-Z]")
                .Matches(@"[a-z]")
                .Matches(@"[0-9]")
                .Matches(@"[^a-zA-Z0-9]");
        }
}
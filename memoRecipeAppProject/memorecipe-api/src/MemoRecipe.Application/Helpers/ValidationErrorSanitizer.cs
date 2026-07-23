using System.Reflection;
using FluentValidation.Results;
using MemoRecipe.Application.Attributes;

namespace MemoRecipe.Application.Helpers;

public static class ValidationErrorSanitizer
{
    public static IEnumerable<object> Sanitize<TDto>(IEnumerable<ValidationFailure> errors)
    {
        var sensitiveProperties = typeof(TDto)
            .GetProperties()
            .Where(p => p.GetCustomAttribute<SensitiveDataAttribute>() != null)
            .Select(p => p.Name)
            .ToHashSet();

        return errors.Select(e => new
        {
            e.PropertyName,
            e.ErrorMessage,
            AttemptedValue = sensitiveProperties.Contains(e.PropertyName)
                ? null
                : e.AttemptedValue
        });
    }
}

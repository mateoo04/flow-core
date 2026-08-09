using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FlowCore.Validation;

public static class ValidationExtensions
{
    public static async Task<bool> ValidateAndAddToModelStateAsync<T>(
        this ControllerBase controller,
        IValidator<T> validator,
        T model,
        CancellationToken ct)
    {
        var result = await validator.ValidateAsync(model, ct);
        result.AddToModelState(controller.ModelState);
        return result.IsValid;
    }

    public static void AddToModelState(this ValidationResult result, ModelStateDictionary modelState)
    {
        foreach (var error in result.Errors)
            modelState.AddModelError(error.PropertyName, error.ErrorMessage);
    }
}

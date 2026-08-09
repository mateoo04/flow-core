using FluentValidation;

namespace FlowCore.Validation;

public static class CommonRuleExtensions
{
    public static IRuleBuilderOptions<T, string> NotBlank<T>(this IRuleBuilder<T, string> rule) =>
        rule.Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("{PropertyName} is required.");

    public static IRuleBuilderOptions<T, Guid> NotEmptyGuid<T>(this IRuleBuilder<T, Guid> rule) =>
        rule.NotEmpty().WithMessage("{PropertyName} is required.");

    public static IRuleBuilderOptions<T, string> HexColor<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotBlank()
            .Matches(ValidationConstants.HexColor)
            .WithMessage(ValidationConstants.HexColorError);

    public static IRuleBuilderOptions<T, List<Guid>> DoesNotContainDuplicates<T>(this IRuleBuilder<T, List<Guid>> rule) =>
        rule.Must(ids => ids.Count == ids.Distinct().Count())
            .WithMessage("{PropertyName} cannot contain duplicate IDs.");
}

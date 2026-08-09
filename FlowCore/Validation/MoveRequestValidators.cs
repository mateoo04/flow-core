using FlowCore.Models.ViewModels;
using FluentValidation;

namespace FlowCore.Validation;

public sealed class MoveTaskRequestValidator : AbstractValidator<MoveTaskRequest>
{
    public MoveTaskRequestValidator()
    {
        RuleFor(x => x.StatusId).NotEmptyGuid();
        RuleFor(x => x.Position).GreaterThanOrEqualTo(0);
    }
}

public sealed class MoveOnHomeRequestValidator : AbstractValidator<MoveOnHomeRequest>
{
    public MoveOnHomeRequestValidator()
    {
        RuleFor(x => x.StatusName).NotBlank();
        RuleFor(x => x.Position).GreaterThanOrEqualTo(0);
    }
}

using FlowCore.Models;
using FlowCore.Models.Dtos;
using FlowCore.Models.ViewModels;
using FluentValidation;

namespace FlowCore.Validation;

public sealed class TagFormValidator : AbstractValidator<TagFormVm>
{
    public TagFormValidator() => Include(new TagFieldsValidator<TagFormVm>());
}

public sealed class TagCreateDtoValidator : AbstractValidator<TagCreateDto>
{
    public TagCreateDtoValidator() => Include(new TagFieldsValidator<TagCreateDto>());
}

public sealed class TagUpdateDtoValidator : AbstractValidator<TagUpdateDto>
{
    public TagUpdateDtoValidator() => Include(new TagFieldsValidator<TagUpdateDto>());
}

public sealed class TaskStatusFormValidator : AbstractValidator<TaskStatusFormVm>
{
    public TaskStatusFormValidator() => Include(new StatusFieldsValidator<TaskStatusFormVm>());
}

public sealed class StatusCreateDtoValidator : AbstractValidator<StatusCreateDto>
{
    public StatusCreateDtoValidator()
    {
        Include(new StatusFieldsValidator<StatusCreateDto>());
        RuleFor(x => x.WorkspaceId).NotEmptyGuid();
        RuleFor(x => x.Position).GreaterThanOrEqualTo(0);
    }
}

public sealed class StatusUpdateDtoValidator : AbstractValidator<StatusUpdateDto>
{
    public StatusUpdateDtoValidator()
    {
        Include(new StatusFieldsValidator<StatusUpdateDto>());
        RuleFor(x => x.Position).GreaterThanOrEqualTo(0);
    }
}

internal sealed class TagFieldsValidator<T> : AbstractValidator<T>
    where T : ITagInput
{
    public TagFieldsValidator()
    {
        RuleFor(x => x.Name)
            .NotBlank()
            .MaximumLength(ValidationConstants.TagNameMaxLength);

        RuleFor(x => x.ColorHex).HexColor();
    }
}

internal sealed class StatusFieldsValidator<T> : AbstractValidator<T>
    where T : IStatusInput
{
    public StatusFieldsValidator()
    {
        RuleFor(x => x.Name)
            .NotBlank()
            .MaximumLength(ValidationConstants.BoardNameMaxLength);

        RuleFor(x => x.ColorHex).HexColor();
    }
}

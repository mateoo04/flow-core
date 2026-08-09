using FlowCore.Models;
using FlowCore.Models.Dtos;
using FlowCore.Models.ViewModels;
using FluentValidation;

namespace FlowCore.Validation;

public sealed class TaskCreateFormValidator : AbstractValidator<TaskCreateFormVm>
{
    public TaskCreateFormValidator()
    {
        Include(new TaskFieldsValidator<TaskCreateFormVm>());
        RuleFor(x => x.ProjectId).NotEmptyGuid();
        RuleFor(x => x.BoardId).NotEmptyGuid();
    }
}

public sealed class TaskEditFormValidator : AbstractValidator<TaskEditFormVm>
{
    public TaskEditFormValidator()
    {
        Include(new TaskFieldsValidator<TaskEditFormVm>());
        RuleFor(x => x.Id).NotEmptyGuid();
    }
}

public sealed class TaskCreateDtoValidator : AbstractValidator<TaskCreateDto>
{
    public TaskCreateDtoValidator()
    {
        Include(new TaskFieldsValidator<TaskCreateDto>());
        RuleFor(x => x.BoardId).NotEmptyGuid();
        RuleFor(x => x.TagIds).DoesNotContainDuplicates();
    }
}

public sealed class TaskUpdateDtoValidator : AbstractValidator<TaskUpdateDto>
{
    public TaskUpdateDtoValidator()
    {
        Include(new TaskFieldsValidator<TaskUpdateDto>());
        RuleFor(x => x.TagIds).DoesNotContainDuplicates();
    }
}

public sealed class CommentFormValidator : AbstractValidator<CommentFormVm>
{
    public CommentFormValidator() => RuleFor(x => x.Body)
        .NotBlank()
        .MaximumLength(ValidationConstants.CommentBodyMaxLength);
}

public sealed class CommentCreateDtoValidator : AbstractValidator<CommentCreateDto>
{
    public CommentCreateDtoValidator()
    {
        RuleFor(x => x.TaskItemId).NotEmptyGuid();
        RuleFor(x => x.AuthorUserId).NotEmptyGuid();
        RuleFor(x => x.Body)
            .NotBlank()
            .MaximumLength(ValidationConstants.CommentBodyMaxLength);
    }
}

public sealed class CommentUpdateDtoValidator : AbstractValidator<CommentUpdateDto>
{
    public CommentUpdateDtoValidator() => RuleFor(x => x.Body)
        .NotBlank()
        .MaximumLength(ValidationConstants.CommentBodyMaxLength);
}

internal sealed class TaskFieldsValidator<T> : AbstractValidator<T>
    where T : ITaskInput
{
    public TaskFieldsValidator()
    {
        RuleFor(x => x.TaskStatusDefinitionId).NotEmptyGuid();

        RuleFor(x => x.Title)
            .NotBlank()
            .MaximumLength(ValidationConstants.TaskTitleMaxLength);

        RuleFor(x => x.Description)
            .MaximumLength(ValidationConstants.DescriptionMaxLength);

        RuleFor(x => x.StoryPoints)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.AssigneeIds)
            .DoesNotContainDuplicates();
    }
}

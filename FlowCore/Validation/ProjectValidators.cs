using FlowCore.Models;
using FlowCore.Models.Dtos;
using FlowCore.Models.ViewModels;
using FluentValidation;

namespace FlowCore.Validation;

public sealed class ProjectCreateFormValidator : AbstractValidator<ProjectCreateFormVm>
{
    public ProjectCreateFormValidator()
    {
        Include(new ProjectFieldsValidator<ProjectCreateFormVm>());
        RuleFor(x => x.WorkspaceId).NotEmptyGuid();
    }
}

public sealed class ProjectEditFormValidator : AbstractValidator<ProjectEditFormVm>
{
    public ProjectEditFormValidator()
    {
        Include(new ProjectFieldsValidator<ProjectEditFormVm>());
        RuleFor(x => x.Id).NotEmptyGuid();
    }
}

public sealed class ProjectCreateDtoValidator : AbstractValidator<ProjectCreateDto>
{
    public ProjectCreateDtoValidator()
    {
        Include(new ProjectFieldsValidator<ProjectCreateDto>());
        RuleFor(x => x.WorkspaceId).NotEmptyGuid();
    }
}

public sealed class ProjectUpdateDtoValidator : AbstractValidator<ProjectUpdateDto>
{
    public ProjectUpdateDtoValidator() => Include(new ProjectFieldsValidator<ProjectUpdateDto>());
}

internal sealed class ProjectFieldsValidator<T> : AbstractValidator<T>
    where T : IProjectInput
{
    public ProjectFieldsValidator()
    {
        RuleFor(x => x.Name)
            .NotBlank()
            .MaximumLength(ValidationConstants.ProjectNameMaxLength);

        RuleFor(x => x.Description)
            .MaximumLength(ValidationConstants.DescriptionMaxLength);

        RuleFor(x => x)
            .Must(x => x.StartDate is null || x.DueDate is null || x.StartDate <= x.DueDate)
            .WithMessage("Start date must be on or before due date.");
    }
}

using FlowCore.Models.Dtos;
using FlowCore.Models.ViewModels;
using FlowCore.Validation;
using Xunit;

namespace FlowCore.Tests.Validation;

public class InputValidatorTests
{
    [Fact]
    public void ProjectValidator_RejectsDateRange_WhenStartDateIsAfterDueDate()
    {
        var validator = new ProjectCreateDtoValidator();
        var result = validator.Validate(new ProjectCreateDto
        {
            WorkspaceId = Guid.NewGuid(),
            Name = "Project",
            StartDate = new DateTime(2026, 6, 2),
            DueDate = new DateTime(2026, 6, 1)
        });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void TaskValidator_RejectsNegativeStoryPoints()
    {
        var validator = new TaskCreateDtoValidator();
        var result = validator.Validate(new TaskCreateDto
        {
            BoardId = Guid.NewGuid(),
            TaskStatusDefinitionId = Guid.NewGuid(),
            Title = "Task",
            StoryPoints = -1
        });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void TaskValidator_RejectsDuplicateAssigneesAndTags()
    {
        var duplicateId = Guid.NewGuid();
        var validator = new TaskCreateDtoValidator();
        var result = validator.Validate(new TaskCreateDto
        {
            BoardId = Guid.NewGuid(),
            TaskStatusDefinitionId = Guid.NewGuid(),
            Title = "Task",
            AssigneeIds = new List<Guid> { duplicateId, duplicateId },
            TagIds = new List<Guid> { duplicateId, duplicateId }
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(TaskCreateDto.AssigneeIds));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(TaskCreateDto.TagIds));
    }

    [Fact]
    public void TagValidator_RejectsInvalidColor()
    {
        var validator = new TagFormValidator();
        var result = validator.Validate(new TagFormVm { Name = "Bug", ColorHex = "red" });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void StatusValidator_RejectsBlankColor()
    {
        var validator = new TaskStatusFormValidator();
        var result = validator.Validate(new TaskStatusFormVm { Name = "Doing", ColorHex = "" });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void MoveValidator_RejectsEmptyStatusAndNegativePosition()
    {
        var validator = new MoveTaskRequestValidator();
        var result = validator.Validate(new MoveTaskRequest { StatusId = Guid.Empty, Position = -1 });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void CommentValidator_RejectsBlankBody()
    {
        var validator = new CommentFormValidator();
        var result = validator.Validate(new CommentFormVm { Body = "   " });

        Assert.False(result.IsValid);
    }
}

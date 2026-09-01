namespace FlowCore.Models.Dtos;

public static class ApiMappingExtensions
{
    public static WorkspaceSummaryDto ToSummaryDto(this Workspace workspace) =>
        new(workspace.Id, workspace.Name);

    public static StatusSummaryDto ToSummaryDto(this TaskStatusDefinition status) =>
        new(status.Id, status.Name, status.ColorHex);

    public static UserSummaryDto ToSummaryDto(this User user) =>
        new(user.Id, user.FullName, user.Email);

    public static TagDto ToDto(this Tag tag) =>
        new(tag.Id, tag.Name, tag.ColorHex);

    public static WorkspaceDto ToDto(this Workspace workspace) =>
        new(
            workspace.Id,
            workspace.Name,
            workspace.Description,
            workspace.CreatedAt);

    public static ProjectDto ToDto(this Project project) =>
        new(
            project.Id,
            project.WorkspaceId,
            project.Name,
            project.Description,
            project.StartDate,
            project.DueDate,
            project.Status,
            project.Priority,
            project.Workspace?.ToSummaryDto());

    public static BoardDto ToDto(this Board board) =>
        new(
            board.Id,
            board.ProjectId,
            board.Name,
            board.Position,
            board.IsDefault,
            board.CreatedAt,
            board.UpdatedAt);

    public static StatusDto ToDto(this TaskStatusDefinition status) =>
        new(
            status.Id,
            status.WorkspaceId,
            status.Name,
            status.ColorHex,
            status.Position,
            status.IsDoneState,
            status.CreatedAt);

    public static TaskItemDto ToDto(this TaskItem task) =>
        new(
            task.Id,
            task.BoardId,
            task.Title,
            task.Description,
            task.TaskStatusDefinitionId,
            task.TaskStatusDefinition?.ToSummaryDto(),
            task.Priority,
            task.StoryPoints,
            task.Position,
            task.ParentTaskItemId,
            task.CreatedAt,
            task.UpdatedAt,
            task.DueDate,
            task.TaskAssignments
                .Where(a => a.User != null)
                .Select(a => a.User!.ToSummaryDto())
                .ToList(),
            task.TaskTags
                .Where(tt => tt.Tag != null)
                .Select(tt => tt.Tag!.ToDto())
                .ToList());

    public static CommentDto ToDto(this Comment comment) =>
        new(
            comment.Id,
            comment.TaskItemId,
            comment.AuthorUserId,
            comment.Author?.ToSummaryDto(),
            comment.Body,
            comment.CreatedAt,
            comment.EditedAt);
}

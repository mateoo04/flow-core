using FlowCore.Models;

namespace FlowCore.Data;

public sealed class ProjectBoardContext
{
    public required Project Project { get; init; }
    public required Board Board { get; init; }
    public required TaskStatusDefinition Backlog { get; init; }
    public required TaskStatusDefinition Todo { get; init; }
    public required TaskStatusDefinition InProgress { get; init; }
    public required TaskStatusDefinition Done { get; init; }
}

public sealed class WorkspaceStatuses
{
    public required TaskStatusDefinition Backlog { get; init; }
    public required TaskStatusDefinition Todo { get; init; }
    public required TaskStatusDefinition InProgress { get; init; }
    public required TaskStatusDefinition Done { get; init; }

    public IReadOnlyList<TaskStatusDefinition> All => new[] { Backlog, Todo, InProgress, Done };
}

public static class ProjectBlueprint
{
    public static WorkspaceStatuses CreateWorkspaceStatuses(Guid workspaceId, DateTime now, Func<Guid> newId)
    {
        return new WorkspaceStatuses
        {
            Backlog = new TaskStatusDefinition
            {
                Id = newId(),
                WorkspaceId = workspaceId,
                Name = "Backlog",
                ColorHex = "#94A3B8",
                Position = 0,
                IsDoneState = false,
                CreatedAt = now
            },
            Todo = new TaskStatusDefinition
            {
                Id = newId(),
                WorkspaceId = workspaceId,
                Name = "Todo",
                ColorHex = "#3B82F6",
                Position = 1,
                IsDoneState = false,
                CreatedAt = now
            },
            InProgress = new TaskStatusDefinition
            {
                Id = newId(),
                WorkspaceId = workspaceId,
                Name = "In progress",
                ColorHex = "#F59E0B",
                Position = 2,
                IsDoneState = false,
                CreatedAt = now
            },
            Done = new TaskStatusDefinition
            {
                Id = newId(),
                WorkspaceId = workspaceId,
                Name = "Done",
                ColorHex = "#22C55E",
                Position = 3,
                IsDoneState = true,
                CreatedAt = now
            }
        };
    }

    /// <summary>
    /// Builds a project with a single board. Statuses are scoped to the workspace and supplied by the caller. Does not add the project to <paramref name="ws"/> — caller must add <c>ctx.Project</c> to the workspace.
    /// </summary>
    public static ProjectBoardContext CreateProject(
        Workspace ws,
        Func<Guid> ng,
        DateTime now,
        WorkspaceStatuses statuses,
        string name,
        string description,
        ProjectStatus status,
        ProjectPriority priority)
    {
        var project = new Project
        {
            Id = ng(),
            WorkspaceId = ws.Id,
            Name = name,
            Description = description,
            StartDate = now.AddDays(-21),
            DueDate = now.AddMonths(3),
            Status = status,
            Priority = priority
        };

        project.Workspace = ws;

        var board = new Board
        {
            Id = ng(),
            ProjectId = project.Id,
            Name = "Delivery board",
            Position = 0,
            IsDefault = true,
            CreatedAt = now.AddDays(-14),
            UpdatedAt = now
        };
        board.Project = project;

        project.Boards.Add(board);

        return new ProjectBoardContext
        {
            Project = project,
            Board = board,
            Backlog = statuses.Backlog,
            Todo = statuses.Todo,
            InProgress = statuses.InProgress,
            Done = statuses.Done
        };
    }
}

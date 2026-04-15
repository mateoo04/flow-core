using FlowCore.Models;

namespace FlowCore.Data;

public sealed class ProjectBoardContext
{
    public required Project Project { get; init; }
    public required Board Board { get; init; }
    public required BoardColumn ColTodo { get; init; }
    public required BoardColumn ColDoing { get; init; }
    public required BoardColumn ColDone { get; init; }
    public required List<TaskStatusDefinition> StatusDefs { get; init; }

    public TaskStatusDefinition Backlog => StatusDefs[0];
    public TaskStatusDefinition Todo => StatusDefs[1];
    public TaskStatusDefinition InProgress => StatusDefs[2];
    public TaskStatusDefinition Done => StatusDefs[3];
}

public static class ProjectBlueprint
{
    public static List<TaskStatusDefinition> CreateDefaultStatuses(Guid projectId, DateTime now, Func<Guid> newId)
    {
        return
        [
            new TaskStatusDefinition
            {
                Id = newId(),
                ProjectId = projectId,
                Name = "Backlog",
                ColorHex = "#94A3B8",
                Position = 0,
                IsDefault = false,
                IsDoneState = false,
                CreatedAt = now
            },
            new TaskStatusDefinition
            {
                Id = newId(),
                ProjectId = projectId,
                Name = "Todo",
                ColorHex = "#3B82F6",
                Position = 1,
                IsDefault = true,
                IsDoneState = false,
                CreatedAt = now
            },
            new TaskStatusDefinition
            {
                Id = newId(),
                ProjectId = projectId,
                Name = "In progress",
                ColorHex = "#F59E0B",
                Position = 2,
                IsDefault = false,
                IsDoneState = false,
                CreatedAt = now
            },
            new TaskStatusDefinition
            {
                Id = newId(),
                ProjectId = projectId,
                Name = "Done",
                ColorHex = "#22C55E",
                Position = 3,
                IsDefault = false,
                IsDoneState = true,
                CreatedAt = now
            }
        ];
    }

    /// <summary>
    /// Builds a project with default statuses and a three-column board. Does not add the project to <paramref name="ws"/> — caller must add <c>ctx.Project</c> to the workspace.
    /// </summary>
    public static ProjectBoardContext CreateProject(
        Workspace ws,
        Func<Guid> ng,
        DateTime now,
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

        var statusDefs = CreateDefaultStatuses(project.Id, now, ng);
        foreach (var s in statusDefs)
        {
            s.Project = project;
            project.TaskStatusDefinitions.Add(s);
        }

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

        var colTodo = new BoardColumn
        {
            Id = ng(),
            BoardId = board.Id,
            Name = "To do",
            Position = 0,
            WipLimit = 0,
            IsDoneColumn = false,
            ColorHex = "#94a3b8",
            CreatedAt = now.AddDays(-14),
            Board = board
        };
        var colDoing = new BoardColumn
        {
            Id = ng(),
            BoardId = board.Id,
            Name = "Doing",
            Position = 1,
            WipLimit = 5,
            IsDoneColumn = false,
            ColorHex = "#3b82f6",
            CreatedAt = now.AddDays(-14),
            Board = board
        };
        var colDone = new BoardColumn
        {
            Id = ng(),
            BoardId = board.Id,
            Name = "Done",
            Position = 2,
            WipLimit = 0,
            IsDoneColumn = true,
            ColorHex = "#10b981",
            CreatedAt = now.AddDays(-14),
            Board = board
        };

        board.Columns.Add(colTodo);
        board.Columns.Add(colDoing);
        board.Columns.Add(colDone);
        project.Boards.Add(board);

        return new ProjectBoardContext
        {
            Project = project,
            Board = board,
            ColTodo = colTodo,
            ColDoing = colDoing,
            ColDone = colDone,
            StatusDefs = statusDefs
        };
    }
}

using FlowCore.Models;

namespace FlowCore.Data;

public sealed class DemoDataGraph
{
    public IReadOnlyList<Workspace> Workspaces { get; init; } = Array.Empty<Workspace>();
    public IReadOnlyList<User> Users { get; init; } = Array.Empty<User>();
    public IReadOnlyList<Tag> Tags { get; init; } = Array.Empty<Tag>();
}

public static class DemoDataBuilder
{
    public static DemoDataGraph CreateSampleGraph()
    {
        var now = new DateTime(2026, 4, 1, 12, 0, 0, DateTimeKind.Utc);
        Guid Ng() => Guid.NewGuid();

        var ownerAlex = new User
        {
            Id = DemoSeedIds.UserAlex,
            FullName = "Alex Owner",
            Email = "alex@flowcore.demo",
            JoinedAt = now.AddMonths(-6),
            IsActive = true
        };

        var memberSam = new User
        {
            Id = DemoSeedIds.UserSam,
            FullName = "Sam Member",
            Email = "sam@flowcore.demo",
            JoinedAt = now.AddMonths(-3),
            IsActive = true
        };

        var users = new List<User> { ownerAlex, memberSam };

        var tagUi = new Tag { Id = DemoSeedIds.TagUi, Name = "ui", ColorHex = "#6366F1" };
        var tagBug = new Tag { Id = DemoSeedIds.TagBug, Name = "bug", ColorHex = "#EF4444" };
        var tags = new List<Tag> { tagUi, tagBug };

        var workspaces = new List<Workspace>();
        var workspaceNames = new[] { "North Division", "South Division", "Platform Guild" };
        var workspaceIds = new[]
        {
            DemoSeedIds.WorkspaceNorth, DemoSeedIds.WorkspaceSouth, DemoSeedIds.WorkspacePlatform
        };

        for (var w = 0; w < 3; w++)
        {
            var workspace = new Workspace
            {
                Id = workspaceIds[w],
                Name = workspaceNames[w],
                Description = $"Workspace #{w + 1} — multi-project delivery.",
                CreatedAt = now.AddDays(-30 * (w + 1)),
                ArchivedAt = null,
                Visibility = w == 0 ? WorkspaceVisibility.Team : WorkspaceVisibility.Private,
                OwnerUserId = w % 2 == 0 ? ownerAlex.Id : memberSam.Id
            };

            for (var p = 0; p < 3; p++)
            {
                var project = new Project
                {
                    Id = Ng(),
                    WorkspaceId = workspace.Id,
                    Name = $"{workspace.Name[..Math.Min(4, workspace.Name.Length)]} Product {p + 1}",
                    Key = $"WS{w + 1}-P{p + 1}",
                    Description = "Sample project with board, custom statuses, and tasks.",
                    StartDate = now.AddDays(-14),
                    DueDate = now.AddMonths(2),
                    Status = p == 0 ? ProjectStatus.Active : ProjectStatus.Planning,
                    Priority = (ProjectPriority)(p % 4)
                };

                project.Workspace = workspace;

                var statusDefs = CreateDefaultStatuses(project.Id, now);
                foreach (var s in statusDefs)
                {
                    s.Project = project;
                    project.TaskStatusDefinitions.Add(s);
                }

                var backlogId = statusDefs[0].Id;
                var todoId = statusDefs[1].Id;
                var inProgressId = statusDefs[2].Id;

                var board = new Board
                {
                    Id = Ng(),
                    ProjectId = project.Id,
                    Name = "Delivery board",
                    Position = 0,
                    IsDefault = true,
                    CreatedAt = now.AddDays(-10),
                    UpdatedAt = now
                };
                board.Project = project;

                var colTodo = new BoardColumn
                {
                    Id = Ng(),
                    BoardId = board.Id,
                    Name = "To do",
                    Position = 0,
                    WipLimit = 0,
                    IsDoneColumn = false,
                    CreatedAt = now.AddDays(-10),
                    Board = board
                };
                var colDoing = new BoardColumn
                {
                    Id = Ng(),
                    BoardId = board.Id,
                    Name = "Doing",
                    Position = 1,
                    WipLimit = 3,
                    IsDoneColumn = false,
                    CreatedAt = now.AddDays(-10),
                    Board = board
                };
                var colDone = new BoardColumn
                {
                    Id = Ng(),
                    BoardId = board.Id,
                    Name = "Done",
                    Position = 2,
                    WipLimit = 0,
                    IsDoneColumn = true,
                    CreatedAt = now.AddDays(-10),
                    Board = board
                };

                board.Columns.Add(colTodo);
                board.Columns.Add(colDoing);
                board.Columns.Add(colDone);

                var parentTask = new TaskItem
                {
                    Id = Ng(),
                    BoardColumnId = colTodo.Id,
                    Title = w == 0 && p == 0 ? "Product page" : $"Epic task W{w + 1}-P{p + 1}",
                    Description = "Parent task with subtasks.",
                    TaskStatusDefinitionId = todoId,
                    Priority = TaskPriority.High,
                    StoryPoints = 5,
                    ParentTaskItemId = null,
                    CreatedAt = now.AddDays(-5),
                    UpdatedAt = now,
                    DueDate = now.AddDays(14),
                    BoardColumn = colTodo,
                    TaskStatusDefinition = statusDefs[1]
                };

                var subA = new TaskItem
                {
                    Id = Ng(),
                    BoardColumnId = colTodo.Id,
                    Title = "Wireframe hero section",
                    Description = string.Empty,
                    TaskStatusDefinitionId = inProgressId,
                    Priority = TaskPriority.Medium,
                    StoryPoints = 2,
                    ParentTaskItemId = parentTask.Id,
                    CreatedAt = now.AddDays(-4),
                    UpdatedAt = now,
                    DueDate = null,
                    BoardColumn = colTodo,
                    TaskStatusDefinition = statusDefs[2],
                    ParentTaskItem = parentTask
                };

                var subB = new TaskItem
                {
                    Id = Ng(),
                    BoardColumnId = colTodo.Id,
                    Title = "Hook up CMS fields",
                    Description = string.Empty,
                    TaskStatusDefinitionId = backlogId,
                    Priority = TaskPriority.Low,
                    StoryPoints = 1,
                    ParentTaskItemId = parentTask.Id,
                    CreatedAt = now.AddDays(-4),
                    UpdatedAt = now,
                    DueDate = null,
                    BoardColumn = colTodo,
                    TaskStatusDefinition = statusDefs[0],
                    ParentTaskItem = parentTask
                };

                parentTask.Subtasks.Add(subA);
                parentTask.Subtasks.Add(subB);

                var sideTask = new TaskItem
                {
                    Id = Ng(),
                    BoardColumnId = colDoing.Id,
                    Title = "QA regression sweep",
                    Description = "Cross-browser checks.",
                    TaskStatusDefinitionId = inProgressId,
                    Priority = TaskPriority.Medium,
                    StoryPoints = 3,
                    ParentTaskItemId = null,
                    CreatedAt = now.AddDays(-2),
                    UpdatedAt = now,
                    DueDate = now.AddDays(7),
                    BoardColumn = colDoing,
                    TaskStatusDefinition = statusDefs[2]
                };

                colTodo.Tasks.Add(parentTask);
                colTodo.Tasks.Add(subA);
                colTodo.Tasks.Add(subB);
                colDoing.Tasks.Add(sideTask);

                foreach (var t in new[] { parentTask, subA, subB, sideTask })
                {
                    t.TaskStatusDefinition!.TaskItems.Add(t);
                }

                var assignParent = new TaskAssignment
                {
                    TaskItemId = parentTask.Id,
                    UserId = ownerAlex.Id,
                    Role = TaskRole.Assignee,
                    AssignedAt = now.AddDays(-5),
                    TaskItem = parentTask,
                    User = ownerAlex
                };
                var assignSide = new TaskAssignment
                {
                    TaskItemId = sideTask.Id,
                    UserId = memberSam.Id,
                    Role = TaskRole.Assignee,
                    AssignedAt = now.AddDays(-2),
                    TaskItem = sideTask,
                    User = memberSam
                };
                var assignSub = new TaskAssignment
                {
                    TaskItemId = subA.Id,
                    UserId = memberSam.Id,
                    Role = TaskRole.Reviewer,
                    AssignedAt = now.AddDays(-3),
                    TaskItem = subA,
                    User = memberSam
                };

                parentTask.TaskAssignments.Add(assignParent);
                sideTask.TaskAssignments.Add(assignSide);
                subA.TaskAssignments.Add(assignSub);
                ownerAlex.TaskAssignments.Add(assignParent);
                memberSam.TaskAssignments.Add(assignSide);
                memberSam.TaskAssignments.Add(assignSub);

                var linkUi = new TaskTag
                {
                    TaskItemId = parentTask.Id,
                    TagId = tagUi.Id,
                    LinkedAt = now.AddDays(-4),
                    TaskItem = parentTask,
                    Tag = tagUi
                };
                var linkBug = new TaskTag
                {
                    TaskItemId = sideTask.Id,
                    TagId = tagBug.Id,
                    LinkedAt = now.AddDays(-1),
                    TaskItem = sideTask,
                    Tag = tagBug
                };
                parentTask.TaskTags.Add(linkUi);
                tagUi.TaskTags.Add(linkUi);
                sideTask.TaskTags.Add(linkBug);
                tagBug.TaskTags.Add(linkBug);

                var comment = new Comment
                {
                    Id = Ng(),
                    TaskItemId = parentTask.Id,
                    AuthorUserId = memberSam.Id,
                    Body = "Subtasks look good — prioritize CMS hookup.",
                    CreatedAt = now.AddDays(-3),
                    EditedAt = null,
                    TaskItem = parentTask
                };
                parentTask.Comments.Add(comment);

                project.Boards.Add(board);
                workspace.Projects.Add(project);
            }

            workspaces.Add(workspace);
        }

        return new DemoDataGraph
        {
            Workspaces = workspaces,
            Users = users,
            Tags = tags
        };
    }

    private static List<TaskStatusDefinition> CreateDefaultStatuses(Guid projectId, DateTime now)
    {
        Guid Ng() => Guid.NewGuid();

        return
        [
            new TaskStatusDefinition
            {
                Id = Ng(),
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
                Id = Ng(),
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
                Id = Ng(),
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
                Id = Ng(),
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
}

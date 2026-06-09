using FlowCore.Data;
using FlowCore.Models;
using Microsoft.EntityFrameworkCore;

namespace FlowCore.Tests.Infrastructure;

// Builds minimal valid object graphs directly in the DbContext, mirroring the lab's
// CreateQuizAsync helper pattern. Every entity uses a fresh Guid so tests sharing a
// class-level database don't collide.
public static class TestDataSeeder
{
    public static async Task<Tag> CreateTagAsync(FlowCoreDbContext db, string? name = null)
    {
        var tag = new Tag { Id = Guid.NewGuid(), Name = name ?? $"tag-{Guid.NewGuid():N}", ColorHex = "#ff0000" };
        db.Tags.Add(tag);
        await db.SaveChangesAsync();
        return tag;
    }

    public static async Task<Workspace> CreateWorkspaceAsync(FlowCoreDbContext db, string? name = null)
    {
        var workspace = new Workspace
        {
            Id = Guid.NewGuid(),
            Name = name ?? $"ws-{Guid.NewGuid():N}",
            Description = "seeded",
            CreatedAt = DateTime.UtcNow,
            Visibility = WorkspaceVisibility.Private
        };
        db.Workspaces.Add(workspace);
        await db.SaveChangesAsync();
        return workspace;
    }

    public static async Task<TaskStatusDefinition> CreateStatusAsync(FlowCoreDbContext db, Workspace workspace)
    {
        var status = new TaskStatusDefinition
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspace.Id,
            Name = $"status-{Guid.NewGuid():N}",
            ColorHex = "#00ff00",
            Position = 0,
            IsDoneState = false,
            CreatedAt = DateTime.UtcNow
        };
        db.TaskStatusDefinitions.Add(status);
        await db.SaveChangesAsync();
        return status;
    }

    public static async Task<Project> CreateProjectAsync(FlowCoreDbContext db, Workspace workspace)
    {
        var project = new Project
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspace.Id,
            Name = $"proj-{Guid.NewGuid():N}",
            Description = "seeded",
            StartDate = DateTime.UtcNow,
            Status = ProjectStatus.Active,
            Priority = ProjectPriority.Medium
        };
        db.Projects.Add(project);
        await db.SaveChangesAsync();
        return project;
    }

    public static async Task<Board> CreateBoardAsync(FlowCoreDbContext db, Project project)
    {
        var now = DateTime.UtcNow;
        var board = new Board
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Name = $"board-{Guid.NewGuid():N}",
            Position = 0,
            IsDefault = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.Boards.Add(board);
        await db.SaveChangesAsync();
        return board;
    }

    public static async Task<User> EnsureTestUserAsync(FlowCoreDbContext db)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == TestAuth.UserId);
        if (user is not null)
            return user;

        user = new User
        {
            Id = TestAuth.UserId,
            UserName = TestAuth.Email,
            NormalizedUserName = TestAuth.Email.ToUpperInvariant(),
            Email = TestAuth.Email,
            NormalizedEmail = TestAuth.Email.ToUpperInvariant(),
            FullName = TestAuth.FullName,
            JoinedAt = DateTime.UtcNow,
            IsActive = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    // Workspace -> Project -> Board (+ a Status in the same workspace) needed to create tasks.
    public sealed record TaskContext(Workspace Workspace, Project Project, Board Board, TaskStatusDefinition Status);

    public static async Task<TaskContext> CreateTaskContextAsync(FlowCoreDbContext db)
    {
        var workspace = await CreateWorkspaceAsync(db);
        var project = await CreateProjectAsync(db, workspace);
        var board = await CreateBoardAsync(db, project);
        var status = await CreateStatusAsync(db, workspace);
        return new TaskContext(workspace, project, board, status);
    }

    public static async Task<TaskItem> CreateTaskAsync(FlowCoreDbContext db, TaskContext? context = null)
    {
        context ??= await CreateTaskContextAsync(db);
        var now = DateTime.UtcNow;
        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            BoardId = context.Board.Id,
            TaskStatusDefinitionId = context.Status.Id,
            Title = $"task-{Guid.NewGuid():N}",
            Description = "seeded",
            Priority = TaskPriority.Medium,
            StoryPoints = 1,
            Position = 0,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.TaskItems.Add(task);
        await db.SaveChangesAsync();
        return task;
    }

    public static async Task<Comment> CreateCommentAsync(FlowCoreDbContext db)
    {
        var user = await EnsureTestUserAsync(db);
        var task = await CreateTaskAsync(db);
        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            TaskItemId = task.Id,
            AuthorUserId = user.Id,
            Body = "seeded comment",
            CreatedAt = DateTime.UtcNow
        };
        db.Comments.Add(comment);
        await db.SaveChangesAsync();
        return comment;
    }
}

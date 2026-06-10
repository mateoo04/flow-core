using FlowCore.Data;
using Microsoft.EntityFrameworkCore;

namespace FlowCore.Tests.Infrastructure;

public static class DbAssert
{
    public static Task<bool> TagExistsAsync(FlowCoreDbContext db, Guid id) =>
        db.Tags.AnyAsync(t => t.Id == id);

    public static Task<bool> WorkspaceExistsAsync(FlowCoreDbContext db, Guid id) =>
        db.Workspaces.AnyAsync(w => w.Id == id);

    public static Task<bool> ProjectExistsAsync(FlowCoreDbContext db, Guid id) =>
        db.Projects.AnyAsync(p => p.Id == id);

    public static Task<bool> BoardExistsAsync(FlowCoreDbContext db, Guid id) =>
        db.Boards.AnyAsync(b => b.Id == id);

    public static Task<bool> StatusExistsAsync(FlowCoreDbContext db, Guid id) =>
        db.TaskStatusDefinitions.AnyAsync(s => s.Id == id);

    public static Task<bool> TaskExistsAsync(FlowCoreDbContext db, Guid id) =>
        db.TaskItems.AnyAsync(t => t.Id == id);

    public static Task<bool> CommentExistsAsync(FlowCoreDbContext db, Guid id) =>
        db.Comments.AnyAsync(c => c.Id == id);
}

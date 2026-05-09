using FlowCore.Models;
using Microsoft.EntityFrameworkCore;

namespace FlowCore.Data;

public class FlowCoreDbContext : DbContext
{
    public FlowCoreDbContext(DbContextOptions<FlowCoreDbContext> options)
        : base(options)
    {
    }

    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Board> Boards => Set<Board>();
    public DbSet<TaskItem> TaskItems => Set<TaskItem>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<TaskStatusDefinition> TaskStatusDefinitions => Set<TaskStatusDefinition>();
    public DbSet<TaskAssignment> TaskAssignments => Set<TaskAssignment>();
    public DbSet<TaskTag> TaskTags => Set<TaskTag>();
    public DbSet<UserTaskOrder> UserTaskOrders => Set<UserTaskOrder>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(b =>
        {
            b.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<Workspace>(b =>
        {
            b.HasIndex(w => w.Name);
            b.HasOne(w => w.Owner)
                .WithMany(u => u.OwnedWorkspaces)
                .HasForeignKey(w => w.OwnerUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Project>(b =>
        {
            b.HasIndex(p => p.WorkspaceId);
            b.HasOne(p => p.Workspace)
                .WithMany(w => w.Projects)
                .HasForeignKey(p => p.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Board>(b =>
        {
            b.HasIndex(x => x.ProjectId);
            b.HasOne(x => x.Project)
                .WithMany(p => p.Boards)
                .HasForeignKey(x => x.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TaskStatusDefinition>(b =>
        {
            b.HasIndex(s => s.WorkspaceId);
            b.HasOne(s => s.Workspace)
                .WithMany(w => w.TaskStatusDefinitions)
                .HasForeignKey(s => s.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TaskItem>(b =>
        {
            b.HasIndex(t => t.BoardId);
            b.HasIndex(t => t.ParentTaskItemId);
            b.HasIndex(t => new { t.TaskStatusDefinitionId, t.Position });

            b.HasOne(t => t.Board)
                .WithMany(brd => brd.Tasks)
                .HasForeignKey(t => t.BoardId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(t => t.TaskStatusDefinition)
                .WithMany(s => s.TaskItems)
                .HasForeignKey(t => t.TaskStatusDefinitionId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(t => t.ParentTaskItem)
                .WithMany(t => t.Subtasks)
                .HasForeignKey(t => t.ParentTaskItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Comment>(b =>
        {
            b.HasIndex(c => c.TaskItemId);
            b.HasOne(c => c.TaskItem)
                .WithMany(t => t.Comments)
                .HasForeignKey(c => c.TaskItemId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(c => c.Author)
                .WithMany()
                .HasForeignKey(c => c.AuthorUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TaskAssignment>(b =>
        {
            b.HasOne(a => a.TaskItem)
                .WithMany(t => t.TaskAssignments)
                .HasForeignKey(a => a.TaskItemId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(a => a.User)
                .WithMany(u => u.TaskAssignments)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TaskTag>(b =>
        {
            b.HasOne(tt => tt.TaskItem)
                .WithMany(t => t.TaskTags)
                .HasForeignKey(tt => tt.TaskItemId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(tt => tt.Tag)
                .WithMany(t => t.TaskTags)
                .HasForeignKey(tt => tt.TagId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserTaskOrder>(b =>
        {
            b.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(x => x.TaskItem)
                .WithMany()
                .HasForeignKey(x => x.TaskItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

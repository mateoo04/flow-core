using FlowCore.Models;
using Microsoft.EntityFrameworkCore;

namespace FlowCore.Data;

public class ClientManagerDbContext : DbContext
{
    public ClientManagerDbContext(DbContextOptions<ClientManagerDbContext> options)
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
}

using FlowCore.Data;
using FlowCore.Models;
using Microsoft.EntityFrameworkCore;

namespace FlowCore.Repositories.EntityFramework;

public sealed class EfWorkspaceRepository : IWorkspaceRepository
{
    private readonly FlowCoreDbContext _db;

    public EfWorkspaceRepository(FlowCoreDbContext db) => _db = db;

    public async Task<IReadOnlyList<Workspace>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.Workspaces
            .AsNoTracking()
            .Include(w => w.Projects)
            .OrderBy(w => w.Name)
            .ToListAsync(ct);
    }

    public Task<Workspace?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return _db.Workspaces
            .AsNoTracking()
            .AsSplitQuery()
            .Include(w => w.Owner)
            .Include(w => w.Projects)
            .Include(w => w.TaskStatusDefinitions)
            .FirstOrDefaultAsync(w => w.Id == id, ct);
    }
}

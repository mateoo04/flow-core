using FlowCore.Data;
using FlowCore.Models;
using Microsoft.EntityFrameworkCore;

namespace FlowCore.Repositories.EntityFramework;

public sealed class EfStatusRepository : IStatusRepository
{
    private readonly FlowCoreDbContext _db;

    public EfStatusRepository(FlowCoreDbContext db) => _db = db;

    public async Task<IReadOnlyList<TaskStatusDefinition>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.TaskStatusDefinitions
            .AsNoTracking()
            .Include(s => s.Workspace)
            .OrderBy(s => s.Workspace!.Name)
            .ThenBy(s => s.Position)
            .ToListAsync(ct);
    }
}

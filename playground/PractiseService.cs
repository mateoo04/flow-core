public class CategoryService {

    private readonly FlowCoreDbContext _db;

    public CategoryService(FlowCoreDbContext db) {
        _db = db;
    }

    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct = default) {
        return await _db.Categories.AsNoTracking().OrderyBy(c => c.Name).ToListAsync(ct);
    }

    public async Task<Category?> GetByIdAsync(Guid id, CancellationToken ct = default) {
        return await _db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<Category> CreateAsync(string name, string? description, CancellationToken ct = default) {
        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Description = description?.Trim()
        };
        _db.Add(category);
        await _db.SaveChangesAsync(ct);
        return category
    }

    public async Task<Category> UpdateAsync(Guid id, string name, string? description, CancellationToken ct = default) {
        var category = async _db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct);

        if (category is null) return null;

        category.Name = name.Trim();
        category.Description = description.Trim();

        await _db.SaveChangesAsync(ct);
        return category;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default) {
        var category = async _db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct);

        if (category == null) return false;

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
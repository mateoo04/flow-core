using FlowCore.Data;
using FlowCore.Models;
using FlowCore.Models.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlowCore.Controllers.Api;

[ApiController]
[Route("api/tags")]
public class TagsApiController : ControllerBase
{
    private readonly FlowCoreDbContext _db;

    public TagsApiController(FlowCoreDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TagDto>>> GetAll([FromQuery] string? query, CancellationToken ct)
    {
        var tags = _db.Tags.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query))
            tags = tags.Where(t => t.Name.Contains(query));

        var result = (await tags.OrderBy(t => t.Name).ToListAsync(ct))
            .Select(t => t.ToDto())
            .ToList();

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TagDto>> GetById(Guid id, CancellationToken ct)
    {
        var tag = await _db.Tags.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, ct);
        if (tag is null)
            return NotFound();

        return Ok(tag.ToDto());
    }

    [HttpPost]
    public async Task<ActionResult<TagDto>> Create([FromBody] TagCreateDto model, CancellationToken ct)
    {
        var tag = new Tag
        {
            Id = Guid.NewGuid(),
            Name = model.Name,
            ColorHex = model.ColorHex
        };

        _db.Tags.Add(tag);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = tag.Id }, tag.ToDto());
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TagDto>> Update(Guid id, [FromBody] TagUpdateDto model, CancellationToken ct)
    {
        var tag = await _db.Tags.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (tag is null)
            return NotFound();

        tag.Name = model.Name;
        tag.ColorHex = model.ColorHex;
        await _db.SaveChangesAsync(ct);

        return Ok(tag.ToDto());
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var tag = await _db.Tags.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (tag is null)
            return NotFound();

        _db.Tags.Remove(tag);
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }
}

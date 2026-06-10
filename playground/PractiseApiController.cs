/* Ticket
- Id: Guid
- Title: string
- Description: string?
- IsClosed: bool
- CreatedAt: DateTime */

[ApiController]
[Route("api/tickets")]
public class TicketsApiController : ControllerBase {
    private readonly FlowCoreDbContext _db;

    public TicketsApiController(FlowCoreDbContext db){
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct){
        var tickets = await _db.Tickets
            .AsNoTracking()
            .OrderBy(t => t.Title)
            .ToListAsync(ct);

        return Ok(tickets);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct){
        var ticket = await _db.Tickets
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        if(ticket == null) return NotFound();

        return Ok(ticket);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTicket([FromBody] TicketCreateRequest request, CancellationToken ct){
        if(string.IsNullOrWhiteSpace(request.title)){
            return BadRequest(new {message = "Title is required."})
        }

        var ticket = new Ticket(Guid.newGuid(), request.Title, request.Description, false, DateTime.UtcNow);

        _db.Add(ticket);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new {id = ticket.Id}, ticket)
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateTicket(Guid id,[FromBody] TicketUpdateRequest request, CancellationToken ct){
        var ticket = await _db.Tickets
            .FirstOrDefaultAsync(ticket => ticket.Id == id, ct);

        if(ticket is null) return NotFound();

        if(string.IsNullOrWhiteSpace(request.title))
            return BadRequest(new {message = "Title is required"});

        ticket.Title = request.Title;
        ticket.Description = request.Description;
        ticket.IsClosed = request.IsClosed;

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteTickeet(Guid id, CancellationToken ct){
        var ticket = await _db.Tickets.FirstOrDefaultAsync(ticket => ticket.Id == id);
        if(ticket == null) return NotFound();
        _db.Tickets.Remove(ticket);
        await _db.SaveChangesAsync(ct)
        return NoContent();
    }

}
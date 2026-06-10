public class NoteController : Controller {
    private readonly INoteService _service;

    public NoteController(INoteService service) {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct) {
        var notes = await _service.GetAllAsync(ct);
        return View(notes);

    [HttpGet("/notes/{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct){
        var note = await _service.GetByIdAsync(id, ct);
        if (note is null) return NotFound();
        return View(note);
    }
    
    [HttpGet]
    public IActionResult Create(){
        return View(new NoteFormVm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(NoteFormVm model, CancellationToken ct) {
        if(!ModelState.IsValid) return View(model);
        var note = await _service.CreateAsync(model.title, model.body, ct);
        return RedirectToAction(nameof(Details), new {id = note.Id});
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct){
        var note = await _service.GetByIdAsync(id, ct)
        if(note == null) return NotFound();
        var model = new NoteFormVm{Title = note.Title, Body = note.Body}
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, NoteFormVm note, CancellationToken ct){
        if(!ModelState.IsValid) return View(note);
        var updated = await await _service.UpdateAsync(id, note.Title, note.Body, ct);
        if(updated == null) return NotFound();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct){
        var deleted = await _service.DeleteAsync(id, ct);
        if(!deleted) return NotFound();
        return RedirectToAction(nameof(Index));
    }
}
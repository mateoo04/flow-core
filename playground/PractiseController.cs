public class Category {
    [Key]
    public Guid Id {get; set;}

    public string Name {get; set;} 

    public string? Description {get; set;}
}

public class CategoryCreateFormVM {
    [Required]
    public string Name {get;set;}

    [StringLength(1000)]
    public string? Description {get;set;}
}

public class CategoryEditFormVM {
    public Guid Id {get; set;}

    [Required]
    public string Name {get;set;}

    [StringLength(1000)]
    public string? Description {get;set;}
}

public class PractiseController : Controller {
    
    //why is this readonly?
    private readonly ICategoryService _services;

    public PractiseController(ICategoryService services) {
        _services = services;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct) {
        var categories = await _services.GetAllAsync(ct);
        return View(categories);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct){
        var category = await _services.GetByIdAsync(id, ct);
        if (category == null) return NotFound();

        return View(category);
    }

    //we dont need async and Task here? or do we?
    [HttpGet]
    public IActionResult Create(CancellationToken ct) {
        var vm = new CategoryCreateFormVM();
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryCreateFormVM model, CancellationToken ct) {
        if(!ModelState.IsValid) return View(model);

        var category = await _services.CreateAsync(model.Name, model.Description, ct);

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct) {
        var category = await _services.GetByIdAsync(id, ct);

        if (category == null) return NotFound();

        var vm = new CategoryEditFormVM{
            Id = catgory.Id,  
            Name = category.Name, 
            Description = category.Description};

        return View(vm);
    }

    //kak da znam jel ce service rec da ne postoji
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, CategoryEditFormVM model, CancellationToken ct) {
        if(!ModelState.IsValid) return View(model);

        var updated = await _services.UpdateAsync(id, model.Name, model.Description, ct);

        if (updated is null) return NotFound();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) {
        await _services.DeleteAsync(id, ct);
        return RedirectToAction(nameof(Index));
    }
}
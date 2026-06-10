using FlowCore.Data;
using FlowCore.Models;
using FlowCore.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlowCore.Controllers;

[Authorize(Roles = AppRoles.Admin)]
[Route("/admin")]
public class AdminController : Controller
{
    private readonly FlowCoreDbContext _db;
    private readonly UserManager<User> _userManager;

    public AdminController(FlowCoreDbContext db, UserManager<User> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var users = await _db.Users.AsNoTracking().OrderBy(u => u.Email).ToListAsync(ct);
        var userRows = new List<AdminUserRow>(users.Count);
        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            userRows.Add(new AdminUserRow(u.Id, u.Email ?? "", u.FullName, u.IsActive, roles.ToList()));
        }

        var workspaceRows = await _db.Workspaces
            .AsNoTracking()
            .OrderBy(w => w.Name)
            .Select(w => new AdminWorkspaceRow(
                w.Id, w.Name, w.Visibility, w.Members.Count, w.Projects.Count))
            .ToListAsync(ct);

        return View(new AdminDashboardViewModel(userRows, workspaceRows));
    }
}

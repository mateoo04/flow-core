using Microsoft.AspNetCore.Mvc;

namespace FlowCore.Controllers;

[Route("/settings")]
public class SettingsController : BaseController
{
    [HttpGet("", Name = "settings")]
    public IActionResult Index() => View();
}

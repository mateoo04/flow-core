using FlowCore.Data;
using FlowCore.Models;
using FlowCore.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace FlowCore.Controllers;

[Route("/account")]
public class AccountController : Controller
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly bool _enableDemoLogin;

    public AccountController(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        IHostEnvironment hostEnvironment,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _enableDemoLogin = hostEnvironment.IsDevelopment() || configuration.GetValue<bool>("Features:EnableDemoLogin");
    }

    [HttpGet("register")]
    [AllowAnonymous]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost("register")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Register(RegisterViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var user = new User
        {
            UserName = vm.Email,
            Email = vm.Email,
            FullName = vm.FullName,
            JoinedAt = DateTime.UtcNow,
            IsActive = true,
        };

        var result = await _userManager.CreateAsync(user, vm.Password);
        if (!result.Succeeded)
        {
            foreach (var err in result.Errors)
                ModelState.AddModelError(string.Empty, err.Description);
            return View(vm);
        }

        await _signInManager.SignInAsync(user, isPersistent: false);
        return RedirectToAction("Index", "Home");
    }

    [HttpGet("login")]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
        => View(new LoginViewModel
        {
            ReturnUrl = returnUrl,
            EnableDemoLogin = _enableDemoLogin
        });

    [HttpPost("login")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login(LoginViewModel vm)
    {
        vm.EnableDemoLogin = _enableDemoLogin;
        if (!ModelState.IsValid) return View(vm);

        var result = await _signInManager.PasswordSignInAsync(
            vm.Email, vm.Password, vm.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            return Url.IsLocalUrl(vm.ReturnUrl)
                ? LocalRedirect(vm.ReturnUrl!)
                : RedirectToAction("Index", "Home");
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "Account locked. Try again in 15 minutes.");
            return View(vm);
        }

        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        return View(vm);
    }

    [HttpPost("logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [HttpPost("demo")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Demo()
    {
        if (!_enableDemoLogin)
        {
            return NotFound();
        }

        var demo = await _userManager.FindByEmailAsync(DemoSeedIds.UserDemoEmail);
        if (demo is null)
        {
            ModelState.AddModelError(string.Empty, "Demo account is not available.");
            return View(nameof(Login), new LoginViewModel { EnableDemoLogin = _enableDemoLogin });
        }
        await _signInManager.SignInAsync(demo, isPersistent: false);
        return RedirectToAction("Index", "Home");
    }

    [HttpPost("reset-demo")]
    [Authorize(Policy = "DemoUser")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ResetDemo(
        [FromServices] IDemoDataResetService reset,
        CancellationToken ct)
    {
        await reset.ResetAsync(ct);

        var demo = await _userManager.FindByEmailAsync(DemoSeedIds.UserDemoEmail);
        if (demo is not null)
            await _signInManager.RefreshSignInAsync(demo);

        TempData["DemoInfo"] = "Demo data has been reset.";
        return RedirectToAction("Index", "Home");
    }

    [HttpGet("access-denied")]
    [AllowAnonymous]
    public IActionResult AccessDenied() => View();
}

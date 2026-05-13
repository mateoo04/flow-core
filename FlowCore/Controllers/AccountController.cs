using FlowCore.Models;
using FlowCore.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FlowCore.Controllers;

[AllowAnonymous]
[Route("/account")]
public class AccountController : Controller
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;

    public AccountController(UserManager<User> userManager, SignInManager<User> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [HttpGet("register")]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost("register")]
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
    public IActionResult Login(string? returnUrl = null)
        => View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost("login")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login(LoginViewModel vm)
    {
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

    [HttpGet("access-denied")]
    public IActionResult AccessDenied() => View();
}

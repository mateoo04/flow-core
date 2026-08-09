using System.Security.Claims;
using FlowCore.Data;
using FlowCore.Models;
using FlowCore.Models.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
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
    private readonly IAuthenticationSchemeProvider _schemes;
    private readonly ILogger<AccountController> _logger;
    private readonly bool _enableDemoLogin;

    public AccountController(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        IAuthenticationSchemeProvider schemes,
        IHostEnvironment hostEnvironment,
        IConfiguration configuration,
        ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _schemes = schemes;
        _logger = logger;
        _enableDemoLogin = hostEnvironment.IsDevelopment() || configuration.GetValue<bool>("Features:EnableDemoLogin");
    }

    private async Task<bool> IsGoogleConfiguredAsync()
    {
        var schemes = await _schemes.GetAllSchemesAsync();
        return schemes.Any(s => s.Name == GoogleDefaults.AuthenticationScheme);
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

        await _userManager.AddToRoleAsync(user, AppRoles.User);
        await _signInManager.SignInAsync(user, isPersistent: false);
        _logger.LogInformation("User registered. {UserId}", user.Id);
        return RedirectToAction("Index", "Home");
    }

    [HttpGet("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(string? returnUrl = null)
        => View(new LoginViewModel
        {
            ReturnUrl = returnUrl,
            EnableDemoLogin = _enableDemoLogin,
            ShowGoogleLogin = await IsGoogleConfiguredAsync()
        });

    [HttpPost("login")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login(LoginViewModel vm)
    {
        vm.EnableDemoLogin = _enableDemoLogin;
        vm.ShowGoogleLogin = await IsGoogleConfiguredAsync();
        if (!ModelState.IsValid) return View(vm);

        var result = await _signInManager.PasswordSignInAsync(
            vm.Email, vm.Password, vm.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            _logger.LogInformation("Password login succeeded.");
            return Url.IsLocalUrl(vm.ReturnUrl)
                ? LocalRedirect(vm.ReturnUrl!)
                : RedirectToAction("Index", "Home");
        }

        if (result.IsLockedOut)
        {
            _logger.LogWarning("Password login rejected because the account is locked out.");
            ModelState.AddModelError(string.Empty, "Account locked. Try again in 15 minutes.");
            return View(vm);
        }

        _logger.LogWarning("Password login failed.");
        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        return View(vm);
    }

    [HttpPost("external-login")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("auth")]
    public IActionResult ExternalLogin(string provider, string? returnUrl = null)
    {
        var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return Challenge(properties, provider);
    }

    [HttpGet("external-login-callback")]
    [AllowAnonymous]
    public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
    {
        if (remoteError is not null)
            return LoginWithError($"External provider error: {remoteError}");

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info is null)
            return LoginWithError("Could not load external login information.");

        var signIn = await _signInManager.ExternalLoginSignInAsync(
            info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
        if (signIn.Succeeded)
            return RedirectAfterLogin(returnUrl);

        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrEmpty(email))
            return LoginWithError("The external provider did not supply an email address.");

        if (info.Principal.FindFirstValue("email_verified") != "true")
            return LoginWithError("The external account's email address is not verified.");

        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new User
            {
                UserName = email,
                Email = email,
                FullName = info.Principal.FindFirstValue(ClaimTypes.Name) ?? email,
                JoinedAt = DateTime.UtcNow,
                IsActive = true,
            };

            var created = await _userManager.CreateAsync(user);
            if (!created.Succeeded)
                return LoginWithError(string.Join(" ", created.Errors.Select(e => e.Description)));

            await _userManager.AddToRoleAsync(user, AppRoles.User);
        }

        var linked = await _userManager.AddLoginAsync(user, info);
        if (!linked.Succeeded)
            return LoginWithError(string.Join(" ", linked.Errors.Select(e => e.Description)));

        await _signInManager.SignInAsync(user, isPersistent: false);
        _logger.LogInformation("External login succeeded. {UserId} {Provider}", user.Id, info.LoginProvider);
        return RedirectAfterLogin(returnUrl);
    }

    private IActionResult RedirectAfterLogin(string? returnUrl)
        => Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl!) : RedirectToAction("Index", "Home");

    private IActionResult LoginWithError(string message)
    {
        ModelState.AddModelError(string.Empty, message);
        return View(nameof(Login), new LoginViewModel { EnableDemoLogin = _enableDemoLogin });
    }

    [HttpPost("logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        _logger.LogInformation("User logged out. {UserId}", User.FindFirstValue(ClaimTypes.NameIdentifier));
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

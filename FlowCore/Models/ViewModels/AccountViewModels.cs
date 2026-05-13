using System.ComponentModel.DataAnnotations;
using FlowCore.Models;

namespace FlowCore.Models.ViewModels;

public class RegisterViewModel
{
    [Required, EmailAddress, StringLength(254)]
    [Display(Name = "Email")]
    public string Email { get; set; } = "";

    [Required, StringLength(100, MinimumLength = 1)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = "";

    [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 8)]
    [Display(Name = "Password")]
    public string Password { get; set; } = "";

    [Required, DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = "";
}

public class LoginViewModel
{
    [Required, EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = "";

    [Required, DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = "";

    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }

    public bool EnableDemoLogin { get; set; }
}

public sealed record WorkspaceMemberRow(Guid UserId, string FullName, string Email, WorkspaceRole Role, DateTime JoinedAt);

public class AddWorkspaceMemberVm
{
    [Required, EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = "";
}

public class TransferOwnershipVm
{
    [Required]
    [Display(Name = "New owner")]
    public Guid NewOwnerUserId { get; set; }
}

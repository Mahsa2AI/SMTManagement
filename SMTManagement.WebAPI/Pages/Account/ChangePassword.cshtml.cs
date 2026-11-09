using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SMTManagement.WebAPI.Services;

namespace SMTManagement.WebAPI.Pages.Account;

public sealed class ChangePasswordModel : PageModel
{
    private readonly UserAuthenticationService _authentication;

    public ChangePasswordModel(UserAuthenticationService authentication)
    {
        _authentication = authentication;
    }

    [BindProperty, Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [BindProperty, Required, StringLength(100, MinimumLength = 8)]
    public string NewPassword { get; set; } = string.Empty;

    [BindProperty, Required, Compare(nameof(NewPassword))]
    public string ConfirmPassword { get; set; } = string.Empty;

    public string? ErrorMessage { get; private set; }
    public string? SuccessMessage { get; private set; }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return Page();

        var username = User.Identity?.Name;
        var changed = await _authentication.ChangePasswordAsync(
            username, CurrentPassword, NewPassword, ct);

        if (!changed)
        {
            ErrorMessage = "The current password is incorrect.";
            return Page();
        }

        CurrentPassword = string.Empty;
        NewPassword = string.Empty;
        ConfirmPassword = string.Empty;
        SuccessMessage = "Your password was changed successfully.";
        return Page();
    }
}

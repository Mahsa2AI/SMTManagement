using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SMTManagement.WebAPI.Services;

namespace SMTManagement.WebAPI.Pages;

public sealed class LoginModel : PageModel
{
    private readonly UserAuthenticationService _authentication;

    public LoginModel(UserAuthenticationService authentication)
    {
        _authentication = authentication;
    }

    [BindProperty]
    public string? Username { get; set; }

    [BindProperty]
    public string? Password { get; set; }

    public string? ErrorMessage { get; private set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl)
    {
        var account = await _authentication.ValidateAsync(Username, Password);
        if (account is null)
        {
            ErrorMessage = "The username or password is invalid.";
            return Page();
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, account.Id.ToString()),
            new Claim(ClaimTypes.Name, account.Username)
        };
        var identity = new ClaimsIdentity(claims, "UiCookie");
        await HttpContext.SignInAsync("UiCookie", new ClaimsPrincipal(identity));

        return LocalRedirect(string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl);
    }
}

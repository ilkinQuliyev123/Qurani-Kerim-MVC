using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuranApp.Models;
using QuranApp.Services;

namespace QuranApp.Controllers;

public static class ClaimsExt
{
    public static int? Id(this ClaimsPrincipal u) => int.TryParse(u.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    public static string Lang(this HttpRequest r) => QuranService.Lang(r.Cookies["lang"]);
}

/// <summary>MVC səhifələri (Razor view-lar).</summary>
public class HomeController(QuranService quran) : Controller
{
    public async Task<IActionResult> Index() => View(await quran.Random(Request.Lang(), User.Id()));

    [Route("/Surah/{n:int}")]
    public async Task<IActionResult> Surah(int n)
    {
        var s = await quran.Surah(Math.Clamp(n, 1, 114));
        return s == null ? NotFound() : View(s);
    }

    public IActionResult Search(string? q) { ViewBag.Q = q; return View(); }

    [Authorize] public IActionResult Wishlist() => View();
    [Authorize] public IActionResult Notes() => View();
    [Authorize(Roles = "Admin")] public IActionResult Admin() => View();
    public IActionResult Error() => Content("Xəta baş verdi.");
}

public class AccountController(UserService users) : Controller
{
    public IActionResult Login() => View();
    public IActionResult Register() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    // ---- JSON API (login/register formaları fetch ilə göndərir) ----
    [HttpPost("/api/auth/login")]
    public async Task<IActionResult> ApiLogin([FromBody] LoginReq r)
    {
        var u = await users.Login(r.Email, r.Password);
        if (u == null) return Ok(new { ok = false, err = "wrongCred" });
        await SignIn(u, r.Remember);
        return Ok(new { ok = true, name = u.Name });
    }

    [HttpPost("/api/auth/register")]
    public async Task<IActionResult> ApiRegister([FromBody] RegisterReq r)
    {
        var (u, err) = await users.Register(r);
        if (u == null) return Ok(new { ok = false, err });
        await SignIn(u, false);
        return Ok(new { ok = true, name = u.Name });
    }

    Task SignIn(User u, bool remember)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, u.Id.ToString()), new(ClaimTypes.Name, u.Name), new(ClaimTypes.Email, u.Email) };
        if (u.IsAdmin) claims.Add(new(ClaimTypes.Role, "Admin"));
        return HttpContext.SignInAsync(new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)),
            new AuthenticationProperties { IsPersistent = remember });
    }
}

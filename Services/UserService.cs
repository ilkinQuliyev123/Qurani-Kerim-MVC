using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuranApp.Data;
using QuranApp.Models;

namespace QuranApp.Services;

/// <summary>Qeydiyyat/giriş, bəyənilənlər, qeydlər və şərhlər.</summary>
public class UserService(AppDbContext db, QuranService quran)
{
    static readonly PasswordHasher<User> Hasher = new();
    public static string Hash(string pw) => Hasher.HashPassword(null!, pw);

    // ---------- Auth ----------
    public async Task<(User? user, string? err)> Register(RegisterReq r)
    {
        var name = r.Name?.Trim() ?? ""; var email = r.Email?.Trim().ToLowerInvariant() ?? "";
        if (name.Length < 3) return (null, "errName");
        if (!Regex.IsMatch(email, @"^\S+@\S+\.\S+$")) return (null, "errEmail");
        if ((r.Password ?? "").Length < 8) return (null, "errPw");
        if (await db.Users.AnyAsync(u => u.Email == email)) return (null, "emailTaken");
        var u = new User { Name = name, Email = email, PasswordHash = Hash(r.Password!) };
        db.Users.Add(u); await db.SaveChangesAsync();
        return (u, null);
    }

    public async Task<User?> Login(string email, string pw)
    {
        email = email?.Trim().ToLowerInvariant() ?? "";
        var u = await db.Users.FirstOrDefaultAsync(x => x.Email == email);
        return u != null && Hasher.VerifyHashedPassword(u, u.PasswordHash, pw ?? "") != PasswordVerificationResult.Failed ? u : null;
    }

    // ---------- Wishlist ----------
    public async Task<PageDto<AyahDto>> Wishlist(int uid, int offset, int limit, string lang)
    {
        var q = db.Favorites.Where(f => f.UserId == uid);
        var total = await q.CountAsync();
        var ids = q.OrderByDescending(f => f.CreatedAt).Skip(Math.Max(0, offset)).Take(Math.Clamp(limit, 1, QuranService.PageSize)).Select(f => f.AyahId);
        var items = await quran.Project(db.Ayahs.Where(a => ids.Contains(a.Id)), lang, uid);
        var order = await ids.ToListAsync();
        return new(items.OrderBy(a => order.IndexOf(a.Id)).ToList(), total);
    }

    /// Bəyən / bəyənməni geri al. Yeni vəziyyəti qaytarır.
    public async Task<bool?> ToggleFavorite(int uid, int ayahId)
    {
        if (!await db.Ayahs.AnyAsync(a => a.Id == ayahId)) return null;
        var f = await db.Favorites.FindAsync(uid, ayahId);
        if (f != null) db.Favorites.Remove(f); else db.Favorites.Add(new Favorite { UserId = uid, AyahId = ayahId });
        await db.SaveChangesAsync();
        return f == null;
    }

    // ---------- Notes (şəxsi) ----------
    public async Task<PageDto<NoteDto>> Notes(int uid, int offset, int limit)
    {
        var q = db.Notes.Where(n => n.UserId == uid);
        var items = await q.OrderByDescending(n => n.CreatedAt).Skip(Math.Max(0, offset)).Take(Math.Clamp(limit, 1, QuranService.PageSize))
            .Select(n => new NoteDto(n.Id, n.Ayah!.SurahId + ":" + n.Ayah.Number, n.Ayah.Arabic, n.Text, n.CreatedAt)).ToListAsync();
        return new(items, await q.CountAsync());
    }

    public async Task<string?> AddNote(int uid, NoteReq r)
    {
        var text = r.Text?.Trim() ?? "";
        if (text.Length == 0 || text.Length > 2000) return "emptyNote";
        var m = QuranService.RefRx().Match(r.Ref?.Replace(" ", "") ?? "");
        var id = m.Success ? await quran.FindId(int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value)) : null;
        if (id == null) return "badRef";
        db.Notes.Add(new Note { UserId = uid, AyahId = id.Value, Text = text });
        await db.SaveChangesAsync();
        return null;
    }

    public async Task<bool> UpdateNote(int uid, int id, string text)
    {
        text = text?.Trim() ?? "";
        var n = await db.Notes.FirstOrDefaultAsync(x => x.Id == id && x.UserId == uid);
        if (n == null || text.Length == 0 || text.Length > 2000) return false;
        n.Text = text; await db.SaveChangesAsync(); return true;
    }

    public async Task<bool> DeleteNote(int uid, int id) =>
        await db.Notes.Where(x => x.Id == id && x.UserId == uid).ExecuteDeleteAsync() > 0;

    // ---------- Comments (hamıya açıq) ----------
    public Task<List<CommentDto>> Comments(int ayahId, int? uid) =>
        db.Comments.Where(c => c.AyahId == ayahId).OrderByDescending(c => c.CreatedAt).Take(100)
            .Select(c => new CommentDto(c.Id, c.User!.Name, c.Text, c.CreatedAt, c.UserId == uid)).ToListAsync();

    public async Task<CommentDto?> AddComment(int uid, int ayahId, string text)
    {
        text = text?.Trim() ?? "";
        if (text.Length == 0 || text.Length > 1000 || !await db.Ayahs.AnyAsync(a => a.Id == ayahId)) return null;
        var c = new Comment { UserId = uid, AyahId = ayahId, Text = text };
        db.Comments.Add(c); await db.SaveChangesAsync();
        var name = await db.Users.Where(u => u.Id == uid).Select(u => u.Name).FirstAsync();
        return new CommentDto(c.Id, name, c.Text, c.CreatedAt, true);
    }

    /// Şərhi yalnız sahibi və ya admin silə bilər.
    public async Task<bool> DeleteComment(int uid, bool isAdmin, int id) =>
        await db.Comments.Where(c => c.Id == id && (isAdmin || c.UserId == uid)).ExecuteDeleteAsync() > 0;
}

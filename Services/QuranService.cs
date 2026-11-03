using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using QuranApp.Data;
using QuranApp.Models;

namespace QuranApp.Services;

/// <summary>Surə/ayə oxuma, səhifələmə (20-20) və axtarış.</summary>
public partial class QuranService(AppDbContext db)
{
    public const int PageSize = 20;
    public static readonly string[] Langs = ["az", "en", "ru", "tr"];
    public static string Lang(string? l) => Langs.Contains(l) ? l! : "az";

    /// Ərəbcə hərəkələri silir, əlif/yə variantlarını birləşdirir (axtarış üçün).
    public static string ArabicPlain(string s) =>
        ArNorm().Replace(Diacritics().Replace(s ?? "", ""), "ا").Replace('ى', 'ي').Trim();

    [GeneratedRegex("[ً-ٰٟـۖ-ۭ]")] private static partial Regex Diacritics();
    [GeneratedRegex("[أإآٱ]")] private static partial Regex ArNorm();
    [GeneratedRegex(@"^(\d{1,3})\s*[:.]\s*(\d{1,3})$")] public static partial Regex RefRx();

    public Task<List<SurahDto>> Surahs() =>
        db.Surahs.OrderBy(s => s.Id).Select(s => new SurahDto(s.Id, s.Name, s.AyahCount)).ToListAsync();

    public Task<Surah?> Surah(int n) => db.Surahs.AsNoTracking().FirstOrDefaultAsync(s => s.Id == n);

    /// Ayə sorğusunu DTO-ya çevirir (yalnız lazım olan dil).
    public async Task<List<AyahDto>> Project(IQueryable<Ayah> q, string lang, int? userId)
    {
        lang = Lang(lang);
        var rows = await q.Select(a => new
        {
            a.Id, a.SurahId, a.Number, a.Arabic,
            Tr = a.Translations.Where(t => t.Lang == lang).Select(t => t.Text).FirstOrDefault()
                 ?? a.Translations.Where(t => t.Lang == "az").Select(t => t.Text).FirstOrDefault() ?? "",
            Words = a.Words.OrderBy(w => w.Order).ToList(),
            Fav = userId != null && db.Favorites.Any(f => f.UserId == userId && f.AyahId == a.Id),
            Cm = db.Comments.Count(c => c.AyahId == a.Id)
        }).AsNoTracking().ToListAsync();
        return rows.Select(r => new AyahDto(r.Id, r.SurahId, r.Number, $"{r.SurahId}:{r.Number}", r.Arabic, r.Tr,
            r.Words.Select(w => new WordDto(w.Arabic, w.Latin, w.Mean(lang) ?? w.MeanAz ?? "")).ToList(), r.Fav, r.Cm)).ToList();
    }

    /// Surədən from..to aralığı (ən çox 20 ayə — sayt yüklənməsin).
    public Task<List<AyahDto>> Range(int surah, int from, int to, string lang, int? userId)
    {
        from = Math.Max(1, from);
        to = Math.Min(to, from + PageSize - 1);
        return Project(db.Ayahs.Where(a => a.SurahId == surah && a.Number >= from && a.Number <= to).OrderBy(a => a.Number), lang, userId);
    }

    public async Task<AyahDto?> Random(string lang, int? userId)
    {
        var count = await db.Ayahs.CountAsync();
        if (count == 0) return null;
        var skip = (DateTime.UtcNow.DayOfYear * 37) % count;   // "Günün ayəsi" — gün ərzində eyni qalır
        return (await Project(db.Ayahs.OrderBy(a => a.Id).Skip(skip).Take(1), lang, userId)).FirstOrDefault();
    }

    /// "3:2" formatı və ya söz (ərəbcə / seçilmiş dilin tərcüməsi).
    public async Task<PageDto<AyahDto>> Search(string q, string lang, int offset, int limit, int? userId)
    {
        q = (q ?? "").Trim(); lang = Lang(lang); limit = Math.Clamp(limit, 1, PageSize);
        if (q.Length == 0) return new([], 0);
        IQueryable<Ayah> src;
        var m = RefRx().Match(q);
        if (m.Success)
        {
            int s = int.Parse(m.Groups[1].Value), v = int.Parse(m.Groups[2].Value);
            src = db.Ayahs.Where(a => a.SurahId == s && a.Number == v);
        }
        else
        {
            var ar = ArabicPlain(q);
            src = db.Ayahs.Where(a => (ar.Length > 0 && a.ArabicPlain.Contains(ar)) ||
                                      a.Translations.Any(t => t.Lang == lang && t.Text.Contains(q)));
        }
        var total = await src.CountAsync();
        var items = await Project(src.OrderBy(a => a.SurahId).ThenBy(a => a.Number).Skip(Math.Max(0, offset)).Take(limit), lang, userId);
        return new(items, total);
    }

    public Task<int?> FindId(int surah, int num) =>
        db.Ayahs.Where(a => a.SurahId == surah && a.Number == num).Select(a => (int?)a.Id).FirstOrDefaultAsync();
}

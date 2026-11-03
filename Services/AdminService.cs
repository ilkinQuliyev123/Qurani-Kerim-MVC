using Microsoft.EntityFrameworkCore;
using QuranApp.Data;
using QuranApp.Models;

namespace QuranApp.Services;

/// <summary>Admin: ayə siyahısı, əlavə, redaktə, silmə. Dəyişikliklər birbaşa SQL-ə yazılır.</summary>
public class AdminService(AppDbContext db)
{
    public async Task<PageDto<AdminAyahDto>> List(int surah, string? q, int page)
    {
        IQueryable<Ayah> src = db.Ayahs.Where(a => a.SurahId == surah);
        q = q?.Trim();
        if (!string.IsNullOrEmpty(q))
        {
            var m = QuranService.RefRx().Match(q);
            if (m.Success) { int s = int.Parse(m.Groups[1].Value), v = int.Parse(m.Groups[2].Value); src = db.Ayahs.Where(a => a.SurahId == s && a.Number == v); }
            else { var ar = QuranService.ArabicPlain(q); src = src.Where(a => a.ArabicPlain.Contains(ar) || a.Translations.Any(t => t.Text.Contains(q)) || a.Words.Any(w => w.Latin.Contains(q))); }
        }
        var total = await src.CountAsync();
        var items = await Load(src.OrderBy(a => a.SurahId).ThenBy(a => a.Number).Skip((Math.Max(1, page) - 1) * QuranService.PageSize).Take(QuranService.PageSize));
        return new(items, total);
    }

    public async Task<AdminAyahDto?> Get(int id) => (await Load(db.Ayahs.Where(a => a.Id == id))).FirstOrDefault();

    async Task<List<AdminAyahDto>> Load(IQueryable<Ayah> q) =>
        (await q.Include(a => a.Translations).Include(a => a.Words).AsNoTracking().AsSplitQuery().ToListAsync())
        .Select(a => new AdminAyahDto(a.Id, a.SurahId, a.Number, $"{a.SurahId}:{a.Number}", a.Arabic,
            a.Translations.ToDictionary(t => t.Lang, t => t.Text),
            a.Words.OrderBy(w => w.Order).Select(w => new AdminWordDto(w.Arabic, w.Latin, new()
            { ["az"] = w.MeanAz ?? "", ["en"] = w.MeanEn ?? "", ["ru"] = w.MeanRu ?? "", ["tr"] = w.MeanTr ?? "" })).ToList())).ToList();

    static string? Validate(AdminAyahReq r)
    {
        if (string.IsNullOrWhiteSpace(r.Arabic)) return "Ərəbcə mətn boş ola bilməz";
        if (r.Tr == null || !r.Tr.TryGetValue("az", out var az) || string.IsNullOrWhiteSpace(az)) return "Azərbaycanca tərcümə məcburidir";
        if (r.Words?.Any(w => string.IsNullOrWhiteSpace(w.Ar) || string.IsNullOrWhiteSpace(w.Lat)) == true) return "Hər sözün ərəbcəsi və oxunuşu dolu olmalıdır";
        if (r.Surah is < 1 or > 114 || r.Num < 1) return "Surə və ya ayə nömrəsi səhvdir";
        return null;
    }

    static void Apply(Ayah a, AdminAyahReq r)
    {
        a.Arabic = r.Arabic.Trim();
        a.ArabicPlain = QuranService.ArabicPlain(a.Arabic);
        foreach (var l in QuranService.Langs)
        {
            var text = r.Tr.GetValueOrDefault(l)?.Trim() ?? "";
            var t = a.Translations.FirstOrDefault(x => x.Lang == l);
            if (text.Length == 0) { if (t != null) a.Translations.Remove(t); }
            else if (t == null) a.Translations.Add(new AyahTranslation { Lang = l, Text = text });
            else t.Text = text;
        }
        a.Words.Clear();
        var i = 0;
        foreach (var w in r.Words ?? [])
            a.Words.Add(new AyahWord { Order = i++, Arabic = w.Ar.Trim(), Latin = w.Lat.Trim(),
                MeanAz = w.Mean.GetValueOrDefault("az"), MeanEn = w.Mean.GetValueOrDefault("en"),
                MeanRu = w.Mean.GetValueOrDefault("ru"), MeanTr = w.Mean.GetValueOrDefault("tr") });
    }

    public async Task<(AdminAyahDto? dto, string? err)> Create(AdminAyahReq r)
    {
        if (Validate(r) is { } e) return (null, e);
        if (await db.Ayahs.AnyAsync(a => a.SurahId == r.Surah && a.Number == r.Num)) return (null, $"{r.Surah}:{r.Num} artıq mövcuddur");
        var a = new Ayah { SurahId = r.Surah, Number = r.Num };
        Apply(a, r); db.Ayahs.Add(a);
        await db.SaveChangesAsync(); await Recount(r.Surah);
        return (await Get(a.Id), null);
    }

    public async Task<(AdminAyahDto? dto, string? err)> Update(int id, AdminAyahReq r)
    {
        var a = await db.Ayahs.Include(x => x.Translations).Include(x => x.Words).FirstOrDefaultAsync(x => x.Id == id);
        if (a == null) return (null, "Ayə tapılmadı");
        if (Validate(r with { Surah = a.SurahId, Num = a.Number }) is { } e) return (null, e);
        Apply(a, r); await db.SaveChangesAsync();
        return (await Get(id), null);
    }

    public async Task<bool> Delete(int id)
    {
        var a = await db.Ayahs.FindAsync(id);
        if (a == null) return false;
        db.Ayahs.Remove(a); await db.SaveChangesAsync(); await Recount(a.SurahId);
        return true;
    }

    public async Task<int> NextNumber(int surah) => (await db.Ayahs.Where(a => a.SurahId == surah).MaxAsync(a => (int?)a.Number) ?? 0) + 1;

    async Task Recount(int surah)
    {
        var n = await db.Ayahs.CountAsync(a => a.SurahId == surah);
        await db.Surahs.Where(s => s.Id == surah).ExecuteUpdateAsync(s => s.SetProperty(x => x.AyahCount, n));
    }
}

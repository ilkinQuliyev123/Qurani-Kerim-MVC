using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using QuranApp.Models;
using QuranApp.Services;

namespace QuranApp.Data;

/// <summary>Baza boşdursa Data/seed/quran.json-dan 114 surə və 6236 ayəni SQL-ə yazır.
/// TR = Ali Bulaç məalı (PDF-dən), AZ/EN/RU/ərəbcə — açıq mənbədən.</summary>
public static class Seeder
{
    record SeedAyah(string ar, string az, string en, string ru, string tr);
    record SeedSurah(int n, string name, List<SeedAyah> ayahs);

    public static async Task Run(AppDbContext db, IConfiguration cfg, string root)
    {
        await db.Database.MigrateAsync();

        if (!await db.Surahs.AnyAsync())
        {
            var data = JsonSerializer.Deserialize<List<SeedSurah>>(await File.ReadAllTextAsync(Path.Combine(root, "Data", "seed", "quran.json")))!;
            db.ChangeTracker.AutoDetectChangesEnabled = false;
            foreach (var s in data)
            {
                var surah = new Surah { Id = s.n, Name = s.name, AyahCount = s.ayahs.Count };
                for (int i = 0; i < s.ayahs.Count; i++)
                {
                    var a = s.ayahs[i];
                    var ayah = new Ayah { Number = i + 1, Arabic = a.ar, ArabicPlain = QuranService.ArabicPlain(a.ar) };
                    foreach (var (l, t) in new[] { ("az", a.az), ("en", a.en), ("ru", a.ru), ("tr", a.tr) })
                        if (!string.IsNullOrWhiteSpace(t)) ayah.Translations.Add(new AyahTranslation { Lang = l, Text = t });
                    surah.Ayahs.Add(ayah);
                }
                db.Surahs.Add(surah);
                if (s.n % 10 == 0) { await db.SaveChangesAsync(); db.ChangeTracker.Clear(); }
            }
            await db.SaveChangesAsync(); db.ChangeTracker.Clear();
            await SeedWords(db);
        }

        // Admin hesabı: parol koda yazılmır — appsettings.Development.json və ya mühit dəyişəni (Admin__Password)
        var email = cfg["Admin:Email"]?.ToLowerInvariant();
        if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(cfg["Admin:Password"]) && !await db.Users.AnyAsync(u => u.Email == email))
        {
            db.Users.Add(new User { Name = "Admin", Email = email, PasswordHash = UserService.Hash(cfg["Admin:Password"]!), IsAdmin = true });
            await db.SaveChangesAsync();
        }
    }

    // Nümunə söz-söz cədvəl (Fatihə 2-4). Qalanlarını admin paneldən əlavə etmək olar.
    static async Task SeedWords(AppDbContext db)
    {
        string[][][] w =
        [
            [["الْحَمْدُ","al-ḥamdu","həmd, tərif","praise","хвала","hamd"],["لِلَّهِ","lillāhi","Allaha məxsusdur","belongs to God","принадлежит Аллаху","Allah’a aittir"],["رَبِّ","rabbi","Rəbbi","Lord of","Господь","Rabbi"],["الْعَالَمِينَ","al-ʿālamīn","aləmlərin","the worlds","миров","âlemlerin"]],
            [["الرَّحْمَٰنِ","ar-raḥmāni","Rəhman","the Gracious","Милостивый","Rahman"],["الرَّحِيمِ","ar-raḥīmi","Rəhim","the Merciful","Милосердный","Rahîm"]],
            [["مَالِكِ","māliki","sahibi","Master","Властелин","sahibi"],["يَوْمِ","yawmi","günün","of the day","дня","günün"],["الدِّينِ","ad-dīni","haqq-hesabın","judgment","воздаяния","dinin"]]
        ];
        for (int i = 0; i < w.Length; i++)
        {
            var ayah = await db.Ayahs.FirstAsync(a => a.SurahId == 1 && a.Number == i + 2);
            for (int k = 0; k < w[i].Length; k++)
                db.Words.Add(new AyahWord { AyahId = ayah.Id, Order = k, Arabic = w[i][k][0], Latin = w[i][k][1], MeanAz = w[i][k][2], MeanEn = w[i][k][3], MeanRu = w[i][k][4], MeanTr = w[i][k][5] });
        }
        await db.SaveChangesAsync();
    }
}

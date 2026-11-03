# Qurani-Kərim — ASP.NET Core MVC

Final layihəsi: ASP.NET Core 8 MVC + EF Core + SQL Server.

## İmkanlar
- 114 surə, 6236 ayə SQL-də (ərəbcə + AZ/EN/RU + TR — Ali Bulaç məalı PDF-dən)
- Surə səhifəsində ayələr **20-20** yüklənir (sonsuz scroll)
- Axtarış: `3:2` və ya söz (ərəbcə hərəkəsiz də tapır)
- Qeydiyyat / giriş (parol hash, cookie sessiya)
- Bəyəndiklərim, şəxsi qeydlər, ayələrə şərhlər — hamısı SQL-də
- Admin panel: ayə əlavə et / redaktə et / sil (tərcümələr və söz-söz cədvəl)
- 4 dil (AZ/EN/RU/TR), tünd/açıq tema

## İşə salma
1. SQL Server Express (`.\SQLEXPRESS`) işləməlidir. Başqa server üçün `appsettings.json` → `ConnectionStrings:Default`.
2. Admin parolu: `appsettings.Development.json` faylı yarat:
   ```json
   { "Admin": { "Password": "öz-parolun" } }
   ```
3. `dotnet run` → http://localhost:5080
   İlk açılışda `QuranDb` bazası miqrasiya ilə yaranır və ayələr `Data/seed/quran.json`-dan yüklənir.

## Struktur
- `Controllers/` — MVC səhifələri və JSON API
- `Services/` — əsas məntiq (QuranService, UserService, AdminService)
- `Data/` — DbContext, Seeder, Migrations
- `Views/` — Razor; `wwwroot/` — CSS/JS

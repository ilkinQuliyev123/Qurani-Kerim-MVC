# CLAUDE.md

Bu fayl Claude Code üçün layihə qaydalarını təyin edir. Hər sessiyanın əvvəlində oxunur, ona görə qısa və dəqiq saxla.

## Layihə haqqında
- Məqsəd: Qurani-Kərim saytı — surələr/ayələr (20-20 yüklənir), axtarış, bəyənilənlər, şəxsi qeydlər, ayələrə şərhlər, admin paneldən ayə əlavə/redaktə/silmə.
- Texnologiyalar: ASP.NET Core 8 MVC (C#), Entity Framework Core, SQL Server Express (`.\SQLEXPRESS`, baza `QuranDb`), sadə HTML/CSS/JS (wwwroot).
- Əsas qovluqlar: `Controllers/` (MVC + API), `Services/` (əsas məntiq), `Models/`, `Data/` (DbContext, Seeder, `seed/quran.json`, Migrations), `Views/` (Razor), `wwwroot/` (css, js).
- Ayə mənbələri: TR — Ali Bulaç məalı (PDF-dən çıxarılıb), ərəbcə/AZ/EN/RU — alquran.cloud.

## Komandalar
- Bərpa: `dotnet restore`
- Işə salma: `dotnet run` (ilk açılışda baza yaradılır və 6236 ayə yüklənir)
- Build: `dotnet build`
- Miqrasiya: `dotnet ef migrations add <Ad>` (təsdiqsiz işlətmə)

## İş qaydaları
1. Kodu dəyişdirməzdən əvvəl qısa plan yaz və təsdiq al, böyük dəyişikliklərdə plan rejimindən istifadə et.
2. Hər dəyişiklikdən sonra build et və işlədib yoxla. Build keçməyincə iş bitmiş sayılmır.
3. Mövcud kodun üslubuna əməl et, yeni asılılıq əlavə etməzdən əvvəl soruş.
4. Lazımsız fayl oxuma: yalnız işlə bağlı faylları aç. Böyük qovluqlarda axtarış əvəzinə konkret yolları istifadə et.
5. `bin`, `obj`, `Data/Migrations` (əl ilə), `.env` və digər generasiya olunan fayllara toxunma.
6. Məlumat silən, `git push --force`, `git reset --hard` və ya miqrasiya kimi geri dönməz əməliyyatlardan əvvəl təsdiq al.
7. Dəyişikliklərdən sonra qısa xülasə yaz: nə dəyişdi, hansı fayllarda, build/test keçdimi.
8. İcazə istəyəndə əmrin nə etdiyini azərbaycanca izah et.

## Kod üslubu
- C#: PascalCase (sinif, metod), camelCase (lokal dəyişən). JS: camelCase.
- Əsas məntiq `Services/`-də olur, controller-lər nazik qalır.
- Şərhlər yalnız məntiqin qeyri-aşkar hissələri üçündür.
- Xəta idarəetməsi: servis xəta açarı qaytarır (`errEmail`, `badRef`...), API `{ err }` ilə 400 cavab verir, frontend açarı I18N ilə göstərir.

## Söhbət və cavab üslubu
- Cavabları qısa və konkret yaz, lazım olmayan təkrarlardan qaçın.
- Qeyri-müəyyən olduqda təxmin etmək əvəzinə soruş.
- Səhv etdiyini görsən, dərhal bildir və düzəlt.

## Məxfilik və təhlükəsizlik
- API açarları, parollar və şəxsi məlumatları kodda və ya cavabda yazma. Admin parolu `appsettings.Development.json`-dadır (git-ə düşmür).
- `.env` faylını oxuma və ya göndərmə.

using System.ComponentModel.DataAnnotations;

namespace QuranApp.Models;

public class Surah
{
    public int Id { get; set; }                       // 1..114
    [MaxLength(60)] public string Name { get; set; } = "";
    public int AyahCount { get; set; }
    public List<Ayah> Ayahs { get; set; } = new();
}

public class Ayah
{
    public int Id { get; set; }
    public int SurahId { get; set; }
    public Surah? Surah { get; set; }
    public int Number { get; set; }
    public string Arabic { get; set; } = "";
    public string ArabicPlain { get; set; } = "";     // hərəkəsiz — axtarış üçün
    public List<AyahTranslation> Translations { get; set; } = new();
    public List<AyahWord> Words { get; set; } = new();
}

public class AyahTranslation
{
    public int Id { get; set; }
    public int AyahId { get; set; }
    [MaxLength(2)] public string Lang { get; set; } = "az";
    public string Text { get; set; } = "";
}

public class AyahWord
{
    public int Id { get; set; }
    public int AyahId { get; set; }
    public int Order { get; set; }
    [MaxLength(100)] public string Arabic { get; set; } = "";
    [MaxLength(100)] public string Latin { get; set; } = "";
    [MaxLength(200)] public string? MeanAz { get; set; }
    [MaxLength(200)] public string? MeanEn { get; set; }
    [MaxLength(200)] public string? MeanRu { get; set; }
    [MaxLength(200)] public string? MeanTr { get; set; }
    public string? Mean(string lang) => lang switch { "en" => MeanEn, "ru" => MeanRu, "tr" => MeanTr, _ => MeanAz };
}

public class User
{
    public int Id { get; set; }
    [MaxLength(50)] public string Name { get; set; } = "";
    [MaxLength(150)] public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public bool IsAdmin { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Favorite
{
    public int UserId { get; set; }
    public int AyahId { get; set; }
    public Ayah? Ayah { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Note
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int AyahId { get; set; }
    public Ayah? Ayah { get; set; }
    [MaxLength(2000)] public string Text { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Comment
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public int AyahId { get; set; }
    public Ayah? Ayah { get; set; }
    [MaxLength(1000)] public string Text { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

using Microsoft.EntityFrameworkCore;
using QuranApp.Models;

namespace QuranApp.Data;

public class AppDbContext(DbContextOptions<AppDbContext> o) : DbContext(o)
{
    public DbSet<Surah> Surahs => Set<Surah>();
    public DbSet<Ayah> Ayahs => Set<Ayah>();
    public DbSet<AyahTranslation> Translations => Set<AyahTranslation>();
    public DbSet<AyahWord> Words => Set<AyahWord>();
    public DbSet<AyahCommentary> Commentaries => Set<AyahCommentary>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<Note> Notes => Set<Note>();
    public DbSet<Comment> Comments => Set<Comment>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Surah>().Property(s => s.Id).ValueGeneratedNever();
        b.Entity<Ayah>().HasIndex(a => new { a.SurahId, a.Number }).IsUnique();
        b.Entity<AyahTranslation>().HasIndex(t => new { t.AyahId, t.Lang }).IsUnique();
        b.Entity<AyahCommentary>().HasIndex(c => new { c.AyahId, c.Order });
        b.Entity<User>().HasIndex(u => u.Email).IsUnique();
        b.Entity<Favorite>().HasKey(f => new { f.UserId, f.AyahId });
        b.Entity<Note>().HasIndex(n => new { n.UserId, n.CreatedAt });
        b.Entity<Comment>().HasIndex(c => new { c.AyahId, c.CreatedAt });

        // İstifadəçi silinəndə onun məlumatları da silinsin; ayə silinəndə də
        b.Entity<Favorite>().HasOne<User>().WithMany().HasForeignKey(f => f.UserId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Note>().HasOne<User>().WithMany().HasForeignKey(n => n.UserId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Comment>().HasOne(c => c.User).WithMany().HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.NoAction);
    }
}

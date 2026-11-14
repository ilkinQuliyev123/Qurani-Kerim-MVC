using Microsoft.EntityFrameworkCore;
using QuranApp.Models;

namespace QuranApp.Services;

/// <summary>Surə haqqında məlumat və ayə şərhləri (təfsirlər).</summary>
public partial class QuranService
{
    public Task<SurahAboutDto?> About(int n) =>
        db.Surahs.AsNoTracking().Where(s => s.Id == n).Select(s => new SurahAboutDto(s.Id, s.Name, s.About)).FirstOrDefaultAsync();

    /// Ayə tapılmasa null (404), şərh yoxdursa boş siyahı.
    public async Task<List<CommentaryDto>?> Commentaries(int surah, int num)
    {
        var id = await FindId(surah, num);
        if (id is null) return null;
        return await db.Commentaries.AsNoTracking().Where(c => c.AyahId == id).OrderBy(c => c.Order)
            .Select(c => new CommentaryDto(c.Title, c.Text)).ToListAsync();
    }
}

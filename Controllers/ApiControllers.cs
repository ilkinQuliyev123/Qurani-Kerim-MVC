using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuranApp.Models;
using QuranApp.Services;

namespace QuranApp.Controllers;

/// <summary>Ayələr: surə üzrə 20-20, axtarış, şərhlər.</summary>
[ApiController, Route("api")]
public class QuranApiController(QuranService quran, UserService users) : ControllerBase
{
    [HttpGet("surahs")] public Task<List<SurahDto>> Surahs() => quran.Surahs();

    [HttpGet("surahs/{n:int}/ayahs")]
    public Task<List<AyahDto>> Ayahs(int n, int from = 1, int to = 20, string? lang = null) => quran.Range(n, from, to, lang ?? Request.Lang(), User.Id());

    [HttpGet("surahs/{n:int}/about")]
    public async Task<IActionResult> About(int n) => await quran.About(n) is { } a ? Ok(a) : NotFound();

    [HttpGet("surahs/{n:int}/ayahs/{a:int}/commentaries")]
    public async Task<IActionResult> Commentaries(int n, int a) => await quran.Commentaries(n, a) is { } c ? Ok(c) : NotFound();

    [HttpGet("search")]
    public Task<PageDto<AyahDto>> Search(string q, string? lang = null, int offset = 0, int limit = 20) => quran.Search(q, lang ?? Request.Lang(), offset, limit, User.Id());

    [HttpGet("ayahs/{id:int}/comments")]
    public Task<List<CommentDto>> Comments(int id) => users.Comments(id, User.Id());

    [Authorize, HttpPost("ayahs/{id:int}/comments")]
    public async Task<IActionResult> AddComment(int id, TextReq r) =>
        await users.AddComment(User.Id()!.Value, id, r.Text) is { } c ? Ok(c) : BadRequest(new { err = "emptyComment" });

    [Authorize, HttpDelete("comments/{id:int}")]
    public async Task<IActionResult> DeleteComment(int id) =>
        await users.DeleteComment(User.Id()!.Value, User.IsInRole("Admin"), id) ? NoContent() : NotFound();
}

/// <summary>İstifadəçinin şəxsi məlumatları: bəyənilənlər və qeydlər.</summary>
[ApiController, Route("api"), Authorize]
public class UserApiController(UserService users) : ControllerBase
{
    int Uid => User.Id()!.Value;

    [HttpGet("wishlist")]
    public Task<PageDto<AyahDto>> Wishlist(int offset = 0, int limit = 20, string? lang = null) => users.Wishlist(Uid, offset, limit, lang ?? Request.Lang());

    [HttpPost("wishlist/{ayahId:int}")]
    public async Task<IActionResult> Toggle(int ayahId) => await users.ToggleFavorite(Uid, ayahId) is { } on ? Ok(new { on }) : NotFound();

    [HttpGet("notes")] public Task<PageDto<NoteDto>> Notes(int offset = 0, int limit = 20) => users.Notes(Uid, offset, limit);

    [HttpPost("notes")]
    public async Task<IActionResult> AddNote(NoteReq r) => await users.AddNote(Uid, r) is { } err ? BadRequest(new { err }) : Ok();

    [HttpPut("notes/{id:int}")]
    public async Task<IActionResult> UpdateNote(int id, TextReq r) => await users.UpdateNote(Uid, id, r.Text) ? Ok() : BadRequest();

    [HttpDelete("notes/{id:int}")]
    public async Task<IActionResult> DeleteNote(int id) => await users.DeleteNote(Uid, id) ? NoContent() : NotFound();
}

/// <summary>Admin paneli: ayələri SQL-də əlavə et / redaktə et / sil.</summary>
[ApiController, Route("api/admin/ayahs"), Authorize(Roles = "Admin")]
public class AdminApiController(AdminService admin) : ControllerBase
{
    [HttpGet] public Task<PageDto<AdminAyahDto>> List(int surah = 1, string? q = null, int page = 1) => admin.List(surah, q, page);
    [HttpGet("next/{surah:int}")] public Task<int> Next(int surah) => admin.NextNumber(surah);
    [HttpGet("{id:int}")] public async Task<IActionResult> Get(int id) => await admin.Get(id) is { } a ? Ok(a) : NotFound();

    [HttpPost]
    public async Task<IActionResult> Create(AdminAyahReq r) { var (a, e) = await admin.Create(r); return a != null ? Ok(a) : BadRequest(new { err = e }); }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, AdminAyahReq r) { var (a, e) = await admin.Update(id, r); return a != null ? Ok(a) : BadRequest(new { err = e }); }

    [HttpDelete("{id:int}")] public async Task<IActionResult> Delete(int id) => await admin.Delete(id) ? NoContent() : NotFound();
}

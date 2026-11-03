namespace QuranApp.Models;

public record SurahDto(int N, string Name, int Count);
public record WordDto(string Ar, string Lat, string Mean);
public record AyahDto(int Id, int Surah, int Num, string Ref, string Arabic, string Translation, List<WordDto> Words, bool Fav, int Comments);
public record PageDto<T>(List<T> Items, int Total);
public record NoteDto(int Id, string Ref, string Arabic, string Text, DateTime Date);
public record CommentDto(int Id, string User, string Text, DateTime Date, bool Mine);

public record LoginReq(string Email, string Password, bool Remember);
public record RegisterReq(string Name, string Email, string Password);
public record NoteReq(string Ref, string Text);
public record TextReq(string Text);

// Admin
public record AdminWordDto(string Ar, string Lat, Dictionary<string, string> Mean);
public record AdminAyahDto(int Id, int Surah, int Num, string Ref, string Arabic, Dictionary<string, string> Tr, List<AdminWordDto> Words);
public record AdminAyahReq(int Surah, int Num, string Arabic, Dictionary<string, string> Tr, List<AdminWordDto> Words);

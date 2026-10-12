using System.Text.RegularExpressions;
namespace MediaPlayback;
internal static class MediaMetadata
{
    public static string Title(string value) => Regex.Replace(value.Trim(), @"^(歌曲|歌名|曲目|song|title)\s*[:：]\s*", "", RegexOptions.IgnoreCase).Trim();
    public static string Base(string value) => Regex.Replace(value, @"[（(][^）)]*[）)]", "").Trim();
    public static string[] Artists(string value) => Regex.Split(Base(value), @"\s*(?:/|&|、|;|；|,|，|\bfeat\.?\b|\bfeaturing\b)\s*", RegexOptions.IgnoreCase).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
    static string Key(string value) => new(Base(Title(value)).Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
    public static bool SameTitle(string expected, string? candidate) => candidate != null && Key(expected).Length > 0 && Key(expected) == Key(candidate);
    public static bool SameArtist(string expected, string? candidate) => candidate != null && Artists(expected).Any(a => Artists(candidate).Any(b => Key(a).Length > 0 && Key(a) == Key(b)));
    public static bool SameDuration(TimeSpan expected, double seconds) => expected <= TimeSpan.Zero || seconds <= 0 || Math.Abs(expected.TotalSeconds - seconds) <= 8;
}

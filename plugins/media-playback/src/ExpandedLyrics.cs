using System.Globalization;
namespace MediaPlayback;
internal static class ExpandedLyrics
{
    public static string[] Wrap(string text, Func<string, bool> fits)
    {
        var tokens = new List<string>();
        var e = StringInfo.GetTextElementEnumerator(text.Trim());
        string word = "";
        while (e.MoveNext())
        {
            var c = e.GetTextElement();
            bool latin = c.All(x => x < 128 && !char.IsWhiteSpace(x));
            if (latin) { word += c; continue; }
            if (word.Length > 0) { tokens.Add(word); word = ""; }
            tokens.Add(c);
        }
        if (word.Length > 0) tokens.Add(word);
        var result = new List<string>(); string line = "";
        foreach (var token in tokens)
        {
            if (line.Length > 0 && !fits(line + token)) { result.Add(line.Trim()); line = ""; }
            line += token;
        }
        if (!string.IsNullOrWhiteSpace(line)) result.Add(line.Trim());
        return result.Where(x => x.Length > 0).ToArray();
    }
    public static IReadOnlyList<LyricLine> Build(IReadOnlyList<LyricLine> source, TimeSpan duration, Func<string, bool> fits)
    {
        var result = new List<LyricLine>();
        for (int i = 0; i < source.Count; i++)
        {
            var parts = Wrap(source[i].Text, fits);
            var end = i + 1 < source.Count ? source[i + 1].Time : duration;
            double span = Math.Max(0, (end - source[i].Time).TotalMilliseconds);
            for (int j = 0; j < parts.Length; j++) result.Add(new(source[i].Time + TimeSpan.FromMilliseconds(span * j / parts.Length), parts[j]));
        }
        return result;
    }
    public static string[] Window(IReadOnlyList<LyricLine> lines, int current) => Enumerable.Range(current - 1, 4).Select(i => i >= 0 && i < lines.Count ? lines[i].Text : "").ToArray();
}

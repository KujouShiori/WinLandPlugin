using System.Globalization;
namespace MediaPlayback;
internal static class CompactLyrics
{
    public static int ActiveIndex(IReadOnlyList<LyricLine>? lines, TimeSpan position)
    {
        int index = -1;
        if (lines != null) for (int i = 0; i < lines.Count && lines[i].Time <= position; i++) index = i;
        return index;
    }
    public static string[] Wrap(string text, Func<string, bool>? fitsEnglish = null)
    {
        var result = new List<string>();
        bool cjk = text.Any(c => c is >= '\u3040' and <= '\u30ff' or >= '\u3400' and <= '\u9fff' or >= '\uac00' and <= '\ud7af');
        if (!cjk)
        {
            string line = "";
            foreach (var word in text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            {
                // Pack the whole sentence when it fits; preserve spaces and word boundaries.
                if (line.Length > 0 && !(fitsEnglish?.Invoke(line + " " + word) ?? (new StringInfo(line + " " + word).LengthInTextElements <= 36))) { result.Add(line); line = ""; }
                line = line.Length == 0 ? word : line + " " + word;
            }
            if (line.Length > 0) result.Add(line);
        }
        else foreach (var phrase in text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
        {
            var chars = new List<string>();
            var e = StringInfo.GetTextElementEnumerator(phrase);
            while (e.MoveNext()) chars.Add(e.GetTextElement());
            for (int i = 0; i < chars.Count; i += 8) result.Add(string.Concat(chars.Skip(i).Take(8)));
        }
        return result.ToArray();
    }    public static IReadOnlyList<LyricLine> Build(IReadOnlyList<LyricLine> source, TimeSpan duration, Func<string, bool>? fitsEnglish = null)
    {
        var result = new List<LyricLine>();
        for (int i = 0; i < source.Count; i++)
        {
            var parts = Wrap(source[i].Text, fitsEnglish);
            var end = i + 1 < source.Count ? source[i + 1].Time : duration;
            double span = Math.Max(0, (end - source[i].Time).TotalMilliseconds);
            for (int j = 0; j < parts.Length; j++) result.Add(new(source[i].Time + TimeSpan.FromMilliseconds(span * j / parts.Length), parts[j]));
        }
        return result;
    }
}





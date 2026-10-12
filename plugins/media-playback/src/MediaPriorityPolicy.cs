namespace MediaPlayback;

/// <summary>Elapsed time is monotonic so system clock changes cannot affect the grace period.</summary>
internal sealed class MediaPriorityPolicy
{
    private TimeSpan? _idleSince;

    public int Evaluate(bool playing, TimeSpan now, bool automatic, int playingPriority, int idlePriority)
    {
        if (playing) _idleSince = null;
        else _idleSince ??= now;

        return automatic && !playing && now - _idleSince!.Value >= TimeSpan.FromMinutes(1)
            ? idlePriority : playingPriority;
    }
}

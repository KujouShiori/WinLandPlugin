
using System.ComponentModel;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using WinIsland.Core;

namespace MediaPlayback;

internal sealed class MediaIslandView : UserControl, IMorphView, IDisposable
{
    readonly MediaViewModel vm;
    readonly AudioLevelMonitor? levels;
    readonly LyricsService? lyrics;
    readonly IIslandTheme theme;
    readonly Grid root = new();
    readonly Border cover = new() { CornerRadius = new(8), Width = 32, Height = 32, VerticalAlignment = VerticalAlignment.Center };
    readonly Image image = new() { Stretch = Stretch.UniformToFill };
    readonly TextBlock title = new() { TextTrimming = TextTrimming.CharacterEllipsis, FontSize = 16 };
    readonly TextBlock artist = new() { FontSize = 13, TextTrimming = TextTrimming.CharacterEllipsis };
    readonly TextBlock sourceApp = new() { FontSize = 10, TextTrimming = TextTrimming.CharacterEllipsis };
    readonly StackPanel metadata = new() { Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
    readonly Grid lyricViewport = new() { Height = 36, VerticalAlignment = VerticalAlignment.Center };
    readonly Grid lyricStack = new();
    
    readonly TextBlock[] rows = new TextBlock[3];
    readonly TranslateTransform[] lineTransforms = new TranslateTransform[3];
    readonly Grid expandedViewport = new() { Height = 72, VerticalAlignment = VerticalAlignment.Center, Opacity = 0, IsHitTestVisible = false };
    readonly Border lyricDivider = new() { Width = 1, Height = 72, VerticalAlignment = VerticalAlignment.Center, Opacity = 0 };
    readonly TextBlock[] expandedRows = new TextBlock[5];
    readonly TranslateTransform[] expandedTransforms = new TranslateTransform[5];
    readonly ScaleTransform[] expandedScales = new ScaleTransform[5];
    readonly SolidColorBrush dividerBrush = new(), expandedNextBrush = new();
    readonly ColumnDefinition expandedColumn = new() { Width = new(0) };
    IReadOnlyList<LyricLine>? rawLines, expandedLines;
    int expandedIndex = -2;
    double expandedWidth;
    readonly Canvas sharedLayer = new() { IsHitTestVisible = false };
    readonly TextBlock[] sharedLyrics = new TextBlock[2];
    readonly TranslateTransform compactMotion = new(), expandedMotion = new(), metadataMotion = new();
    readonly Point[] compactOrigins = new Point[2];
    readonly int[] sharedTargets = { -1, -1 };
    readonly StackPanel detail = new() { Height = 0, Opacity = 0, Spacing = 2 };
    readonly FontIcon playIcon = new() { FontSize = 20 };
    readonly Grid progress = new() { Height = 14, Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent) };
    readonly Border progressFill = new() { Height = 4, CornerRadius = new(2), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center };
    readonly Ellipse progressKnob = new() { Width = 9, Height = 9, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center };
    double progressValue;
    readonly TextBlock elapsed = new() { FontSize = 10 };
    readonly TextBlock duration = new() { FontSize = 10 };
    readonly SolidColorBrush foreground = new(), secondary = new(), muted = new(), transitionBrush = new();
    readonly Ellipse[] glows = new Ellipse[3];
    readonly ScaleTransform[] rowScales = new ScaleTransform[3];
    readonly DispatcherQueueTimer timer;
    readonly double[] glowEnergy = new double[3];
    IReadOnlyList<LyricLine>? lines;
    CancellationTokenSource? fetch;
    string track = "";
    DateTimeOffset retryLyrics = DateTimeOffset.MaxValue;
    int index = -2;
    double morph, from, target, morphMs;
    DateTimeOffset morphStart, lastFrame, lastTimeline, positionSample;
    bool morphing, dragging, disposed;
    readonly bool large;
    readonly Button prevSession, nextSession;

    public MediaIslandView(MediaViewModel model, AudioLevelMonitor? monitor, LyricsService? service, IIslandTheme islandTheme, bool spotlight = false)
    {
        vm = model; levels = monitor; lyrics = service; theme = islandTheme; large = spotlight;
        HorizontalContentAlignment = HorizontalAlignment.Stretch;
        VerticalContentAlignment = VerticalAlignment.Stretch;
        root.Padding = new(8, 0, 12, 0);
        root.RowDefinitions.Add(new() { Height = new(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new() { Height = GridLength.Auto });
        root.RowDefinitions.Add(new() { Height = GridLength.Auto });
        var glowLayer = new Grid { IsHitTestVisible = false, Opacity = 1 };
        Grid.SetRowSpan(glowLayer, 2); root.Children.Add(glowLayer);
        var colors = new[] { Windows.UI.Color.FromArgb(192, 99, 102, 241), Windows.UI.Color.FromArgb(176, 20, 184, 166), Windows.UI.Color.FromArgb(160, 244, 114, 176) };
        for (int i = 0; i < glows.Length; i++)
        {
            var brush = new RadialGradientBrush();
            brush.GradientStops.Add(new GradientStop { Color = colors[i], Offset = 0 });
            var faded = colors[i]; faded.A = (byte)(i == 0 ? 64 : 48);
            brush.GradientStops.Add(new GradientStop { Color = faded, Offset = .3 });
            faded.A = 0; brush.GradientStops.Add(new GradientStop { Color = faded, Offset = i == 0 ? .75 : i == 1 ? .7 : .65 });
            glows[i] = new Ellipse { Fill = brush, RenderTransformOrigin = i == 0 ? new(.3, .5) : i == 1 ? new(.7, .4) : new(.5, .7), RenderTransform = new ScaleTransform(), HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
            glowLayer.Children.Add(glows[i]);
        }        var header = new Grid { ColumnSpacing = 0 };
        header.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
        header.ColumnDefinitions.Add(new() { Width = new(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new() { Width = GridLength.Auto }); header.ColumnDefinitions.Add(expandedColumn);
        lyricDivider.Background = dividerBrush; Grid.SetColumn(lyricDivider, 2); header.Children.Add(lyricDivider);
        Grid.SetColumn(expandedViewport, 3); header.Children.Add(expandedViewport);
        for (int i = 0; i < expandedRows.Length; i++) {
            expandedTransforms[i] = new() { Y = i * 18 };
            expandedScales[i] = new() { ScaleX = i == 1 ? 1 : 12d / 13, ScaleY = i == 1 ? 1 : 12d / 13 };
            var expandedTransform = new TransformGroup(); expandedTransform.Children.Add(expandedScales[i]); expandedTransform.Children.Add(expandedTransforms[i]);
            expandedRows[i] = new TextBlock { Height = 18, FontSize = 13, VerticalAlignment = VerticalAlignment.Top, TextWrapping = TextWrapping.NoWrap, Foreground = i == 1 ? foreground : secondary, RenderTransform = expandedTransform };
            expandedViewport.Children.Add(expandedRows[i]);
        }
        expandedViewport.SizeChanged += (_, _) => { expandedViewport.Clip = new RectangleGeometry { Rect = new(0, 0, Math.Max(0, expandedViewport.ActualWidth), 72) }; if (!morphing && morph > .99) RebuildExpanded(); };
        var coverContent = new Grid();
        coverContent.Children.Add(new FontIcon { Glyph = "\uE8D6", FontSize = 18, Foreground = secondary });
        coverContent.Children.Add(image); cover.Child = coverContent; header.Children.Add(cover);
        var textArea = new Grid { Margin = new(10, 0, 0, 0) }; Grid.SetColumn(textArea, 1); header.Children.Add(textArea);
        metadata.Children.Add(title); metadata.Children.Add(artist); metadata.Children.Add(sourceApp);
        textArea.Children.Add(metadata);

        for (int i = 0; i < rows.Length; i++)
        {
            lineTransforms[i] = new() { Y = i * 18 };
            rowScales[i] = new() { ScaleX = i == 0 ? 1 : 10d / 12, ScaleY = i == 0 ? 1 : 10d / 12 };
            var transforms = new TransformGroup(); transforms.Children.Add(rowScales[i]); transforms.Children.Add(lineTransforms[i]);
            rows[i] = new TextBlock { Height = 18, VerticalAlignment = VerticalAlignment.Top, FontSize = 12, TextWrapping = TextWrapping.NoWrap, Foreground = i == 0 ? foreground : secondary, RenderTransform = transforms };
            lyricStack.Children.Add(rows[i]);
        }
        lyricViewport.Children.Add(lyricStack); textArea.Children.Add(lyricViewport);
        root.Children.Add(header);
        var controls = new Grid { Height = 40 };
        controls.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
        controls.ColumnDefinitions.Add(new() { Width = new(1, GridUnitType.Star) });
        controls.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
        prevSession = Button("\uE76B", () => vm.SwitchToPrevSession?.Invoke(), "上一个播放器");
        nextSession = Button("\uE76C", () => vm.SwitchToNextSession?.Invoke(), "下一个播放器");
        controls.Children.Add(prevSession); Grid.SetColumn(nextSession, 2); controls.Children.Add(nextSession);
        var transport = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 18, HorizontalAlignment = HorizontalAlignment.Center };
        transport.Children.Add(Button("\uE892", () => vm.SkipPrevious?.Invoke(), "上一首"));
        var play = Button("", () => vm.TogglePlayPause?.Invoke(), "播放 / 暂停"); play.Content = playIcon; transport.Children.Add(play);
        transport.Children.Add(Button("\uE893", () => vm.SkipNext?.Invoke(), "下一首"));
        Grid.SetColumn(transport, 1); controls.Children.Add(transport); detail.Children.Add(controls);
        var timeRow = new Grid { ColumnSpacing = 8, Height = 24 };
        timeRow.ColumnDefinitions.Add(new() { Width = GridLength.Auto }); timeRow.ColumnDefinitions.Add(new() { Width = new(1, GridUnitType.Star) }); timeRow.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
        elapsed.VerticalAlignment = duration.VerticalAlignment = VerticalAlignment.Center;
        timeRow.Children.Add(elapsed); Grid.SetColumn(progress, 1); timeRow.Children.Add(progress); Grid.SetColumn(duration, 2); timeRow.Children.Add(duration);
        Grid.SetRow(detail, 1); root.Children.Add(detail);
        timeRow.Height = 0; timeRow.Opacity = 0; timeRow.Tag = "timeline"; Grid.SetRow(timeRow, 2); root.Children.Add(timeRow); Content = root;
        Grid.SetRowSpan(sharedLayer, 3); root.Children.Add(sharedLayer);
        for (int i = 0; i < sharedLyrics.Length; i++) { sharedLyrics[i] = new TextBlock { FontSize = 12, Foreground = foreground, Opacity = 0, RenderTransform = new CompositeTransform() }; sharedLayer.Children.Add(sharedLyrics[i]); }
        lyricViewport.RenderTransform = compactMotion; expandedViewport.RenderTransform = expandedMotion; metadata.RenderTransform = metadataMotion;
        progress.Children.Add(new Border { Height = 4, CornerRadius = new(2), Background = muted, VerticalAlignment = VerticalAlignment.Center });
        progressFill.Background = secondary; progressKnob.Fill = foreground; progress.Children.Add(progressFill); progress.Children.Add(progressKnob);
        progress.SizeChanged += (_, _) => UpdateProgress();
        progress.PointerPressed += (_, e) => { dragging = true; progress.CapturePointer(e.Pointer); DragTo(e.GetCurrentPoint(progress).Position.X); e.Handled = true; };
        progress.PointerMoved += (_, e) => { if (dragging) DragTo(e.GetCurrentPoint(progress).Position.X); };
        progress.PointerReleased += (_, e) => { if (dragging) { DragTo(e.GetCurrentPoint(progress).Position.X); dragging = false; vm.SeekTo?.Invoke(TimeSpan.FromSeconds(progressValue * vm.Duration.TotalSeconds)); progress.ReleasePointerCapture(e.Pointer); } e.Handled = true; };
        progress.PointerCaptureLost += (_, _) => dragging = false;
        progress.Tapped += (_, e) => e.Handled = true;        ApplyTheme(); theme.Changed += ApplyTheme;
        vm.PropertyChanged += Changed;
        timer = DispatcherQueue.CreateTimer(); timer.Interval = TimeSpan.FromMilliseconds(16); timer.Tick += Tick;
        Loaded += (_, _) => { lastFrame = DateTimeOffset.UtcNow; timer.Start(); Update(); };
        Unloaded += (_, _) => timer.Stop();
        SizeChanged += (_, _) => ApplyClip(); lyricViewport.SizeChanged += (_, _) => ApplyClip();
        prevSession.Visibility = nextSession.Visibility = vm.SwitcherVisibility;
        ApplyMorph(spotlight ? 1 : 0); Update();
    }

    Button Button(string glyph, Action action, string label)
    {
        var b = new Button { Content = new FontIcon { Glyph = glyph, FontSize = 16, Foreground = foreground }, Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent), BorderThickness = new(0), CornerRadius = new(20), Padding = new(10, 6, 10, 6), Foreground = foreground };
        ToolTipService.SetToolTip(b, label); Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(b, label);
        b.Click += (_, _) => action(); b.Tapped += (_, e) => e.Handled = true; return b;
    }
    public UIElement View => this;
    public void AnimateToExpanded(TimeSpan d) => StartMorph(1, d);
    public void AnimateToCompact(TimeSpan d) => StartMorph(0, d);
    void StartMorph(double t, TimeSpan d) { CaptureSharedLyrics(); from = morph; target = large ? 1 : t; morphMs = Math.Max(1, d.TotalMilliseconds); morphStart = DateTimeOffset.UtcNow; morphing = true; timer.Start(); }
    void ApplyMorph(double p)
    {
        morph = p; double q = Math.Clamp(p, 0, 1);
        cover.Width = cover.Height = 32 + 40 * q;
        title.FontSize = 12 + 3 * q; title.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold;
        cover.CornerRadius = new(8 + 6 * q);
        detail.Height = 40 * q;
        foreach (var child in root.Children) if (child is Grid g && Equals(g.Tag, "timeline")) { g.Height = 14 * q; g.Opacity = q; g.IsHitTestVisible = q > .95; } detail.Opacity = q; detail.IsHitTestVisible = q > .95;
        metadata.Opacity = q; lyricViewport.Opacity = 1 - q;
        expandedViewport.Opacity = lyricDivider.Opacity = q;
        lyricDivider.Width = q; lyricDivider.Margin = new(10 * q, 0, 10 * q, 0);
        expandedColumn.Width = q <= .001 ? new GridLength(0) : new GridLength(1.25 * q, GridUnitType.Star);
        metadata.IsHitTestVisible = q > .95;
        compactMotion.X = 20 * q; compactMotion.Y = -4 * q;
        expandedMotion.X = -20 * (1 - q); expandedMotion.Y = 4 * (1 - q);
        metadataMotion.Y = 8 * (1 - q);
        root.Padding = new(8 + 8 * q, 2 + 8 * q, 14 + 2 * q, 2 + 6 * q);
    }
    void Changed(object? sender, PropertyChangedEventArgs args) { if (args.PropertyName == nameof(vm.Position)) positionSample = DateTimeOffset.UtcNow; if (!disposed) Update(); }
    void Update()
    {
        title.Text = vm.Title; artist.Text = vm.Artist; sourceApp.Text = vm.SourceAppName; image.Source = vm.Thumbnail; playIcon.Glyph = vm.PlayPauseGlyph;
        prevSession.Visibility = nextSession.Visibility = vm.SwitcherVisibility;
        elapsed.Text = vm.PositionText; duration.Text = vm.DurationText;
        if (!dragging) { progressValue = vm.Progress; UpdateProgress(); }
        string key = $"{vm.Title}\n{vm.Artist}\n{(int)vm.Duration.TotalSeconds}\n{vm.LyricsEnabled}";
        if (key != track || (lines == null && DateTimeOffset.UtcNow >= retryLyrics)) { retryLyrics = DateTimeOffset.MaxValue; track = key; lines = null; rawLines = expandedLines = null; expandedIndex = -2; index = -2; fetch?.Cancel(); fetch?.Dispose(); fetch = new(); _ = FetchLyrics(key, fetch.Token); }
        UpdateLines();
    }
    async Task FetchLyrics(string key, CancellationToken token)
    {
        try
        {
            await Task.Delay(180, token);
            if (vm.LyricsEnabled && lyrics != null && !string.IsNullOrWhiteSpace(vm.Title))
            {
                var result = await lyrics.FetchAsync(vm.Title, vm.Artist, vm.Duration, token);
                if (token.IsCancellationRequested || disposed || track != key) return;
                rawLines = result; expandedWidth = 0; RebuildExpanded();
                lines = result == null ? null : CompactLyrics.Build(result, vm.Duration, FitsEnglish);
                retryLyrics = lines is { Count: > 0 } ? DateTimeOffset.MaxValue : DateTimeOffset.UtcNow.AddSeconds(20);
                index = -2; UpdateLines();
            }
        }
        catch (OperationCanceledException) { }
    }
    void UpdateProgress()
    {
        double width = Math.Max(0, progress.ActualWidth);
        progressFill.Width = width * progressValue;
        progressKnob.Margin = new(Math.Clamp(width * progressValue - 4.5, 0, Math.Max(0, width - 9)), 0, 0, 0);
    }
    void DragTo(double x)
    {
        progressValue = Math.Clamp(x / Math.Max(1, progress.ActualWidth), 0, 1);
        UpdateProgress(); elapsed.Text = MediaViewModel.FormatTime(TimeSpan.FromSeconds(progressValue * vm.Duration.TotalSeconds));
    }
    bool FitsEnglish(string text)
    {
        var measure = new TextBlock { Text = text, FontSize = 12, FontFamily = rows[0].FontFamily };
        measure.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        return measure.DesiredSize.Width <= Math.Clamp(lyricViewport.ActualWidth - 2, 160, 214);
    }
    void UpdateLines()
    {
        int next = CompactLyrics.ActiveIndex(lines, vm.Position + TimeSpan.FromSeconds(vm.LyricsOffsetSeconds));
        if (next == index) return;

        index = next;

        if (lines is { Count: > 0 })
        {
            int first = Math.Max(0, next);
            for (int i = 0; i < 3; i++) rows[i].Text = first + i < lines.Count ? lines[first + i].Text : "";
        }
        else { var fallback = CompactLyrics.Wrap(vm.Title); rows[0].Text = fallback.FirstOrDefault() ?? ""; rows[1].Text = fallback.Skip(1).FirstOrDefault() ?? CompactLyrics.Wrap(vm.Artist).FirstOrDefault() ?? ""; rows[2].Text = ""; }
        for (int i = 0; i < rows.Length; i++) { lineTransforms[i].X = 0; lineTransforms[i].Y = i * 18; rowScales[i].ScaleX = rowScales[i].ScaleY = i == 0 ? 1 : 10d / 12; rows[i].Opacity = 1; rows[i].Foreground = i == 0 ? foreground : secondary; }
    }
    void Tick(DispatcherQueueTimer sender, object args)
    {
        if (disposed) return;
        foreach (var row in rows) row.Opacity = 1; title.Opacity = artist.Opacity = 1;
        for (int i = 0; i < expandedRows.Length; i++) expandedRows[i].Opacity = i == 0 ? .55 : i == 3 ? .65 : 1;
        var now = DateTimeOffset.UtcNow;
        if (morphing)
        {
            double t = Math.Clamp((now - morphStart).TotalMilliseconds / morphMs, 0, 1), d = t - 1;
            ApplyMorph(from + (target - from) * (1 + 1.45 * d * d * d + .45 * d * d));
            if (t >= 1) { morphing = false; ApplyMorph(target); if (target > .99) RebuildExpanded(); }
        }
        if ((now - lastTimeline).TotalMilliseconds >= 200) { lastTimeline = now; vm.RefreshTimeline?.Invoke(); }
        double displayed = vm.Position.TotalSeconds + vm.LyricsOffsetSeconds;
        if (vm.IsPlaying) displayed += Math.Clamp((now - positionSample).TotalSeconds, 0, .2);
        double transition = 0;
        if (vm.IsPlaying && lines != null && index >= 0 && index + 1 < lines.Count)
        {
            double nextTime = lines[index + 1].Time.TotalSeconds;
            double t = Math.Clamp((displayed - nextTime + .32) / .32, 0, 1);
            transition = t * t * (3 - 2 * t);
        }
        for (int i = 0; i < rows.Length; i++) lineTransforms[i].Y = i * 18 - 18 * transition;
        rows[0].Opacity = 1 - transition;
        rowScales[1].ScaleX = rowScales[1].ScaleY = (10 + 2 * transition) / 12;
        var color = secondary.Color; color.A = (byte)(170 + 70 * transition); transitionBrush.Color = color; rows[1].Foreground = transitionBrush;
        RenderExpanded(displayed);
        RenderSharedLyrics();
        if ((now - lastFrame).TotalMilliseconds >= 33) { lastFrame = now; DrawGlow(); }
    }    void CaptureSharedLyrics()
    {
        if (!morphing)
        {
            for (int i = 0; i < 2; i++)
            {
                if (morph < .1) compactOrigins[i] = rows[i].TransformToVisual(sharedLayer).TransformPoint(new Point());
                sharedTargets[i] = -1;
                for (int j = 0; j < 4; j++) if (!string.IsNullOrEmpty(rows[i].Text) && rows[i].Text == expandedRows[j].Text) { sharedTargets[i] = j; break; }
                if (sharedTargets[i] < 0 && rows[i].Text == title.Text && !string.IsNullOrEmpty(title.Text)) sharedTargets[i] = 5;
                if (sharedTargets[i] < 0 && rows[i].Text == artist.Text && !string.IsNullOrEmpty(artist.Text)) sharedTargets[i] = 6;
                sharedLyrics[i].Text = rows[i].Text;
            }
        }
    }
    void RenderSharedLyrics()
    {
        double q = Math.Clamp(morph, 0, 1);
        if (!morphing || q <= .001 || q >= .999)
        {
            foreach (var text in sharedLyrics) text.Opacity = 0;
            return;
        }
        for (int i = 0; i < 2; i++)
        {
            int j = sharedTargets[i];
            if (j < 0) { sharedLyrics[i].Opacity = 0; continue; }
            var destination = j == 5 ? title : j == 6 ? artist : expandedRows[j];
            if (sharedLyrics[i].Text != rows[i].Text || sharedLyrics[i].Text != destination.Text) { sharedLyrics[i].Opacity = 0; continue; }
            var end = destination.TransformToVisual(sharedLayer).TransformPoint(new Point());
            var tr = (CompositeTransform)sharedLyrics[i].RenderTransform;
            tr.TranslateX = compactOrigins[i].X + (end.X - compactOrigins[i].X) * q;
            tr.TranslateY = compactOrigins[i].Y + (end.Y - compactOrigins[i].Y) * q;
            double startSize = i == 0 ? 12 : 10, endSize = j == 5 ? 15 : j == 6 || j == 1 ? 13 : 12;
            tr.ScaleX = tr.ScaleY = (startSize + (endSize - startSize) * q) / 12;
            sharedLyrics[i].Opacity = 1;
            rows[i].Opacity = 0; destination.Opacity = 0;
        }
    }
    void RebuildExpanded()
    {
        double width = Math.Max(100, expandedViewport.ActualWidth - 2);
        if (Math.Abs(width - expandedWidth) < 1 && expandedLines != null) return;
        expandedWidth = width;
        var measure = new TextBlock { FontSize = 13, FontFamily = title.FontFamily };
        bool Fits(string text) { measure.Text = text; measure.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity)); return measure.DesiredSize.Width <= width; }
        expandedLines = rawLines == null ? null : ExpandedLyrics.Build(rawLines, vm.Duration, Fits);
        expandedIndex = -2;
    }
    void RenderExpanded(double displayed)
    {
        int current = CompactLyrics.ActiveIndex(expandedLines, TimeSpan.FromSeconds(Math.Max(0, displayed)));
        if (current != expandedIndex)
        {
            expandedIndex = current;
            for (int i = 0; i < expandedRows.Length; i++)
            {
                int n = current - 1 + i;
                expandedRows[i].Text = expandedLines != null && n >= 0 && n < expandedLines.Count ? expandedLines[n].Text : "";
                expandedRows[i].FontWeight = Microsoft.UI.Text.FontWeights.SemiBold;
            }
            if (expandedLines is not { Count: > 0 }) expandedRows[1].Text = vm.LyricsEnabled ? "暂无同步歌词" : "歌词已关闭";
        }
        double shift = 0;
        if (vm.IsPlaying && expandedLines != null && current >= 0 && current + 1 < expandedLines.Count)
        {
            double interval = (expandedLines[current + 1].Time - expandedLines[current].Time).TotalSeconds;
            double duration = Math.Min(.48, Math.Max(.08, interval * .45));
            double t = Math.Clamp((displayed - expandedLines[current + 1].Time.TotalSeconds + duration) / duration, 0, 1);
            // Quintic easing keeps speed and acceleration continuous at both ends.
            shift = t * t * t * (t * (t * 6 - 15) + 10);
        }
        for (int i = 0; i < expandedRows.Length; i++)
        {
            expandedTransforms[i].Y = i * 18 - 18 * shift;
            double startOpacity = i == 0 ? .5 : i == 1 ? 1 : i == 2 ? .65 : i == 3 ? .4 : 0;
            double endOpacity = i == 0 ? 0 : i == 1 ? .5 : i == 2 ? 1 : i == 3 ? .65 : .4;
            expandedRows[i].Opacity = startOpacity + (endOpacity - startOpacity) * shift;
            double startSize = i == 1 ? 13 : 12, endSize = i == 2 ? 13 : 12;
            expandedScales[i].ScaleX = expandedScales[i].ScaleY = (startSize + (endSize - startSize) * shift) / 13;
            expandedRows[i].Foreground = foreground;
        }
    }    void DrawGlow()
    {
        double w = root.ActualWidth, h = root.ActualHeight;
        if (w <= 0 || h <= 0) return;
        double[] bands = { levels?.Bass ?? 0, levels?.Mid ?? 0, levels?.Treble ?? 0 };
        for (int i = 0; i < glows.Length; i++)
        {
            double signal = vm.IsPlaying && vm.GlowEnabled ? Math.Clamp(bands[i], 0, 1) : 0;
            glowEnergy[i] += (signal - glowEnergy[i]) * (i == 0 ? .45 : i == 1 ? .40 : .38);
    
            var scale = (ScaleTransform)glows[i].RenderTransform;
            scale.ScaleX = scale.ScaleY = (i == 0 ? .50 : i == 1 ? .45 : .40) + glowEnergy[i] * (i == 0 ? 1.8 : i == 1 ? 1.4 : 1);
            glows[i].Opacity = vm.GlowEnabled && vm.IsPlaying ? (i == 0 ? .40 : i == 1 ? .35 : .30) + glowEnergy[i] * (i == 0 ? .60 : i == 1 ? .55 : .50) : 0;
        }
    }    void ApplyClip()
    {
        root.Clip = new RectangleGeometry { Rect = new(0, 0, Math.Max(0, root.ActualWidth), Math.Max(0, root.ActualHeight)) };
        lyricViewport.Clip = new RectangleGeometry { Rect = new(0, 0, Math.Max(0, lyricViewport.ActualWidth), 36) };
    }
    void ApplyTheme()
    {
        byte v = theme.IsLight ? (byte)0 : (byte)255;
        foreground.Color = Windows.UI.Color.FromArgb(240, v, v, v); secondary.Color = Windows.UI.Color.FromArgb(170, v, v, v); muted.Color = Windows.UI.Color.FromArgb(30, v, v, v);
        dividerBrush.Color = Windows.UI.Color.FromArgb(100, v, v, v);
        title.Foreground = foreground; artist.Foreground = sourceApp.Foreground = elapsed.Foreground = duration.Foreground = secondary; cover.Background = muted; playIcon.Foreground = foreground;
    }
    public void Dispose() { if (disposed) return; disposed = true; timer.Stop(); fetch?.Cancel(); fetch?.Dispose(); vm.PropertyChanged -= Changed; theme.Changed -= ApplyTheme; }
}




















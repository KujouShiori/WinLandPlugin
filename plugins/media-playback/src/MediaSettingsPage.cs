using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinIsland.Core;

namespace MediaPlayback;

internal sealed class MediaSettingsPage : UserControl
{
    public MediaSettingsPage(IPluginContext context, MediaPlugin plugin)
    {
        var panel = new StackPanel { Spacing = 16, Padding = new(24), MaxWidth = 680, HorizontalAlignment = HorizontalAlignment.Left };
        panel.Children.Add(new TextBlock { Text = "媒体播放", FontSize = 26 });
        panel.Children.Add(new TextBlock { Text = "先在音乐软件中播放一首歌。小部件会读取 Windows 系统媒体会话；鼠标悬停可展开播放控制。若与原生播放器重复显示，可在 WinIsland 设置中关闭原生媒体模块。", TextWrapping = TextWrapping.Wrap });
        AddToggle("enabled", "启用媒体播放", true);
        AddToggle("lyrics", "同步歌词（收起两排 / 展开四排）", true);
        panel.Children.Add(new TextBlock { Text = "歌词按歌名、歌手匹配：向 QQ 音乐 / 网易云 / LRCLIB 检索，缓存在本地。匹配不到时显示歌名和歌手；音乐软件必须提供系统媒体会话与播放进度。", TextWrapping = TextWrapping.Wrap, Opacity = .7 });
        var offsetLabel = new TextBlock();
        var offset = new Slider { Minimum = -10, Maximum = 10, StepFrequency = .1, Width = 320, HorizontalAlignment = HorizontalAlignment.Left, Value = context.Settings.Get("lyrics_offset", 0d) };
        void DescribeOffset() => offsetLabel.Text = $"歌词校准：{offset.Value:+0.0;-0.0;0.0} 秒（正值提前）";
        DescribeOffset();
        offset.ValueChanged += (_, _) => { DescribeOffset(); context.Settings.Set("lyrics_offset", offset.Value); };
        panel.Children.Add(offsetLabel); panel.Children.Add(offset);
        AddToggle("glow", "音乐光束背景", true);
        panel.Children.Add(new TextBlock { Text = "光束分析系统扬声器输出，不使用麦克风，不录音、不上传音频。其他程序的声音也可能影响光束。", TextWrapping = TextWrapping.Wrap, Opacity = .7 });
        var refresh = new Button { Content = "重新检测播放器" }; refresh.Click += async (_, _) => await plugin.RefreshNowAsync(); panel.Children.Add(refresh);
        Content = new ScrollViewer { Content = panel };

        void AddToggle(string key, string label, bool initial)
        {
            var toggle = new ToggleSwitch { Header = label, IsOn = context.Settings.Get(key, initial) };
            toggle.Toggled += (_, _) => context.Settings.Set(key, toggle.IsOn); panel.Children.Add(toggle);
        }
    }
}





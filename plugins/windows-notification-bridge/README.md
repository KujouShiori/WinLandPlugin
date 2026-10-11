# Windows 通知桥 1.1

把 Windows 应用通知显示为灵动岛卡片：左侧应用图标，右侧应用名、消息标题和正文。背景、圆角和出入动画由 WinIsland 提供，文字跟随岛体明暗主题。

## 使用

1. 安装新版后，在 WinIsland 设置 → 插件管理重新加载「Windows 通知桥」。
2. 打开插件设置，点击「预览通知」。未授权时会出现「允许读取通知」按钮。
3. 点击「关闭系统通知横幅」，进入对应应用的 Windows 通知设置：关闭「显示通知横幅」，保留「在通知中心中显示通知」。**通知总开关保持开启。**
4. 收到新消息后，灵动岛显示卡片，通知中心记录保留。

设置只保留三项：**启用通知、显示消息内容、停留时间**。默认显示 5 秒。旧版的屏蔽应用、移除原通知设置不再使用。

## 范围与限制

本插件使用官方 UserNotificationListener 读取通知中心中的应用 Toast，不是 Windows 通知投递前的拦截器。系统横幅需按以上步骤关闭；插件不能接管系统对话框、应用内部气泡或没有写入通知中心的提示，不能保证转移原通知的点击动作。

应用图标优先从通知来源 AppInfo.DisplayInfo.GetLogo 读取；桌面应用再从 Windows 应用列表解析注册图标。两条路径均不可用时使用铃铛图标；失败不影响正文显示。长标题截断为一行，正文最多两行。隐藏内容时只显示应用名和「收到一条新通知」。

每 250 毫秒检查新通知；不重播启动前或暂停期间的历史通知。新消息按顺序显示，队列最多 100 条，过期两分钟的消息跳过。短于轮询间隔就消失的通知可能漏掉。

消息和图标只暂存内存，不上传、不保存通知正文日志。暂停、禁用或退出会清空队列。插件不会修改系统通知设置或删除通知中心记录。

## 开发

在项目目录运行（保留固定 .NET 10 的 global.json）：

```powershell
dotnet build -c Release -p:InstallPlugin=false
dotnet run --project tests/WindowsMessage.Tests.csproj
pwsh -File scripts/pack.ps1 -SkipBuild
```

安装包：`dist/windows-notification-bridge.lwp`。

如需编译后复制到本机宿主，省略 `-p:InstallPlugin=false`；默认安装目录为 `%LocalAppData%\Programs\WinIsland\plugins\windows-notification-bridge`。运行中的插件需要重新加载才能使用新版。

宿主至少支持 SDK 2.3（用于岛体主题）。不打包 WinUI、Core、WinRT 等宿主运行库。

接口诊断（只报告权限、通知数量及可读取的应用图标数量，不打印消息内容）：

```powershell
dotnet run --project tools/ListenerProbe/ListenerProbe.csproj
```

## 实测

用真实应用发一条消息：检查应用图标、标题和正文；切换岛体深浅外观；连续两条消息应按顺序显示；关闭内容时不暴露标题正文；关闭系统横幅后通知中心仍保留记录。

构建与逻辑测试不能替代真实宿主视觉检查。

[Microsoft 官方通知监听文档](https://learn.microsoft.com/en-us/windows/apps/develop/notifications/app-notifications/notification-listener)


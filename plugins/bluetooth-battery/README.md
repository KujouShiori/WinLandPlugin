# 电量与音量 · WinIsland 插件

在 WinIsland（Windows 灵动岛）上显示**已连接蓝牙设备的电量**和**系统音量**：
小岛第一行是电量最低的那台设备（型号 + 电量），第二行是系统音量（音量条 + 百分比，静音时显示「静音」）；
展开后逐台列出电量，点岛体打开「超级展开」聚光卡查看全部设备、系统音量、手动刷新或跳转系统设置。

小岛效果截图：[screenshots/island-compact.png](screenshots/island-compact.png)

> 蓝牙电量来自 Windows PnP 设备属性里的 `DEVPKEY_Bluetooth_Battery`（与「设置 → 蓝牙」同一份数据）；
> 系统音量来自 Windows Core Audio 默认播放设备。不联网、无第三方依赖、不需要管理员权限。

## 功能

- **小岛**：第一行「蓝牙图标 + 型号 + 电量百分比」（电量最低的设备排最前，任务栏空间小所以不带品牌），
  第二行「喇叭图标 + 音量条 + 音量百分比 / 静音」
- **展开**：最多 3 台设备逐行显示「分类图标 + 完整名称 + 电量条 + 百分比」，电量低的排前面
- **聚光卡**（点岛体）：系统音量卡片 + 完整设备列表、每台设备的蓝牙地址、最后更新时间，
  可「立即刷新」「打开声音设置」「打开蓝牙设置」
- **低电量提醒**：电量掉到阈值以下时在岛上弹一条提醒（每台设备只提醒一次，电量回升/重连后重新计数）
- **可调项**：是否占岛、是否显示系统音量、自动刷新间隔（3~120 秒，默认 5 秒）、
  低电量提醒开关与阈值；音量每秒刷新一次
- **浅色 / 深色适配**：颜色跟随岛体主题（Fluent 跟随系统，Apple 恒为深色）
- 既没有蓝牙设备、系统音量也读不到时**不占用岛**（配合宿主的「空闲时隐藏」，岛会安静收起）

## 设置

设置 → 电量与音量：

| 设置项 | 默认值 | 说明 |
|---|---|---|
| 在灵动岛上显示 | 开 | 关闭后插件照常后台读取，只是不占岛 |
| 显示系统音量 | 开 | 关闭后第二行隐藏，只显示设备电量 |
| 自动刷新间隔（秒） | 5 | 蓝牙电量变化不频繁，5 秒足够；单次读取约 2~4 ms |
| 低电量提醒 | 开 | 弹岛内临时消息 |
| 低电量阈值（%） | 20 | 5~50 可调 |

岛上的优先级（在多个插件同时报事时谁占小岛）可以在「设置 → 插件管理 → 电量与音量」里调整；
本插件声明的默认优先级是 60（会排在剪贴板插件前面；想让它靠后可以在插件管理里调低）。

## 数据来源与限制

### 蓝牙电量

- 读取 PnP 属性集 `{104EA319-6EE2-4701-BD47-8DDBF425BBE5}` 的 pid 2
  （`DEVPKEY_Bluetooth_Battery`，本机实测为 `DEVPROP_TYPE_BYTE`，0~100），用设备实例 ID 里的
  12 位蓝牙地址把「服务节点的电量」与「主设备节点的名字」对上。
- **经典蓝牙耳机**（HFP）由 Hands-Free AG 服务节点上报；**BLE 设备**由 `BTHLE` 设备节点上报，
  能否读到取决于设备/驱动是否支持，读不到时显示 `—`。
- 数值是 **Windows 缓存的上次上报值**（通常连接时上报），不是实时电量计。
- 只显示**当前已连接**的设备：设备断开后 Windows 会移除它的服务节点，也不缓存电量。
- 不是所有蓝牙设备都会上报电量；系统没这份数据时插件不猜。

### 系统音量

- 通过 Core Audio（`IMMDeviceEnumerator` → `IAudioEndpointVolume`）读**默认播放设备**的主音量
  和静音状态，每秒刷新一次；没有播放设备 / 音频服务不可用时隐藏音量行（不显示假数据）。
- 读的是「主音量」，不是每个 App 的独立音量；切换默认播放设备后会自动跟上。
- 只读，不会修改你的音量/静音状态。

### 为什么不支持 2.4G 无线键鼠（已实测调查）

2.4G 键鼠走的是 USB 接收器，**Windows 没有给它们提供任何电量数据**。本次在同一台机器上实测：

| 接收器 | 结论 |
|---|---|
| 通用 HID `Battery Strength`（Usage Page 0x06 / Usage 0x20） | 48 个 HID 顶层集合逐个检查，**0 个设备**实现该标准用量 |
| 罗技 Bolt `046D:C548`（Logi Options+ 在用） | HID++ 2.0 长报文通道可写但**无任何回包**；退出 Logi Options+ 后复测仍然无回包，排除软件占用 |
| Sonix `0C45:FEFE`、Rapoo `24AE:1416`、F108Pro `05AC:024F` 等 | 无标准 HID 电量上报；厂商私有协议无公开文档 |

所以本插件**不显示 2.4G 键鼠电量**（不编造数据），完整调研见 `_research/hid-proto/REPORT.md`。

## 构建与安装

按 WinIsland 插件 SDK 2.0（`luolan.winland.Core` 2.4.0）编写，需要 .NET 10 SDK。
项目根目录的 `global.json` 把 SDK 钉在 10.0.200（`rollForward: latestFeature`），**不要删**。

```powershell
cd bluetooth-battery
dotnet build
```

`BluetoothBattery.csproj` 里的 `WinIslandPluginsDir` 默认指向
`%LocalAppData%\Programs\WinIsland\plugins`，构建成功后插件会被自动拷进
`plugins\bluetooth-battery\`。让插件生效：设置 → 插件管理 →「重新加载」，或重启 WinIsland。

手动安装：把构建输出目录里的 `BluetoothBattery.dll`、`BluetoothBattery.deps.json`、
`BluetoothBattery.pri`、`plugin.json` 拷进 `<WinIsland 安装目录>\plugins\bluetooth-battery\`。

日志：`%LocalAppData%\WinIsland\logs\plugin.bluetooth-battery.log`。

### 打包成 .lwp

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\pack.ps1
```

产出 `..\dist\bluetooth-battery.lwp`，拖进 WinIsland 的 `plugins\` 目录或从
「设置 → 插件管理 → 安装插件包…」安装。包内只有入口 dll / deps.json / pri / plugin.json / README，
不含宿主自带的运行时文件。

## 文件结构

```
bluetooth-battery/
  plugin.json                      清单（id / 名称 / 入口 dll / API 版本）
  BluetoothBattery.csproj          插件工程（自动部署到 WinIsland 插件目录）
  global.json                      钉住 .NET 10 SDK，不能删
  pack.ps1                         打包 .lwp（UTF-8 安全，兼容 Windows PowerShell 5.1）
  BluetoothBatteryPlugin.cs        插件入口：定时读取、低电量提醒、音量刷新、设置与内容注册
  BluetoothBatteryView.cs          岛视图（紧凑两行 + 展开，逐帧形态动画）
  BluetoothBatterySpotlightView.cs 聚光卡（音量卡片 + 完整列表 / 刷新 / 打开系统设置）
  BluetoothBatterySettingsPage.cs  设置页
  Devices/
    BatteryDevice.cs               设备 + 电量快照模型
    BluetoothBatteryReader.cs      PnP 设备树读取（CfgMgr32 P/Invoke）
    DeviceNameFormatter.cs         型号名（去掉品牌词，小岛显示用）
    SystemVolume.cs                Core Audio 读系统主音量 / 静音（COM 互操作）
  Ui/
    IslandPalette.cs               主题调色板（共享画刷，浅色/深色适配）
    BatteryRow.cs                  设备行 + 电量条
```

## 实现要点

- **读取**：`CM_Get_Device_ID_ListW`（只枚举在场设备）+ `CM_Get_DevNode_PropertyW` 读电量，
  按蓝牙地址合并同一台设备的多个节点，名字来自 `BTHENUM\DEV_<地址>` 主节点（绝不用
  `... Hands-Free AG` 这种服务节点名）。实测热读 2~4 ms，后台线程执行。
- **系统音量**：Core Audio COM 在后台线程读取，每秒一次；只读不写，不碰 UI 线程。
- **形态动画**：逐帧给属性赋值（`DispatcherQueueTimer` 16ms + BackEase(EaseOut, 0.45)），
  不用 Storyboard —— 动态加载的插件程序集里 Storyboard 属性路径动画会在 tick 上抛
  `COMException 0x800F1001`。
- **主题**：颜色全在 `IslandPalette` 的共享画刷里，订阅 `IIslandTheme.Changed` 就地重刷。
- **刷新**：`SemaphoreSlim` 串行化，并发请求排队而不是丢弃；停用后入口直接返回，不留幽灵任务。

## 许可证

MIT

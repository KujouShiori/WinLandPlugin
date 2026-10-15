# 硬件监控（hw-monitor）

常驻硬件信息插件：小岛显示你勾选的指标，展开后显示前台窗口帧率、CPU、GPU 与上下传速度。

## 功能

- **小岛（紧凑态）**：CPU 占用 / GPU 占用 / 上下传速度 / 帧率，可在插件设置里勾选（默认 CPU + 上下传）
- **展开态**：前台窗口 FPS、CPU、GPU、上下传速度
- 只在系统里存在多个同类设备时才显示设备选择框（单 GPU / 单网卡机器不会出现无意义选项）

## 数据来源（全部系统 API，无第三方传感器库）

| 指标 | 来源 |
|------|------|
| CPU 名称 / 占用 | 注册表 `ProcessorNameString` + PDH `\Processor Information(*)\% Processor Time`（多路 CPU 可选组） |
| GPU 名称 / 占用 | 注册表显示适配器列表（过滤虚拟显示器）+ PDH `\GPU Engine(*)\Utilization Percentage`（取最忙引擎，任务管理器口径） |
| 上下传速度 | `NetworkInterface.GetIPStatistics()`（按网卡，过滤无网关的虚拟网卡） |
| 前台窗口帧率 | ETW `Microsoft-Windows-DxgKrnl` / `Win32k` present 事件（PresentMon 口径） |

## 权限说明

**前台窗口帧率需要以管理员身份运行 WinIsland**（读取 ETW 事件）；普通权限下自动降级为 DWM 桌面合成帧率，
其余指标不受影响。插件不访问网络、不落盘，只读系统计数器和注册表。

## 配置

- **小岛显示项**：设置 → 硬件监控 → 勾选 CPU / GPU / 上下传 / 帧率
- **设备**：多设备时可在同一页选择要监控的 GPU / 网卡
- **岛上优先级**：默认 95（低于媒体的 100，高于电池 50），可在「插件管理」里覆盖

## 依赖

依赖 `Microsoft.Diagnostics.Tracing.TraceEvent`（微软官方 ETW 封装，用于读帧率），已随插件包分发；
WinAppSDK / WinUI 运行时由宿主提供，不打进包里。

## 1.0.3
使用 DXGI 显卡名称与 LUID 配对性能计数器，修复双显卡占用错配；无活动引擎的显卡显示零占用，不再回退到其他显卡。

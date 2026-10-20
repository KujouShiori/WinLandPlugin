# 岛历 · island-almanac

给 [WinIsland](https://winisland.luolan.fun/) 灵动岛用的中文日历插件：小岛看今天，点开是完整月历与提醒。

## 定位：不做第二个日历，做「时间仪表」

Windows 日历（含新版 Outlook）是**日程库**：有账号、多端同步、能邀请他人、能挂会议链接、能共享委托。这些它做得比任何桌面小组件都好，岛历不去碰。

岛历做的是**仪表**：不录入、不协作、不需要账号，只负责「抬眼这一秒看到该看的东西」。

| 能力 | Windows 日历 | 岛历 | 谁该负责 |
|---|---|---|---|
| 多账户 / 多端同步 | ✅ | ❌ | Windows 日历 |
| 会议邀请、共享、委托日历 | ✅ | ❌ | Windows 日历 |
| 事件编辑（拖拽、全天、附件、时区） | ✅ | ❌ | Windows 日历 |
| 任务清单 | ✅（To Do） | ❌ | Windows 日历 |
| 抬眼看到「下一件事」 | ❌ 必须打开 Outlook；任务栏日历面板只有月视图 | ✅ 常驻小岛 + 悬停展开 | **岛历** |
| 农历、节气 | 仅任务栏弹出面板有农历小字，无节气释义 | ✅ 格内小字 + 节气一句解释 | **岛历** |
| 调休 / 补班（中国特色） | ❌ 只知道放假，不标补班 | ✅ `休` / `班` + 连续假期带 | **岛历** |
| 离线可用、不需要登录 | ❌ 依赖账号 | ✅ 内置数据 + 本地 JSON | **岛历** |
| 定时提醒的必达性 | ✅ 系统通知中心 | ✅ 岛体消息 + 缺席补提 | 平手（岛历不输） |

### 明确不做（防止插件长成半吊子日历）

会议邀请、共享/委托日历、多账户登录、时区换算、附件、拖拽改期、任务清单、团队日历、RRULE 全集。
这些领域已经有主，重复实现只会造成「两份日程、哪个才算」的混乱。

### 还没做的（候选，按价值排序）

| 候选 | 价值 | 现状 |
|---|---|---|
| 农历重复（农历生日、忌日） | Outlook 按公历重复，每年漂 10~11 天，这块只有岛历能做 | 未实现 |
| 以工作日计的倒数（扣周末 + 法定假日） | 倒 deadline 用，比自然日准 | 未实现 |
| 七十二候、月相 | 文化线，不需要新数据源 | 未实现 |

以下东西**不会加**：黄历宜忌（数据不权威）、限行尾号（政策多变、错一次就没人信）、每日一言（噪音）。

### 与 Windows 日历的连接方式：只读桥，不做双写

需要 Windows 日历里的日程出现在岛上时，走 **ICS 订阅（只读）**，而不是双向同步：

```
Outlook / Google / 腾讯日历  ──发布私有 ICS 链接──▶  岛历（缓存 + 只读显示）
        （唯一数据源，可编辑）                        （只显示，不改，不回写）
```

- 只读：岛上永远不提供编辑入口，避免两边同时改同一事件的冲突
- 通知去重：订阅来的事件默认**只在岛上显示、不弹提醒**；只有岛历自己建的「碎片提醒」才弹，避免和 Outlook 通知撞车
- 断网：保留上次缓存继续显示，抓取失败会记在该源上（设置页显示最后一次成功时间与错误原因），不会因为一次失败就把已有日程清空

## 日程订阅（只读桥 · 已实现）

设置页「日程订阅（只读）」里贴一个 ICS 链接即可（Outlook 发布日历、Google 日历私有地址、腾讯日历订阅地址都行）。

| 项 | 说明 |
|---|---|
| 只读 | 不提供编辑入口，不回写原日历，不弹通知（避免和 Outlook 通知撞车） |
| 刷新 | 启动 + 每 6 小时一次；缓存超过 6 小时才联网；某个源失败时保留它上次的数据 |
| 显示 | 展开态右下「📅 下一条日程」、聚光卡顶部「今天 N 件」、月历格内紫色标题 |
| 窗口 | 缓存最近 7 天到未来 180 天，最多 3000 条 |
| 本机地址 | `127.0.0.1` / `localhost` 自动绕过系统代理（代理会把它回成 502） |

支持的 iCalendar 子集：`VEVENT`、`SUMMARY`、`DTSTART`/`DTEND`（`VALUE=DATE` 全天、`TZID` 时区、`Z` UTC）、
`RRULE`（`DAILY` / `WEEKLY` / `MONTHLY` / `YEARLY` + `INTERVAL` / `BYDAY` / `BYMONTHDAY` / `COUNT` / `UNTIL`）、`EXDATE`、`STATUS:CANCELLED`。
不支持 `BYSETPOS` / `BYYEARDAY` / `RECURRENCE-ID` 覆盖 —— 遇到就只取首次并在日志里说明，不做半截实现。

## 小岛

- 常驻（紧凑）：只有 `📅 10月19日 周一`。农历、节气、倒数一律不放 —— 岛体窄，字多了必被截断
- 悬停展开：
  - 左：当月迷你月历（周一起，今天高亮，法定假日红、调休上班青）
  - 右上：下一个节假日 + 日期/周几/还有几天/放假天数；**当天就是节日时**换成「今天 中秋 · 农历八月十五 · 放假第 1 天」
  - 右下：日程与提醒里**更近的那一件**（`📅` = 订阅日程 / `⏰` = 自有提醒）；两样都没有才回落到当前节气
- 点击岛体：弹出完整月历

## 完整月历（聚光卡）

- 月历格子：公历日 + 农历/节气/节日小字、`休` / `班` 标记、今天高亮
  - **格子里写当天的条目**，按时间混排，最多两行，多的折成 `+2`
  - 颜色即来源：**蓝 = 岛历自己的提醒**（可编辑），**紫 = 订阅来的日程**（只读）；今天已过点的提醒转灰
  - **放假整段铺一层淡红底色**，连着的几天自然连成一整块；调休上班日只留青色 `班` 标记，不铺色
- 右侧顶部一行「📅 今天 N 件：09:30 周会 · 14:00 评审 · +1」，来自订阅日历；今天没有日程时整条隐藏
- 右侧提醒清单：按最近触发时间排序，已完成的沉底并置灰
- 提醒编辑器：标题 → 重复（单次/每天/每周/每月/每年）→ 日期·时间 → 提醒时机（一个下拉框）
  - 只有一个入口：清单右上角 `＋` 新建；主按钮随模式变「添加 / 保存修改」
  - 删除在每条清单右侧的 `✕`
  - 编辑已有提醒时，下方多出一行 **稍后 10 分 / 稍后 1 小时 / 完成（单次）或跳过本次（重复）**

## 提醒不会「静默丢失」

| 场景 | 处理 |
|---|---|
| 正常到点 | 岛体临时消息弹出（不抢焦点），实时窗口为到点后 6 分钟内 |
| 宿主被关掉 / 崩了 / 系统睡眠 | 每次心跳（60 秒）写 `state.json`；重新启动或唤醒后，按缺席时间段补扫，把漏掉的提醒合成一条「错过 N 条提醒」弹出，并立即写 `LastFired`，不会重复补 |
| 想晚点再说 | 稍后 10 分钟 / 1 小时；岛体与清单都显示「稍后 HH:mm 提醒」，到点优先触发一次后回到原周期 |
| 这件事不用做了 | 单次提醒 → 完成（不再触发，置灰保留）；重复提醒 → 跳过本次（下个周期照常） |

启动时也会立刻检查一次，不依赖第一个 15 秒定时器 —— 宿主寿命短也能把提醒送到。

## 数据源

| 数据 | 来源 | 是否联网 |
|------|------|----------|
| 法定节假日 / 调休 | [holiday-cn](https://github.com/NateScarlet/holiday-cn)（据国务院办公厅放假公告生成） | 内置 2025–2026；**全自动**更新：启动时检查 + 每 6 小时一次，缓存超过 24 小时才联网。无手动按钮 |
| 24 节气 | 离线日期表 1901–2100（寿星天文历算法生成，已与日本国立天文台 2026/2027 历要项逐项核对） | 不联网 |
| 农历 | .NET 内置 `ChineseLunisolarCalendar` | 不联网 |

官方还没公布下一年放假安排时，会按节日日期兜底显示「元旦 1月1日 · 还有 74 天 · 放假安排待公布」，不会空着。

**节假日更新只写 `holidays-cache.json`，与 `reminders.json` 完全隔离 —— 你的日程提醒不会被覆盖。**

内置数据的再生成方式（都离线，只在需要更新时跑）：

| 数据 | 脚本 | 依赖 |
|---|---|---|
| `Data/holidays.json`（2025–2026 法定假日与调休） | `python tools/gen-holidays.py` | 无（先把 `holiday-YYYY.json` 下载到 `Data/`） |
| `Core/SolarTermsData.cs`（1901–2100 节气表） | `python tools/gen-solar-terms.py` | `pip install lunar-python` |

## 存放位置

- 插件：`%LocalAppData%\Programs\WinIsland\plugins\island-almanac\`
- 提醒数据：`%LocalAppData%\WinIsland\island-almanac\reminders.json`
- 日程订阅地址：`%LocalAppData%\WinIsland\island-almanac\feeds.json`
- 日程缓存：`%LocalAppData%\WinIsland\island-almanac\schedule-cache.json`
- 运行状态（心跳，用于补提）：`%LocalAppData%\WinIsland\island-almanac\state.json`
- 节假日缓存：`%LocalAppData%\WinIsland\island-almanac\holidays-cache.json`
- 日志：`%LocalAppData%\WinIsland\logs\plugin.island-almanac.log`

## 设置项

| 键 | 说明 |
|----|------|
| `enabled` | 在岛上显示 |
| `showLunar` | 月历里显示农历小字 |
| `showTerm` | 月历/展开态里显示节气 |
| `showHoliday` | 展开态右上显示节日块 |
| `showReminder` | 展开态右下显示提醒（关掉则固定显示节气） |
| `showSchedule` | 展开态右下显示订阅来的下一条日程 |

## 从源码构建

```powershell
cd island-almanac
dotnet --version      # 必须是 10.x（global.json 已钉住）
dotnet build          # 自动拷贝到 %LocalAppData%\Programs\WinIsland\plugins\island-almanac
```

依赖：`luolan.winland.Core` 2.4.0（宿主 API 2.4.0）。插件用到聚光卡、主题、输入框，`min_host_version` 因此是 `2.4.0`。

## 打包发布

```powershell
powershell -ExecutionPolicy Bypass -File tools\pack.ps1
```

产出 `dist\island-almanac.lwp`（约 67 KB），包里只有：

```
plugin.json
IslandAlmanac.dll
IslandAlmanac.deps.json
IslandAlmanac.pri
README.md
```

宿主的 `WinIsland.Core.dll`、WinUI、WebView2、`Microsoft.WindowsAppRuntime*`、`*.pdb` 全部剔除——带上只会白胖几十 MB，市场 CI 也会拦。

- 市场图标 `logo.png`（240×240 透明 PNG）由 `tools\make-logo.py` 生成，只放仓库根目录供投稿脚本取用，不进插件包
- 发新版本：改 `plugin.json` 的 `version`（只增不减，例如 `1.0.0` → `1.0.1`）→ 重新跑 `pack.ps1`

## 更新日志

### 1.0.0

首个公开版本。

- 小岛：常驻 `📅 10月19日 周一`；悬停展开当月迷你月历 + 下一个节日 + 日程/提醒/节气
- 完整月历：农历与节气小字、`休`/`班` 标记、连续假期底色、格内条目、今天高亮
- 提醒：单次 / 每天 / 每周（多选星期）/ 每月（短月落月末）/ 每年；提前 5 分～1 天
- 提醒不会静默丢失：心跳 + 启动补提、稍后 10 分/1 小时、完成/跳过本次
- 日程订阅：ICS 只读桥（RRULE 子集、全天、时区、EXDATE），只显示不弹通知
- 数据：holiday-cn 全自动更新（内置 2025–2026 兜底）、1901–2100 节气表、.NET 农历

## 许可与致谢

- 插件代码：MIT（见 `LICENSE`）
- 法定节假日数据：[NateScarlet/holiday-cn](https://github.com/NateScarlet/holiday-cn)（MIT），据国务院办公厅放假公告生成
- 节气日期：由 lunar_python（寿星天文历算法，MIT）离线生成，未随插件分发该库
- 农历换算：.NET 内置 `System.Globalization.ChineseLunisolarCalendar`
- 图标：`logo.png` 由 `tools/make-logo.py` 生成，无第三方素材

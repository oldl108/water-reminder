# 早睡早起多喝水

一个**绝不打扰**的 Windows 托盘小工具：定时提醒喝水 + 站起来活动 + 定点吃药提醒，并记录每天的情况。

> 作者微信公众号：**爱玩的果果**

## 下载

到 [Releases](https://github.com/oldl108/water-reminder/releases) 页面下载最新的
`WaterReminder-vX.X.X-win-x64.zip`，解压后双击 `WaterReminder.exe` 即可，
免安装、无需 .NET 运行时（Windows 10/11 x64）。

> 首次运行时 Windows SmartScreen 可能提示"已保护你的电脑"——因为程序没有付费的
> 代码签名证书。点「更多信息」→「仍要运行」即可。程序完全离线，不联网、不收集任何数据。

## 设计原则

提醒气泡永远不抢焦点。它用 `WS_EX_NOACTIVATE + WS_EX_TOOLWINDOW + WS_EX_TOPMOST` 创建，
显示在屏幕右下角最上层，但**不获得键盘焦点、不出现在任务栏和 Alt+Tab**——
打字、游戏按键、任何输入都不受影响。不理它 10 秒后自动淡出。

## 功能

- **提醒**：默认每 50 分钟弹一次气泡（"该喝口水了，顺便站起来动一动"），
  点"已喝"一键记录一杯并同时记一次起身活动；点"稍后"或忽略则 20 分钟后再温和提示
- **吃药提醒**：托盘右键 → "吃药提醒…"，添加药名和每天的固定时间（如 08:00、21:00）。
  到点弹同样的无焦点气泡，点"已吃了"确认；没确认前每 10 分钟催一次。
  吃药提醒优先于喝水提醒，且**不受"暂停提醒"和提醒时段限制**（药不能漏）
- **空闲检测**：离开电脑超过 10 分钟（无键鼠输入）视为已活动，回来后久坐计时和提醒重置，不会对着空椅子弹窗
- **全屏检测**（可选）：开启后，前台全屏（游戏/视频）时不弹气泡，延后 5 分钟再试
- **提醒时段**：默认 9:00–23:00，之外不打扰
- **暂停**：托盘菜单一键"暂停提醒 1 小时"（开会/打排位用）
- **记录**：点托盘水滴图标弹出快捷面板，+100/+200/+300ml 或自定义补记
- **统计**：今日进度、站起次数、最近 7 天柱状图（深色 = 达标）
- **开机自启**：设置里勾选即可（写 HKCU Run 注册表，仅当前用户）

## 数据

全部存本地，无任何网络请求：

- `%APPDATA%\WaterReminder\config.json` — 设置
- `%APPDATA%\WaterReminder\data.json` — 每日饮水与活动记录

## 构建

需要 .NET 8 SDK：

```
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

产物是单个 `WaterReminder.exe`，双击即用，无需安装。

## 项目结构

| 文件 | 职责 |
|------|------|
| `TrayContext.cs` | 托盘常驻主逻辑：计时、空闲/全屏判断、调度提醒 |
| `ToastForm.cs` | 无焦点提醒气泡（核心：`CreateParams` 加 `WS_EX_NOACTIVATE`） |
| `PanelForm.cs` | 托盘快捷记录面板 |
| `MedsForm.cs` | 吃药提醒管理（药名 + 每日固定时间） |
| `StatsForm.cs` | 统计窗口 |
| `SettingsForm.cs` | 设置窗口 + 开机自启注册表 |
| `DataStore.cs` | JSON 配置与记录读写 |
| `NativeMethods.cs` | Win32 P/Invoke：空闲时间、全屏检测 |

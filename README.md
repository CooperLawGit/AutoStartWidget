# AutoStartWidget

开机自启的办公小组件集合管理器。

AutoStartWidget 管理多个办公小组件。截图工具和护眼助手是平级模块，独立开关，开启时才加载对应能力。

## 组件

- `AutoStartWidget.App`: 集合管理器入口，负责托盘菜单、模块开关、设置、开机自启。
- `AutoStartWidget.Core`: 护眼计时、休息周期、配置模型。
- `src/Setuna.Legacy`: SETUNA2 截图组件源码，作为原生截图工具重写参考。

## 功能

- 托盘常驻。
- 截图工具和护眼助手独立启用/禁用。
- 截图工具使用 .NET 8 WinForms 原生 C# 实现，参考 SETUNA2 交互。
- 支持区域截图、窗口截图辅助、截图贴片、复制、另存、关闭后恢复、清空废纸篓。
- 每 20 分钟显示护眼屏，默认休息 20 秒。
- 锁屏时暂停护眼计时，解锁后重新计时。
- 可配置护眼背景、Tips、倒计时和显示屏幕范围。
- 可写入当前用户 `Run` 注册表项，实现集合管理器开机自启。

## 使用说明

### 启动

运行 `AutoStartWidget.exe` 后，程序进入托盘常驻。托盘图标是集合管理器入口。

### 截图工具

在托盘菜单中开启截图工具后，可使用“区域截图”拖拽选择区域，也可使用“窗口截图”点击高亮窗口进行窗口截图。

截图完成后会生成置顶贴片。贴片右键可复制、另存或关闭。关闭后的截图贴片会进入废纸篓，可在托盘菜单恢复最近关闭的截图，或清空废纸篓。

截图工具关闭后会释放热键、截图窗口和废纸篓内容。

截图热键在“截图工具 -> 设置”中使用 SETUNA2 同款热键控件录入。把光标放到热键框，直接按下组合键即可记录。默认热键是 `Ctrl+1`。

截图贴片左上角会显示左侧白线和上侧白线，表示截图已生成。右键菜单顶部的“关闭”会把截图放入回收站；“回收站”子菜单悬停展开，列表按关闭时间排序，越新的越靠上。菜单还包含复制、剪切、保存、还原缩放、变换和选项。鼠标悬停在贴片上时，按住 `Ctrl` 滚动鼠标中键可缩放，每一格 10%。双击截图会像 SETUNA2 一样缩小成 50x50 小方块，再次双击恢复。

### 护眼助手

在托盘菜单中开启护眼助手后，护眼计时开始工作。默认每 20 分钟弹出一次护眼屏，默认休息 20 秒。

可使用“立即护眼”手动打开护眼屏。护眼助手关闭后，不再计时，也不监听锁屏/解锁事件。

### 开机自启

开机自启只负责启动 AutoStartWidget 集合管理器。截图工具和护眼助手是否启用，由各自模块开关配置决定。

### 图标

应用图标统一来自：

```text
D:\WorkSpace\GithubProject\icon.png
```

## Build

主应用需要 .NET 8 SDK：

```powershell
dotnet build .\AutoStartWidget.sln
dotnet run --project .\tests\AutoStartWidget.Tests\AutoStartWidget.Tests.csproj
```

legacy 截图参考项目需要 Visual Studio 2022/MSBuild。它只作为迁移参考，不是主应用运行依赖：

```powershell
nuget restore .\src\Setuna.Legacy\SETUNA.sln
msbuild .\src\Setuna.Legacy\SETUNA.sln /p:Configuration=Release /p:Platform=x64
```

## 设计文档

- `版本设计.md`: 产品定位和版本节奏。
- `后续计划.md`: roadmap 和 SETUNA2 非核心扩展清单。

## 来源

- 护眼助手来自本地 `ProtectMyEyes` 项目并改名整合。
- 截图工具参考 `CooperLawGit/SETUNA2`，原许可证见 `third_party/SETUNA2/LICENSE`。

# AutoStartWidget

开机自启的办公小组件基准版。

## 组件

- `AutoStartWidget.App`: 托盘入口，负责启动截图、护眼提醒、设置、开机自启。
- `AutoStartWidget.Core`: 护眼计时、休息周期、配置模型。
- `src/Setuna.Legacy`: SETUNA2 截图组件源码，保持 .NET Framework 4.7 形态。

## 功能

- 托盘常驻。
- 托盘菜单启动 SETUNA 截图组件。
- 每 20 分钟显示护眼屏，默认休息 20 秒。
- 锁屏时暂停护眼计时，解锁后重新计时。
- 可配置护眼背景、Tips、倒计时和显示屏幕范围。
- 可写入当前用户 `Run` 注册表项，实现开机自启。

## Build

主应用需要 .NET 8 SDK：

```powershell
dotnet build .\AutoStartWidget.sln
dotnet run --project .\tests\AutoStartWidget.Tests\AutoStartWidget.Tests.csproj
```

SETUNA 截图组件是 legacy .NET Framework 4.7.2 项目，需要 Visual Studio 2022/MSBuild：

```powershell
nuget restore .\src\Setuna.Legacy\SETUNA.sln
msbuild .\src\Setuna.Legacy\SETUNA.sln /p:Configuration=Release /p:Platform=x64
```

发布时，把生成的 `SETUNA.exe` 放到 `AutoStartWidget.exe` 同目录的 `Setuna` 文件夹。

## 来源

- 护眼助手来自本地 `ProtectMyEyes` 项目并改名整合。
- 截图组件来自 `CooperLawGit/SETUNA2`，原许可证见 `third_party/SETUNA2/LICENSE`。

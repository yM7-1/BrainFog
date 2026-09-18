# BlindSpire 研读总览（笔记 00）

> 状态：项目初始化 + 游戏/依赖库研读完成，**等待功能目标**。最后更新：2026-09-18。

## 1. 环境与版本

| 项 | 版本/路径 |
| --- | --- |
| 游戏 | Slay the Spire 2 **v0.111.0**（commit `41cef1ea`，2026-08-13） |
| 游戏安装 | `D:\Steam\steamapps\common\Slay the Spire 2\`（主程序集 `data_sts2_windows_x86_64\sts2.dll` + 官方 `sts2.xml` 文档） |
| BaseLib | **v3.4.7**（Alchyr；创意工坊 `3737335127`，min_game 0.107.1） |
| RitsuLib | **0.6.2**（OLC；创意工坊 `3747602295`，含 0.107.1~0.111.0 compat 变体 + `RitsuLib.References.props`） |
| 示例 mod | QuickRestart v2.0.0（erasels，依赖 BaseLib ≥3.4.5）；ModConfig（`3749062616`，中文配置库） |
| 反编译工具 | ilspycmd 8.2（`/root/.dotnet/tools/ilspycmd`，需 `DOTNET_ROOT=/root/.dotnet` + `DOTNET_ROLL_FORWARD=LatestMajor`） |

## 2. 反编译产物（`.refs/`，gitignored）

| 目录 | 内容 | 文件数 |
| --- | --- | --- |
| `.refs/sts2-v0.111.0/src` | 游戏本体 | 3538 |
| `.refs/baselib-v3.4.7/src` | BaseLib | 511 |
| `.refs/ritsulib-0.6.2/Runtime-0.111.0/src` | RitsuLib 主实现 | 2222 |
| `.refs/ritsulib-0.6.2/STS2-RitsuLib-0.111.0/src` | RitsuLib 门面（TypeForwardedTo） | 3 |
| `.refs/ritsulib-0.6.2/{Shared,Ui,Settings}/src` | RitsuLib 共享模块 | 117/133/190 |
| `.refs/quickrestart-example/src` | QuickRestart（BaseLib 用法示例） | — |

复现命令（以 sts2 为例）：

```bash
export DOTNET_ROOT=/root/.dotnet PATH=/root/.dotnet:/root/.dotnet/tools:$PATH DOTNET_ROLL_FORWARD=LatestMajor
ilspycmd -p -o .refs/sts2-v0.111.0/src --nested-directories \
  "/mnt/d/Steam/steamapps/common/Slay the Spire 2/data_sts2_windows_x86_64/sts2.dll"
```

## 3. 研读笔记索引

- [`01-sts2-source-map.md`](01-sts2-source-map.md)：游戏源码地图（战斗引擎/内容注册/RNG/Mod 机制/扩展点）
- [`02-baselib-api.md`](02-baselib-api.md)：BaseLib API（构造即注册、ModConfig、本地化、UI 挂点、pck）
- [`03-ritsulib-api.md`](03-ritsulib-api.md)：RitsuLib API（生命周期事件、ModPatcher、持久化、设置 UI、多版本引用）

## 4. 两库定位速记（选型时用）

- **BaseLib**：偏「内容注册 + 配置 + 本地化 + UI 挂点 + 工具」；内容基类构造即注册（卡/遗物/能力/角色），`[Pool(...)]` 必填；配置自动生成设置 UI；无 props，需自己 HintPath 引用。
- **RitsuLib**：偏「框架运行时」；生命周期事件订阅、ModPatcher 补丁管线、三层持久化、设置 UI 反射绑定、多游戏版本 compat 变体；自带 `RitsuLib.References.props`（自动选 0.111.0 变体），编译期只需引用门面 dll。
- 两者可共存（RitsuLib 设置模块会镜像 BaseLib/ModConfig 的配置页）；选型/分工待功能目标确定后定。

## 5. 待办

1. **等待用户提供 BlindSpire 的功能目标**（决定选型：BaseLib 基类注册 vs RitsuLib 声明式注册；是否两者都用）。
2. 目标确定后：搭 csproj（游戏 + 两库引用）、`BlindSpire.json` manifest、`[ModInitializer]` 入口、pck 打包链路。
3. 决定是否把本仓库注册为 `D:\0_git` 元仓库子模块（需要 GitHub remote 后）。

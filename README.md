# BrainFog / 脑雾尖塔

杀戮尖塔 2（Slay the Spire 2）认知障碍难度 mod：**一场发生在脑内的灾难**——
卡牌要打过一次才能认出来；所有文字（界面、事件、对话、菜单，含主标题）碎成不同比例的乱码，且每次启动游戏重新随机；
生命与金币停留在"上次休息时"的记忆里；敌人、遗物与关底 Boss 只剩轮廓和问号。
你仍然能操作，只是认不出任何东西。

- 状态：**Phase 1–3 功能已实现**（Release 构建 + 全部单测 + 补丁目标审计全绿，数量以 `tools/check.sh` 输出为准），游戏内行为待人工验收（见 `docs/IMPLEMENTATION-MAP.md` §3）
- 目标游戏版本：**v0.111.0**（当前仅支持该版本）
- 依赖：**RitsuLib 0.6.2**（Steam 创意工坊）；仅支持**单人**
- 规格（冻结）：`docs/spec/SPEC-consolidated.md`（0.01~0.03 合并）

## 开发

```bash
# 构建 + 单测 + 补丁目标审计（一键验证）
bash tools/check.sh

# 可用环境变量覆盖默认路径
# DOTNET_ROOT、STEAM_ROOT、STS2_DATA_DIR
```

- 产物：`.godot/mono/temp/bin/Release/BrainFog.dll`（含 Core 逻辑，单程序集）
- 调试：环境变量 `BRAINFOG_DEBUG=1` 输出状态日志并显示 F9 状态覆盖层
- 安装：`dotnet build BrainFog.csproj -c Release -p:CopyModOnBuild=true` 会复制到 `D:\Steam\steamapps\common\Slay the Spire 2\mods\BrainFog\`（默认 **不写**游戏目录）
- 反编译参考源码在 `.refs/`（gitignored，可用 ilspycmd 重建，见 `docs/research/00-overview.md`）

## 目录

| 路径 | 内容 |
| --- | --- |
| `src/BrainFog.Core/` | 纯逻辑（状态机、规则、快照、模糊算法），无 Godot 依赖，可单测 |
| `src/Runtime/` | 游戏适配层（Harmony 补丁 + Godot UI 逻辑） |
| `tests/` | xUnit：Core 逻辑测试 + 对 `sts2.dll` 的补丁目标审计（防游戏升级漂移） |
| `tools/check.sh` | 构建 + 验证脚本 |
| `docs/spec/` | 需求原文与冻结规格 |
| `docs/IMPLEMENTATION-MAP.md` | 规格→代码→测试映射、已知限制、晨间验收清单 |
| `docs/research/` | 游戏与依赖库研读笔记 |

## 依赖与致谢

- [RitsuLib](https://steamcommunity.com/sharedfiles/filedetails/?id=3747602295)（作者 OLC）：生命周期、补丁管线、局内数据持久化、设置与本地化框架
- 游戏内文本/内容版权归 MegaCrit 所有；本 mod 仅修改本地显示行为

## 免责声明

非官方 mod，与 MegaCrit 无关；使用风险自负，联机模式不支持（自动禁用）。

## 已知限制（摘要）

近似与未覆盖项（黑雾层级、事件获得路径、RitsuLib 读档时序等）见 `docs/IMPLEMENTATION-MAP.md` §2。

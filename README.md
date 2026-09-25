# BrainFog / 脑雾尖塔

杀戮尖塔 2（Slay the Spire 2）认知障碍难度 mod：**一场发生在脑内的灾难**——
卡牌要打过一次才能认出来（「好记性」）或上手 2 次不打出就失忆（「坏记性」，n 可调）；失忆的牌在战斗结束时从卡组中消失（「记忆消逝」，可关闭）；开局牌组视为记得，刚获得的牌也先认得，而即将失忆的手牌会变暗提醒。所有文字（界面、事件、对话、菜单，含主标题）按统一比例碎成乱码（面板可调 0%–100%，乱码可固定或每次重进随机）；
生命与金币默认实时且数值可读（可切换「失忆模式」停留在"上次休息时"的记忆里，或关闭「血量数/金币数恢复正常显示」让数值重新乱码）；遗物与敌人模型默认可见（可关闭「显示遗物」「敌人模型可见」恢复轮廓/呼吸方框）。
**认知修改器在主标题界面与对局内均可操作**（文字 / 认知 / 感知三模块；可把当前设置保存为默认，或一键重置为默认；亦可一键「关闭脑雾尖塔」让本 mod 完全停止影响游戏，随时恢复）；默认配置偏辅助：卡牌奖励全部揭示、商店/事件卡面揭示、数值可读、遗物可见、地图全路线、敌人模型与意图可见。
你仍然能操作，只是认不出任何东西。

- 状态：**Phase 1–3 功能已实现并实机验收；当前版本 0.3.9**（六项认知规则 / 遗物全场景显示 / 记忆消逝删卡动画 / 默认值调整 + 「当前设置为默认」/ 地图画图原版化 / 地图选项恢复阶段 Boss 图标与图例 / 正式服 v0.107.1 适配 + Boss/图例悬停描述纳入文字乱码 / 移除卡牌计数器 / 认知修改器「关闭脑雾尖塔」开关 / 迭代完善批次：Neow 实时血量修复、文本恢复修复、审计与本地化测试防线 / 卡牌文字乱码恢复修复；见 `CHANGELOG.md`）。Release 构建 + 全部单测 + 双版本补丁目标审计全绿（数量以 `tools/check.sh` 输出为准）；发布与协作约定见 `AGENTS.md`
- 目标游戏版本：**v0.107.1 – v0.111.0**（正式服与测试服共用同一 DLL）
- 依赖：**RitsuLib 0.6.2**（Steam 创意工坊）；仅支持**单人**
- 规格（冻结）：`docs/spec/SPEC-consolidated.md`（0.01~0.03 合并）

## 开发

```bash
# 首次或更换引用版本时：拉取 NuGet 引用程序集（.refs/sts2-refs/）
bash tools/fetch-refs.sh

# 构建 + 单测 + 双版本审计（min 0.107.1 编译 + 0.107.1/0.111.0 补丁目标审计）
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
| `tools/check.sh` | 构建 + 双版本验证脚本（installed 游戏 + min 0.107.1 编译 + 0.107.1/0.111.0 审计） |
| `tools/fetch-refs.sh` | 拉取 NuGet 引用程序集（`.refs/sts2-refs/`，供 min 版本编译与审计） |
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

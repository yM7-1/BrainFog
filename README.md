# BlindSpire

杀戮尖塔 2（Slay the Spire 2）盲玩难度 mod：卡牌牌面初始黑雾，打出一张才揭示一张。

- 状态：**Phase 1–3 功能已实现**（构建+45 单测+补丁目标审计全绿），游戏内行为待人工验收（见 `docs/IMPLEMENTATION-MAP.md` §3）
- 目标游戏版本：**v0.111.0**（当前仅支持该版本）
- 依赖：**RitsuLib 0.6.2**（Steam 创意工坊）；仅支持**单人**
- 规格（冻结）：`docs/spec/SPEC-consolidated.md`（0.01~0.03 合并）

## 开发

```bash
# 构建 + 单测 + 补丁目标审计（一键验证）
bash tools/check.sh
```

- 产物：`bin/Release/net9.0/BlindSpire.dll`；构建时默认复制到 `D:\Steam\steamapps\common\Slay the Spire 2\mods\BlindSpire\`（`-p:CopyModOnBuild=false` 可关闭）
- 反编译参考源码在 `.refs/`（gitignored，可用 ilspycmd 重建，见 `docs/research/00-overview.md`）

## 目录

| 路径 | 内容 |
| --- | --- |
| `src/BlindSpire.Core/` | 纯逻辑（状态机、规则、快照、模糊算法），无 Godot 依赖，可单测 |
| `src/Runtime/` | 游戏适配层（Harmony 补丁 + Godot UI 逻辑） |
| `tests/` | xUnit：Core 逻辑测试 + 对 `sts2.dll` 的补丁目标审计（防游戏升级漂移） |
| `tools/check.sh` | 构建 + 验证脚本 |
| `docs/spec/` | 需求原文与冻结规格 |
| `docs/IMPLEMENTATION-MAP.md` | 规格→代码→测试映射、已知限制、晨间验收清单 |
| `docs/research/` | 游戏与依赖库研读笔记 |

## 已知限制（摘要）

战争迷雾近似与未覆盖项（黑雾层级、玩家视野半径、事件获得路径、Spine 呼吸相位、RitsuLib 读档时序）见 `docs/IMPLEMENTATION-MAP.md` §2。

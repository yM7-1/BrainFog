# 游戏版本升级指南

BrainFog 支持一个**版本窗口**：编译目标固定为**最低支持版本**（当前正式服 v0.107.1），
并在窗口内的最新测试版本（当前 beta v0.111.0）上做补丁目标审计；同一个 DLL 在窗口内所有版本运行。

## 版本窗口

| 角色 | 版本 | 来源 |
| --- | --- | --- |
| 最低支持（编译目标 / `min_game_version`） | 0.107.1 | `.refs/sts2-refs/0.107.1`（NuGet `Book.StS2.RefLib`） |
| 审计上限（当前 beta） | 0.111.0 | `.refs/sts2-refs/0.111.0` |

- 引用程序集不入库（`.refs/` 已 gitignore）：首次使用运行 `bash tools/fetch-refs.sh`。
- `tools/check.sh` 先用本机安装的游戏（真实程序集）构建 + 审计，再对 0.107.1 编译，并对
  0.107.1 / 0.111.0 两个引用程序集跑补丁目标审计（`PatchTargetAuditTests`）。

## 游戏升级后怎么适配

1. **测试服出新版本（窗口上限前移）**
   - 在 `tools/fetch-refs.sh` 增加新版本引用包并拉取，把它加进 `tools/check.sh` 的审计列表；
   - 跑 `bash tools/check.sh`，按 `PatchTargetAuditTests` 的失败列表修漂移；
   - 编译失败 → 游戏 API 变更，按新反编译源码调整（重建反编译见 `docs/research/00-overview.md`）。
2. **正式服追上新版本（最低支持版本前移）**
   - 把 `BrainFog.csproj` 的 `Sts2MinRefsDir`、`RitsuLibReferenceTarget` 与 `BrainFog.json`
     （含 `packaging/workshop/content/BrainFog/BrainFog.json`）的 `min_game_version` 升到新版本；
   - 重新拉取引用包并跑 `tools/check.sh`；
   - 只有在确认不再需要兼容旧正式服时才前移，前移即放弃旧版本玩家。
3. **RitsuLib 升级**：`RitsuLibReferenceTarget` 需有对应 compat 变体；升级前先确认其 `compat/` 目录。

## 实机验收

跑 `docs/IMPLEMENTATION-MAP.md` §3 清单，重点：卡牌黑雾、读档重绑、地图迷雾、事件模糊、
Boss/图例悬停描述随乱码百分比（0.3.5）。

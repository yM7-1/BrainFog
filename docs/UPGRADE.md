# 游戏版本升级指南

BrainFog 支持一个**版本窗口**：编译目标固定为**最低支持版本**（当前正式服 v0.107.1），
并在窗口内的最新测试版本（当前 beta v0.113.0）上做补丁目标审计；同一个 DLL 在窗口内所有版本运行。

## 版本窗口

| 角色 | 版本 | 来源 |
| --- | --- | --- |
| 最低支持（编译目标 / `min_game_version`） | 0.107.1 | `.refs/sts2-refs/0.107.1`（NuGet `Book.StS2.RefLib`） |
| 审计上限（当前 beta） | 0.113.0 | `.refs/sts2-refs/0.113.0` |

- 引用程序集不入库（`.refs/` 已 gitignore）：首次使用运行 `bash tools/fetch-refs.sh`。
- `tools/check.sh` 先用本机安装的游戏（真实程序集）构建 + 审计，再对 0.107.1 编译，并对
  0.107.1 / 0.113.0 两个引用程序集跑补丁目标审计（`PatchTargetAuditTests`）。

## 新模式（0.113.0+）

- **Blitz**：无幕地图、无金币。地图迷雾与地图选项自动跳过；修改器显示模式提示并禁用地图选项。
- **Dread**：全飞升词缀 + 支配（Dominion）与诅咒；诅咒走「新卡先认得」后仍受记忆规则约束。
- 模式通过 `GameMode.ToString()` 的运行时名称识别（`RunModeRules`），不引用 0.113 才有的枚举值，
  因此最低版本 0.107.1 仍可编译；下一个版本若改名/新增模式，只需扩展 `RunModeRules.Parse`。

## 多人（联机）适配

- 采用方案 A「只盲自己」：每个客户端只记录/揭示**本地玩家**的卡牌与知识；队友卡牌、宠物保持原版呈现。
- 揭示数据存 RitsuLib **按玩家（NetId）槽** `reveal_player`；单人会从旧 `reveal` 槽自动迁移。
- 「记忆消逝」删除共享卡组状态，联机下停用；修改器选项置灰并提示。
- `affects_gameplay: false`：好友联机无需安装同版本 BrainFog；双方都装时各盲各的，不交换记忆状态。
- 断线重连仍未实现（游戏侧 `NetError.RunInProgress`）；方案 A 只依赖本地状态，不因此阻塞。

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
4. **新模式上线**：先补 `RunModeRules` 的名称识别与模式差异（地图/金币/共享状态），再补修改器提示；
   若新模式会改写共享卡组/对局状态，必须同步评估联机方案 A 的“零共享写入”前提。

## 实机验收

跑 `docs/IMPLEMENTATION-MAP.md` §3 清单，重点：卡牌黑雾、读档重绑、地图迷雾、事件模糊、
Boss/图例悬停描述随乱码百分比（0.3.5）、Blitz 无地图/无金币适配、Dread 诅咒、联机只盲自己。

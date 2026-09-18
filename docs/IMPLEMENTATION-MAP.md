# 实现映射与验收清单

> 规格：`docs/spec/SPEC-consolidated.md`（v0.3，已冻结）
> 游戏：STS2 v0.111.0；依赖：RitsuLib 0.6.2；仅单人。
> 验证状态：`bash tools/check.sh` = Release 构建 + 全部单测 + 补丁目标审计全绿（测试数量以脚本输出为准）。**游戏内行为待人工验收（见下）。**

## 1. 规格 → 代码 → 测试 映射

| 规格条目 | 实现 | 测试/审计 |
| --- | --- | --- |
| 1.1 初始黑雾（整张牌面） | `Game/CardFogRenderer.cs` + `Patches/NCardFogPatch.cs`（NCard.Reload postfix） | 补丁审计 NCard 字段 |
| 1.1 打出一次揭示 | `Patches/CardModelPlayRevealPatch.cs`（OnPlayWrapper postfix） | Core 状态机测试 |
| 1.1 升级一律揭示 | `Patches/CardModelUpgradeRevealPatch.cs`（set_CurrentUpgradeLevel postfix） | 补丁审计 |
| 1.1 按实例记忆/单局 | `Core/Reveal/CardRevealTracker.cs`、`Game/CardInstanceRegistry.cs` | `CardRevealTrackerTests` |
| 1.1 随存档持久化 | `Game/RevealPersistence.cs`（RitsuLib RunSavedDataStore + 牌序重绑） | 编译期集成；待游戏内存读档验收 |
| 1.1 悬停仅边框+黑雾 | 通用黑雾（`RevealRules.ShowsFrameOnHover/ShowsFaceOnHover`） | `RevealRulesTests` |
| 1.1 获得场景稀有度+加号 | `CardFogRenderer`（Reward/Shop 上下文 + `UpdatePlusMarker`） | `RevealRulesTests`（RarityOnly） |
| 1.1 牌堆已揭示显示真牌面 | 通用黑雾规则（NCardPileScreen 使用 NCard） | `RevealRulesTests` |
| 1.1 图鉴豁免 | `CardFogRenderer.ResolveContext`（NCardLibrary → FullFace） | 同上 |
| 1.2 快照显示（HP/金币） | `Core/Status/StatusSnapshot.cs`、`Game/SnapshotDisplay.cs` | `StatusSnapshotTests` |
| 1.2 休息刷新 HP+金币 | `Patches/HealRestSiteSnapshotPatch.cs` | 补丁审计 HealRestSiteOption |
| 1.2 扣钱刷新金币 | `Patches/TopBarGoldSnapshotPatch.cs`（ShouldRefreshGold） | `StatusSnapshotTests` |
| 1.2 濒危提示（真实HP<15%） | `Core/Status/LowHpHint.cs`、`Game/LowHpHintDisplay.cs` | `LowHpHintTests` |
| 1.3 敌人呼吸方框 | `Game/EnemyVisualMask.cs` + `Game/BreathingBox.cs`（Spine 轨道相位）+ `Patches/NCreatureEnemyMaskPatch.cs` | 补丁审计 NCreature |
| 1.3 玩家视野半径（周围一圈） | `Game/VisionMask.cs`（CanvasLayer+圆形软边遮罩，跟随玩家屏幕位置）+ `Patches/VisionMaskPatch.cs` | 补丁审计 NCombatRoom |
| 1.1 牌堆上下文 | `CardFogRenderer`（NCardPileScreen → PileView） | `RevealRulesTests` |
| 1.3 受击效果/伤害数字保留 | 仅隐藏 `Body`；VFX 在 `NCombatRoom.CombatVfxContainer`，不受影响 | 见 §3 人工核查项 |
| 1.3 意图仅首回合 | `Game/IntentGate.cs` + `Patches/NIntentFirstRoundPatch.cs` | 补丁审计 NIntent |
| 1.3 药水仅轮廓 | `Patches/PotionOutlinePatch.cs`（NPotion.Reload 隐藏 Image） | 补丁审计 NPotion |
| 1.4 遗物不可见 | `Patches/RelicHidePatch.cs`（NRelic + RelicReward + 检视黑雾） | 补丁审计 |
| 1.4 Boss 遗物三选一可见 | `EventTextBlurPatch` 对 `AncientEventModel` 豁免 | 补丁审计 |
| 1.4 地图迷雾+画线禁用 | `Game/MapFogController.cs` + `Patches/MapFogPatch.cs` | 补丁审计 NMapScreen |
| 1.4 事件 75% 固定模糊 | `Core/Text/EventTextBlurrer.cs` + `Patches/EventTextBlurPatch.cs` | `EventTextBlurrerTests` |
| 1.5 仅单人守卫 | `Core/MultiplayerGuard.cs`、`ModRuntime`、`RunManagerMultiplayerGuardPatch` | `MultiplayerGuardTests` + 审计 |
| 全局部件：异常边界 | `Game/PatchGuard.cs`（每 key 一次性 Error 日志），关键类入口与前缀补丁均包裹 | 编译期 |
| 全局部件：新增文本字体 | `ApplyLocaleFontSubstitution`（濒危标签/加号标记，CJK 字体替换） | 编译期 |
| 全局部件：调试观测 | `BLINDSPIRE_DEBUG=1` → `ModRuntime.DumpState`（加载/开局输出揭示数、快照值、禁用状态） | 编译期 |
| 全局部件：发布形态 | manifest 字段/依赖 + 单程序集内含 Core 类型 | `ReleaseShapeTests` |
| 泄漏修复：悬停/高亮 | 未知卡不显示真实 HoverTips；手牌可打出高亮隐藏；遗物 Focus/奖励提示移除；特殊卡奖励文案与提示替换 | 编译期 + 审计 |
| 泄漏修复：排序/卡框 | 牌组查看排序在未知卡存在时回退获得序；获得场景隐藏类型卡框（仅稀有度） | 编译期 + 审计 |
| 适配层：Core 纯函数 | `MapFogRules`/`CardContextClassifier`/`MaskEligibility`/`DeckOrderBinding`/`IntentRevealGate`（+测试） | 单测 |
| 本地化 | `ModLocalization`（RitsuLib I18N，嵌入 zhs/eng：low_hp_warning/unknown_card） | 编译期 |
| 调试 | `BLINDSPIRE_DEBUG=1` → 状态 dump + F9 覆盖层 | 编译期 |

## 2. 已知限制 / 近似（晨间重点核查）

1. **黑雾层级**：`CardFogRenderer` 把黑雾矩形插到 `_frame` 的绘制位置（帧应压在其上）。若实机出现"整张牌全黑"或"帧被盖住"，调整 `MoveChild` 目标索引。
2. **悬停**：未做专门的悬停视觉（规格允许：未知牌悬停=边框+雾）。若原版悬停有额外放大/高亮，可能仍显示已知信息，需目视确认。
3. **事件获得卡牌**：事件获得走 `CardSelectCmd.FromChooseACardScreen` / `FromSimpleGridForRewards`（`NChooseACardSelectionScreen` / `NSimpleCardSelectScreen`），与"选择自己牌组的牌"共用同一屏幕类，**无法仅按控件类型区分获得语义**；当前事件获得显示为黑雾（非稀有度边框）。后续方案：在对应 `CardSelectCmd` 命令外挂作用域标记（scope flag）。
4. **呼吸方框**：已绑定 Spine 轨道相位（`GetTrackTime/GetAnimationDuration`）；无 Spine 动画的敌人回退相位 0（静态微光）。
5. **受击特效**：设计上不受影响（VFX 容器独立），需实机确认敌人受击特效/伤害数字可见。
7. **事件选项文本**：只在按钮 `_Ready` 时模糊；若后续流程重写文本（投票刷新等），可能回退为原文。
8. **RitsuLib 持久化时序**：揭示集合随存档读写（RunSavedDataStore），读档恢复依赖 RunStarted 时序；需实机"揭几张牌→存档→读档"验证。
9. **图例**：地图图例保持可见（按 0.02 第 9 条"图例可见"），画线工具隐藏且右键绘制被拦截。

## 3. 晨间验收清单（约 10 分钟）

> 启动参数：在环境变量设置 `BLINDSPIRE_DEBUG=1` 可让日志输出状态摘要（`[BlindSpire][State:...]`）。


1. **启动**：游戏加载 mod 无报错（日志 `mods/BlindSpire/`；检查 `BlindSpire.json` 依赖 RitsuLib 0.6.2 已装）。
2. **新局卡牌**：初始卡组全部黑雾（仅边框）；战斗中打出一张 → 该张在后续战斗中显示真牌面；同名其他副本仍黑雾。
3. **升级**：锻造一张从未打出的牌 → 变真牌面。
4. **读档**：揭示 2~3 张 → 存退读档 → 揭示状态保留（其余仍黑雾）。
5. **悬停**：手牌未知牌悬停/选中 → 边框+黑雾（无识别信息）。
6. **牌堆**：打开抽/弃/消耗堆 → 已揭示显示真牌面，未揭示黑雾。
7. **奖励/商店**：卡牌奖励与商店卡牌 → 仅稀有度边框；升级版显示"+"。
8. **顶栏**：受伤/加钱后 HP/金币数字不变（快照）；休息后刷新；商店/事件扣钱后金币刷新。
9. **濒危**：把 HP 打到 <15% → 角色红边 + 状态栏"濒危：我感觉自己快死了"（战斗内外）。
10. **战斗**：敌人=呼吸方框（呼吸节奏应跟随敌人原动画）；第 1 回合有意图、之后无；受击特效/伤害数字仍在；己方召唤物正常可见；屏幕只留玩家周围圆形可见区域（视野遮罩）。
11. **战斗外**：遗物不可见（检视界面黑雾）；Boss 遗物三选一正常可读；药水仅轮廓；地图仅当前/已走/下一层可见，画线按钮消失且右键无法画；事件文本 75% 乱码且每次进入一致；图鉴正常。
12. **联机**：进入联机对局 → 日志提示 BlindSpire 已禁用（本 mod 不做联机适配）。
13. **异常自检**：若某功能未生效，日志搜索 `[BlindSpire][` 前缀：`PatchGuard` 会记录首个失败点（补丁目标漂移的最小线索）。

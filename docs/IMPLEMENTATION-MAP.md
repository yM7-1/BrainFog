# 实现映射与验收清单

> 规格：`docs/spec/SPEC-consolidated.md`（v0.3，已冻结）
> 游戏：STS2 v0.111.0；依赖：RitsuLib 0.6.2；仅单人。
> 验证状态：`bash tools/check.sh` = Release 构建 + 全部单测 + 补丁目标审计 + **shader 语法校验**（headless Godot；无二进制时跳过，`STRICT=1` 强制）全绿。**游戏内行为待人工验收（见下）。**

## 1. 规格 → 代码 → 测试 映射

| 规格条目 | 实现 | 测试/审计 |
| --- | --- | --- |
| 1.1 初始黑雾（整张牌面） | `Game/CardFogRenderer.cs` + `Patches/NCardFogPatch.cs`（NCard.Reload postfix） | 补丁审计 NCard 字段 |
| 1.1 打出一次揭示（按定义，实机调整 2026-09-19） | `Patches/CardModelPlayRevealPatch.cs`（OnPlayWrapper postfix；`RevealKeys.Of` 定义键） | Core 状态机测试 |
| 1.1 升级一律揭示 | `Patches/CardModelUpgradeRevealPatch.cs`（set_CurrentUpgradeLevel postfix） | 补丁审计 |
| 1.1 按卡牌定义记忆/单局（实机调整 2026-09-19：原按实例） | `Core/Reveal/CardRevealTracker.cs`（定义键 `cards.*`）、`Game/RevealKeys.cs` | `CardRevealTrackerTests` |
| 1.1 随存档持久化 | `Game/RevealPersistence.cs`（RitsuLib RunSavedDataStore；定义键集合，无需牌序重绑） | 编译期集成；待游戏内存读档验收 |
| 1.1 悬停仅边框+黑雾 | 通用黑雾（`RevealRules.ShowsFrameOnHover/ShowsFaceOnHover`） | `RevealRulesTests` |
| 1.1 获得场景稀有度+加号 | `CardFogRenderer`（Reward/Shop 上下文 + `UpdatePlusMarker`） | `RevealRulesTests`（RarityOnly） |
| 1.1 牌堆已揭示显示真牌面 | 通用黑雾规则（NCardPileScreen 使用 NCard） | `RevealRulesTests` |
| 1.1 图鉴豁免 | `CardFogRenderer.ResolveContext`（NCardLibrary → FullFace） | 同上 |
| 1.2 快照显示（HP/金币） | `Core/Status/StatusSnapshot.cs`、`Game/SnapshotDisplay.cs` | `StatusSnapshotTests` |
| 1.2 休息刷新 HP+金币 | `Patches/HealRestSiteSnapshotPatch.cs` | 补丁审计 HealRestSiteOption |
| 1.2 扣钱刷新金币 | `Patches/TopBarGoldSnapshotPatch.cs`（ShouldRefreshGold） | `StatusSnapshotTests` |
| 1.2 濒危提示（真实HP<15%） | `Core/Status/LowHpHint.cs`、`Game/LowHpHintDisplay.cs` | `LowHpHintTests` |
| 1.3 敌人呼吸方框 | `Game/EnemyVisualMask.cs` + `Game/BreathingBox.cs`（Spine 轨道相位）+ `Patches/NCreatureEnemyMaskPatch.cs` | 补丁审计 NCreature |
| ~~1.3 玩家视野半径（周围一圈）~~ | **已移除**（2026-09-19 实机反馈：迷雾过大，仅遮盖敌人）；`VisionMask.cs`/`VisionMaskPatch.cs` 删除，只保留敌人呼吸方框 | — |
| 1.1 牌堆上下文 | `CardFogRenderer`（NCardPileScreen → PileView） | `RevealRulesTests` |
| 1.3 受击效果/伤害数字保留 | 仅隐藏 `Body`；VFX 在 `NCombatRoom.CombatVfxContainer`，不受影响 | 见 §3 人工核查项 |
| 1.3 意图仅首回合 | `Game/IntentGate.cs` + `Patches/NIntentFirstRoundPatch.cs` | 补丁审计 NIntent |
| 1.3 药水仅轮廓 | `Patches/PotionOutlinePatch.cs`（NPotion.Reload 隐藏 Image） | 补丁审计 NPotion |
| 1.3 药水名称/描述 50% 乱码（实机调整 2026-09-19；每次启动随机 2026-09-20） | `Patches/PotionTextBlurPatch.cs`（PotionModel.HoverTip 拦截）+ `Core/Text/PotionTextBlurrer.cs` | `PotionTextBlurrerTests` + 补丁审计 PotionModel |
| 1.3 敌人名字隐藏（实机调整 2026-09-19） | `Patches/EnemyNameHidePatch.cs`（NCreatureStateDisplay.SetCreature → `_nameplateLabel` 隐藏） | 补丁审计 NCreatureStateDisplay |
| 1.2 战斗内角色真实血量隐藏（实机调整 2026-09-19） | `Patches/PlayerHpBarHidePatch.cs`（NHealthBar：HP 数字/填充/中毒/毁灭覆盖隐藏，格挡保留） | 补丁审计 NHealthBar |
| 1.1 事件/通用选牌按获得场景（实机调整 2026-09-19） | `Game/CardFogRenderer.cs`（`Pile==null` → 获得场景）+ `Core/Reveal/CardContextClassifier.cs` | `CardContextClassifierTests` |
| 1.1 升级预览不揭示（实机调整 2026-09-19） | `Patches/CardModelUpgradeRevealPatch.cs`（`Pile==null` 的克隆跳过） | 编译期 |
| 1.4 宝箱/商店遗物提示遮蔽 + 奖励行"未知遗物"（实机调整 2026-09-19） | `Patches/RelicHoverTipPatch.cs`（TreasureRoom/Merchant）+ `Patches/RelicHidePatch.cs`（NRewardButton 标签替换） | 补丁审计 + I18N |
| 1.4 标题/暂停菜单 75% 乱码（实机调整 2026-09-19；含主标题界面，每次启动随机 2026-09-20） | `Patches/MenuTextBlurPatch.cs`（设置按钮保留可读） | 补丁审计 NMainMenu/NPauseMenu |
| 1.1 卡牌查看/升级界面关键词描述 50% 乱码（实机调整 2026-09-19；每次启动随机 2026-09-20） | `Patches/HoverTipDescriptionBlurPatch.cs`（NHoverTipSet.Init + 上下文判定：Inspect/UpgradeSelect/UpgradePreview） + `Core/Text/PotionTextBlurrer.cs` | 补丁审计 NHoverTipSet |
| 1.2 顶栏快照灰显 + 标注与提示（实机调整 2026-09-19） | `Patches/HpSnapshotVisualPatch.cs`（HP 数字灰色；"上次休息时的状态"标签 + 刷新提示；I18N zhs/eng） | 补丁审计 NTopBarHp |
| 1.4 顶栏不显示阶段 Boss 图标（实机调整 2026-09-19） | `Patches/BossIconHidePatch.cs`（Icon 隐藏 + 悬停提示移除） | 补丁审计 NTopBarBossIcon |
| 1.1 揭示卡面文字 85% 乱码（实机调整 2026-09-19） | **`Patches/CardFaceTextBlurPatch.cs`（源头乱码：MegaLabel/MegaRichTextLabel.SetTextAutoSize 前缀，图鉴豁免，性能 2026-09-20）** + `Core/Text/TextBlurPercents.cs` | `TextBlurPercentsTests` + 补丁审计 MegaLabel/MegaRichTextLabel |
| 1.3 战斗开始横幅乱码（实机调整 2026-09-19） | `Patches/CombatStartBannerBlurPatch.cs`（默认 60%） | 补丁审计 NCombatStartBanner |
| 1.4 顶栏/地图 UI 描述 70% + 未规定文本默认 60%（统一悬停提示策略，实机调整 2026-09-19） | `Patches/HoverTipTextBlurPatch.cs` + `Patches/MapLegendTextBlurPatch.cs`（设置/图鉴豁免、药水跳过） | 补丁审计 NHoverTipSet/NMapLegendItem |
| 1.4 先古之民（含建筑师）对话与选项 90%（实机调整 2026-09-19） | `Patches/AncientDialogueBlurPatch.cs` + `EventTextBlurPatch.cs`（选项 90% 保留遗物图标） | 补丁审计 NAncientDialogueLine |
| 1.4 商人对话 90% 乱码（实机调整 2026-09-19） | `Patches/MerchantDialogueBlurPatch.cs` | 补丁审计 NMerchantDialogue |
| 1.4 顶栏阶段 Boss 图标彻底移除（实机调整 2026-09-19 第三轮） | `Patches/BossIconHidePatch.cs`（跳过 RefreshBossIcon/OnRoomEntered + 隐藏/失焦/悬停移除） | 补丁审计 NTopBarBossIcon |
| 1.4 阶段切换横幅乱码（实机调整 2026-09-19 第三轮） | `Patches/ActBannerBlurPatch.cs`（"阶段X/地区名" 60%） | 补丁审计 NActBanner |
| 全局：其余 UI 文本 60%（实机调整 2026-09-19；源头化 2026-09-21） | `Patches/GlobalTextBlurPatch.cs`（Mega 控件源头乱码）+ `Game/GlobalTextBlurSource.cs` + Core `Text/GlobalTextBlurRules.cs`；`Game/GlobalTextBlurDriver.cs` 仅兜底非 Mega 控件；设置/图鉴/卡面/悬停提示各自豁免 | `GlobalTextBlurRulesTests` |
| 受击/治疗/格挡数字乱码（用户规则 2026-09-21，撤销"数字可读"） | `GlobalTextBlurRules`（`NDamageNumVfx`/`NHealNumVfx`/`NDamageBlockedVfx` → 默认 60%）；其余 VFX 文本仍保持可读 | `GlobalTextBlurRulesTests` |
| 地图 Boss 点显示"?"（实机调整 2026-09-19 第四轮） | `Patches/BossMapPointMaskPatch.cs`（立绘隐藏 + 放大"?"、节点可点击、悬停移除） | 补丁审计 NBossMapPoint |
| 药水槽位/名称/描述 50%（实机调整 2026-09-19 第五轮） | `Patches/HoverTipTextBlurPatch.cs`（药水上下文 50%，含空槽提示；源级 PotionModel 补丁已移除） | 补丁审计 NHoverTipSet |
| 顶栏 HP/金币数值保持可读（实机调整 2026-09-19 第五轮） | `Game/GlobalTextBlurSource.cs`（`HpLabel`/`GoldLabel` 跳过，源头与兜底共用） | `GlobalTextBlurRulesTests` |
| 标题/开场 logo 保持原版（实机调整 2026-09-19 第五轮回退） | `Patches/LogoBlurPatch.cs` 删除 | — |
| 1.1 奖励右键查看屏按获得场景（实机调整 2026-09-19 第二轮） | `Game/CardFogRenderer.cs`（NInspectCardScreen 源卡无牌堆 → 强制获得场景） | `CardContextClassifierTests` |
| 1.7 认知修改器面板（2026-09-20，原"难度调整器"） | `Game/DifficultyPanel.cs` + `Game/DifficultyRuntime.cs`（user:// 配置持久化）+ `Core/Options/DifficultySettings.cs` | `DifficultySettingsTests` |
| 1.7 选卡揭露 / 商店事件开关规则 | `Core/Options/SelectionRevealPlanner.cs`（随机槽位，确定性+盐值）+ `Core/Reveal/RevealRules.cs` + `Game/CardFogRenderer.cs`（槽位集合解析/刷新全场） | `SelectionRevealPlannerTests` + `RevealRulesTests` |
| 1.7 同名揭露关闭 = 按副本记忆 | `Core/Reveal/InstanceIds.cs`/`DeckOrderBinding.cs`/**`CardIdentity.cs`**（战斗克隆体身份沿 `DeckVersion`/`CloneOf` 回溯本体） + `Game/CardInstanceRegistry.cs` + `Game/RevealPersistence.cs`（实例 ID + 牌序重绑） | `InstanceAndBindingTests` + `CardRevealTrackerTests` |
| 1.7 实时状态/已拥有遗物/地图全路线（2026-09-20 第二批） | `Patches/HpSnapshotVisualPatch.cs`/`TopBarGoldSnapshotPatch.cs`/`PlayerHpBarHidePatch.cs`（实时）、`Patches/RelicHidePatch.cs`（RelicMasking 按拥有上下文）、`Game/MapFogController.cs`（RevealEverything）+ `Game/DifficultyRefresh.cs`（切换即时刷新） | `DifficultySettingsTests` |
| 1.7 可见敌人意图（2026-09-19） | `Game/IntentGate.cs`（开关短路）、`Game/DifficultyRefresh.cs`（即时刷新） | 编译期 |
| 1.7 卡牌本局全部揭示（2026-09-21） | `Core/Reveal/RevealRules.cs`（`RevealAllCards` 短路，全部上下文 FullFace）+ `Game/DifficultyPanel.cs` + `Game/DifficultyRuntime.cs`（`reveal_all_cards`） | `RevealRulesTests` |
| 1.7 面板边缘缩进（2026-09-21） | `Game/DifficultyPanel.cs`（标题栏 ◀/▶ 缩进到较近边缘 + 边缘小按钮弹出）+ `Game/DifficultyRuntime.cs`（`docked`/`dock_side`/`dock_y` 持久化） | 编译期 |
| 1.1 SL 读档按副本揭示保留（2026-09-21） | `Game/RevealPersistence.cs`（`RunStarted` 立即绑定）+ `Core/Reveal/DeckRebinder.cs`（按定义键对齐，容忍增删）+ `Game/BrainFogRunData.cs`（`DeckOrderKeys`） | `DeckRebinderTests` |
| 1.1 事件获得卡牌按获得场景（2026-09-21，闭合 §2.3） | `Patches/CardAcquireScopePatch.cs`（选择屏幕创建时打标：`NChooseACardSelectionScreen.ShowScreen`、`NSimpleCardSelectScreen.Create` 两个重载）+ `Game/CardAcquireScope.cs` + `Game/CardFogRenderer.cs`（命中标记 → 强制获得场景） | 补丁审计 |
| 1.4 敌人意图保持可读（2026-09-20） | `Core/Text/GlobalTextBlurRules.cs`（`NIntent` 子树豁免，源头与兜底共用）、`Patches/HoverTipTextBlurPatch.cs`（意图悬停提示按标题识别豁免） | `GlobalTextBlurRulesTests` |
| 1.4 前进按钮/火堆选项乱码（实机调整 2026-09-19 第二轮） | `Patches/RoomTextBlurPatch.cs`（默认 60%） | 补丁审计 NProceedButton/NRestSiteButton |
| 1.4 遗物不可见 | `Patches/RelicHidePatch.cs`（NRelic + RelicReward + 检视黑雾） | 补丁审计 |
| 1.4 Boss 遗物三选一可见 | `EventTextBlurPatch` 对 `AncientEventModel` 豁免 | 补丁审计 |
| 1.4 地图迷雾+画线禁用 | `Game/MapFogController.cs` + `Patches/MapFogPatch.cs` | 补丁审计 NMapScreen |
| 1.4 事件 75% 模糊（同次启动内固定、每次启动随机，2026-09-20） | `Core/Text/EventTextBlurrer.cs` + `Core/Text/BlurSalt.cs` + `Patches/EventTextBlurPatch.cs` | `EventTextBlurrerTests` |
| 1.5 仅单人守卫 | `Core/MultiplayerGuard.cs`、`ModRuntime`、`RunManagerMultiplayerGuardPatch` | `MultiplayerGuardTests` + 审计 |
| 全局部件：异常边界 | `Game/PatchGuard.cs`（每 key 一次性 Error 日志），关键类入口与前缀补丁均包裹 | 编译期 |
| 全局部件：新增文本字体 | `ApplyLocaleFontSubstitution`（濒危标签/加号标记，CJK 字体替换） | 编译期 |
| 全局部件：调试观测 | `BRAINFOG_DEBUG=1` → `ModRuntime.DumpState`（加载/开局输出揭示数、快照值、禁用状态） | 编译期 |
| 全局部件：发布形态 | manifest 字段/依赖 + 单程序集内含 Core 类型 | `ReleaseShapeTests` |
| 泄漏修复：悬停/高亮 | 未知卡不显示真实 HoverTips；手牌可打出高亮隐藏；遗物 Focus/奖励提示移除；特殊卡奖励文案与提示替换 | 编译期 + 审计 |
| 泄漏修复：排序/卡框 | 牌组查看排序在未知卡存在时回退获得序；获得场景隐藏类型卡框（仅稀有度） | 编译期 + 审计 |
| 适配层：Core 纯函数 | `MapFogRules`/`CardContextClassifier`/`MaskEligibility`/`IntentRevealGate`（+测试） | 单测 |
| 本地化 | `ModLocalization`（RitsuLib I18N，嵌入 zhs/eng：low_hp_warning/unknown_card） | 编译期 |
| 调试 | `BRAINFOG_DEBUG=1` → 状态 dump + F9 覆盖层 | 编译期 |
| 性能 | 在场卡组注册（揭示刷新精确查找）；雾规则/排序/持久化写入去抖；每帧路径去分配 | 编译期 |
| 战斗收尾 | 敌人死亡隐藏占位框 | 编译期 |

## 2. 已知限制 / 近似（晨间重点核查）

1. **黑雾层级**：`CardFogRenderer` 把黑雾矩形插到 `_frame` 的绘制位置（帧应压在其上）。若实机出现"整张牌全黑"或"帧被盖住"，调整 `MoveChild` 目标索引。
2. **悬停**：未做专门的悬停视觉（规格允许：未知牌悬停=边框+雾）。若原版悬停有额外放大/高亮，可能仍显示已知信息，需目视确认。
3. ~~**事件获得卡牌**~~（**已解决 2026-09-21**）：选择屏幕创建时打 `BrainFogAcquireScreen` 标记（`CardAcquireScopePatch`），`CardFogRenderer` 命中即按获得场景（仅稀有度边框）。验收时确认事件"获得卡牌"网格与通用选牌均为稀有度边框。
4. **呼吸方框**：已绑定 Spine 轨道相位（`GetTrackTime/GetAnimationDuration`）；无 Spine 动画的敌人回退相位 0（静态微光）。
5. **受击特效**：特效本身不受影响（VFX 容器独立）；**受击/治疗/格挡数字自 2026-09-21 起按 60% 乱码**（用户规则，撤销"数字可读"），验收时确认数字乱码且特效仍在。
7. **事件选项文本**：只在按钮 `_Ready` 时模糊；若后续流程重写文本（投票刷新等），可能回退为原文。
8. ~~**RitsuLib 持久化时序**~~（**已解决 2026-09-21**）：定义键与实例 ID 均随存档读写；实例 ID 在 `RunStarted` 立即按 `DeckOrderKeys` 重新绑定到牌组（旧存档退回索引绑定），SL 重进不再重置。旧 BlindSpire 存档（mod id 变更）不继承（预期）。
9. **图例**：地图图例保持可见（按 0.02 第 9 条"图例可见"），画线工具隐藏且右键绘制被拦截。

## 3. 晨间验收清单（约 10 分钟）

> 启动参数：在环境变量设置 `BRAINFOG_DEBUG=1` 可让日志输出状态摘要（`[BrainFog][State:...]`）。


1. **启动**：游戏加载 mod 无报错（日志 `mods/BrainFog/`；检查 `BrainFog.json` 依赖 RitsuLib 0.6.2 已装）。
2. **新局卡牌**：初始卡组全部黑雾（仅边框）；战斗中打出一张 → 该卡本局永久揭示（同名其他副本、之后获得的同名卡都显示真牌面，但**卡面文字 85% 乱码**，图鉴除外）；事件选牌只显示稀有度边框；奖励界面右键查看升级不露牌面（**含已揭示的卡**，如投掷匕首/连续反弹）；查看/升级界面的关键词描述 50% 乱码。
3. **升级**：锻造一张从未打出的牌 → 该卡（按定义）变真牌面。
4. **读档/SL**：揭示 2~3 张 → 退出到主标题 → 继续游戏 → 揭示状态保留（其余仍黑雾）；**按副本模式（关闭「同名卡全部揭露」）同样保留**（2026-09-21 修复：`DeckRebinder` 按定义键重绑实例 ID）。
5. **悬停**：手牌未知牌悬停/选中 → 边框+黑雾（无识别信息）。
6. **牌堆**：打开抽/弃/消耗堆 → 已揭示显示真牌面，未揭示黑雾。
7. **奖励/商店**：卡牌奖励与商店卡牌 → 仅稀有度边框；升级版显示"+"。
8. **顶栏**：受伤/加钱后 HP/金币数字不变（快照）；**HP 为灰色并标注"上次休息时的状态"+提示文字**；休息后刷新；商店/事件扣钱后金币刷新；战斗内自己角色血条不显示真实血量（数字与填充都隐藏，格挡仍可见）。
9. **濒危**：把 HP 打到 <15% → 角色红边 + 状态栏"濒危：我感觉自己快死了"（战斗内外）。
10. **战斗**：敌人=呼吸方框（呼吸节奏应跟随敌人原动画，悬停显示血条/能力但**不显示敌人名字**）；第 1 回合有意图、之后无；受击特效仍在；**伤害/治疗/格挡数字为乱码（60%，2026-09-21 调整）**；己方召唤物正常可见；战斗场景其余部分全可见（视野遮罩已移除）；**"战斗开始"横幅乱码**（60%）。
11. **战斗外**：遗物不可见（检视界面黑雾；宝箱/商店悬停无名称描述；遗物奖励行显示"未知遗物"）；Boss 遗物三选一：选项文字 90% 乱码（图标可见）；药水仅轮廓、名称与描述 50% 乱码且同一药水每次一致；地图仅当前/已走/下一层可见，画线按钮消失且右键无法画，**Boss 点显示"?"**（略小于原图标），**图例文字 70% 乱码**；事件文本 75% 乱码且每次进入一致；**先古之民（含建筑师）名称/描述/对话 90% 乱码（对话；名称描述 60%）、商人对话 90% 乱码**；标题界面与暂停菜单选项 75% 乱码（设置按钮可读）；**顶栏无阶段 Boss 图标（悬停描述同移除）**；**阶段横幅/前进按钮/火堆选项/所有提示介绍文字 60%**（源头乱码：结算"胜利/对建筑师造成…"、模式选择、角色选择、继续/主菜单按钮等）；标题 logo 与开场 logo 换为乱码文本；顶栏/地图 UI 描述 70% 乱码（设置/图鉴可读）；图鉴正常。
12. **联机**：进入联机对局 → 日志提示 BrainFog 已禁用（本 mod 不做联机适配）。
13. **异常自检**：若某功能未生效，日志搜索 `[BrainFog][` 前缀：`PatchGuard` 会记录首个失败点（补丁目标漂移的最小线索）。
14. **认知修改器**（对局内面板，可拖动/收起/完全隐藏，中/英文随游戏语言）：拖动标题栏到任意位置（重启后保留）；选卡揭露切到"随机2张"→ 奖励界面固定随机两张显真牌面（重开界面不重掷）；"不揭露"→ 恢复全黑雾；商店/事件开关即时生效；"同名卡全部揭露"关闭后打出一张打击 → 仅该张揭示（重进存档后仍只揭示那一张）；**「卡牌本局全部揭示」开启 → 全场卡牌立即显示真牌面（文字仍乱码），关闭恢复黑雾**；**标题栏 ◀/▶ 缩进到较近屏幕边缘（重启后仍缩进），点边缘小按钮弹出**；"显示实时血量/金币"→ 顶栏数值随受伤/加钱即时变化且无灰色标注；"显示已拥有遗物"→ 库存与检视可见（奖励/商店仍遮蔽）；"显示地图所有路线"→ 地图全节点路线可见；设置重启游戏后保留。
15. **可见敌人意图**（认知修改器）：开启 → 战斗中每回合都显示敌人意图（不止首回合）；关闭 → 恢复"仅首回合"；切换即时生效（含切换后新入场的敌人）。意图数字与悬停提示**不被乱码**（保持可读，便于判断伤害）。

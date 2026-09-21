# 实现映射与验收清单

> 规格：`docs/spec/SPEC-consolidated.md`（v0.3，已冻结）
> 游戏：STS2 v0.111.0；依赖：RitsuLib 0.6.2；仅单人。
> 验证状态：`bash tools/check.sh` = Release 构建 + 全部单测 + 补丁目标审计 + **shader 语法校验**（headless Godot；无二进制时跳过，`STRICT=1` 强制）全绿。**游戏内行为待人工验收（见下）。**
> **2026-09-21 用户改版：乱码比例统一**——所有文本（含此前分档）改由认知修改器「乱码百分比」滑块控制（0–100%，1% 步进，默认 60%）；血量/金币**默认实时**（数值随统一比例乱码），**快照模式**为面板选项（默认关）。下表各行中的旧比例仅作历史参考。
> **2026-09-21 六项认知规则（0.3.0，待验收）**——① 开局初始卡组揭示（好记性/坏记性，歪比巴卜除外）；② 感知模块「敌人模型可见」（默认关）；③ 坏记性 n 默认 2 + 新入组卡牌初始揭示；④ 「记忆消逝」战斗结束移除未揭示牌（默认开，歪比巴卜豁免，无保底）；⑤ 坏记性失忆提醒（最后机会的已揭示手牌变暗）；⑥ 「卡牌计数器」+ 右上角出牌计数排行榜（默认关，同名副本固定编号）。

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
| 1.2 失忆显示（HP/金币，2026-09-21 改面板选项，默认关=实时；原名"快照模式"） | `Core/Status/StatusSnapshot.cs`、`Game/SnapshotDisplay.cs`、`Patches/HpSnapshotVisualPatch.cs`/`TopBarHpSnapshotPatch.cs`/`TopBarGoldSnapshotPatch.cs`/`PlayerHpBarHidePatch.cs` | `StatusSnapshotTests` |
| 1.2 血量/金币数值可读开关（2026-09-21 追加，默认关） | `Core/Text/GlobalTextBlurRules.cs`（`IsStatusNumberLabel`：顶栏 `HpLabel`/`GoldLabel`）+ `Game/GlobalTextBlurSource.cs`（选项判定 + 本地玩家 `NHealthBar` 血条数字）+ `Game/TextBlurService.cs`（`Restore` 还原）+ `Game/DifficultyPanel.cs`（感知模块开关） | `GlobalTextBlurRulesTests` |
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
| 1.1 揭示卡面文字乱码（比例 2026-09-21 起随统一滑块） | **`Patches/CardFaceTextBlurPatch.cs`（源头乱码：MegaLabel/MegaRichTextLabel.SetTextAutoSize 前缀，图鉴豁免，性能 2026-09-20）** + `Game/DifficultyRuntime.cs` | `GlobalTextBlurRulesTests` |
| 1.5 统一乱码百分比滑块（2026-09-21） | `Game/DifficultyPanel.cs`（HSlider 0–100，1% 步进）+ `Game/TextBlurService.cs`（原文本元数据 `InputMeta` + `ReapplyAllText` 即时重应用）+ `Game/DifficultyRuntime.cs`（`text_blur_percent`） | `TextBlurPercentsTests` |
| 1.3 战斗开始横幅乱码（实机调整 2026-09-19） | `Patches/CombatStartBannerBlurPatch.cs`（默认 60%） | 补丁审计 NCombatStartBanner |
| 1.4 顶栏/地图 UI 描述 70% + 未规定文本默认 60%（统一悬停提示策略，实机调整 2026-09-19） | `Patches/HoverTipTextBlurPatch.cs` + `Patches/MapLegendTextBlurPatch.cs`（设置/图鉴豁免、药水跳过） | 补丁审计 NHoverTipSet/NMapLegendItem |
| 1.4 先古之民（含建筑师）对话与选项 90%（实机调整 2026-09-19） | `Patches/AncientDialogueBlurPatch.cs` + `EventTextBlurPatch.cs`（选项 90% 保留遗物图标） | 补丁审计 NAncientDialogueLine |
| 1.4 商人对话 90% 乱码（实机调整 2026-09-19） | `Patches/MerchantDialogueBlurPatch.cs` | 补丁审计 NMerchantDialogue |
| 1.4 顶栏阶段 Boss 图标彻底移除（实机调整 2026-09-19 第三轮） | `Patches/BossIconHidePatch.cs`（跳过 RefreshBossIcon/OnRoomEntered + 隐藏/失焦/悬停移除） | 补丁审计 NTopBarBossIcon |
| 1.4 阶段切换横幅乱码（实机调整 2026-09-19 第三轮） | `Patches/ActBannerBlurPatch.cs`（"阶段X/地区名" 60%） | 补丁审计 NActBanner |
| 全局：其余 UI 文本（2026-09-21 起统一滑块比例） | `Patches/GlobalTextBlurPatch.cs`（Mega 控件源头乱码）+ `Game/GlobalTextBlurSource.cs` + Core `Text/GlobalTextBlurRules.cs`（ShouldBlur 分类）；`Game/GlobalTextBlurDriver.cs` 仅兜底非 Mega 控件；设置/图鉴/意图/卡面/悬停/菜单/对话各自豁免 | `GlobalTextBlurRulesTests` |
| 受击/治疗/格挡数字乱码（用户规则 2026-09-21，撤销"数字可读"） | `GlobalTextBlurRules`（`NDamageNumVfx`/`NHealNumVfx`/`NDamageBlockedVfx` → 默认 60%）；其余 VFX 文本仍保持可读 | `GlobalTextBlurRulesTests` |
| 地图 Boss 点显示"?"（实机调整 2026-09-19 第四轮） | `Patches/BossMapPointMaskPatch.cs`（立绘隐藏 + 放大"?"、节点可点击、悬停移除） | 补丁审计 NBossMapPoint |
| 药水槽位/名称/描述 50%（实机调整 2026-09-19 第五轮） | `Patches/HoverTipTextBlurPatch.cs`（药水上下文 50%，含空槽提示；源级 PotionModel 补丁已移除） | 补丁审计 NHoverTipSet |
| 顶栏 HP/金币数值默认实时且乱码（2026-09-21 改版） | `Game/GlobalTextBlurSource.cs`（无顶栏数值豁免，随统一比例乱码）；快照模式见 1.2 行 | `GlobalTextBlurRulesTests` |
| 标题/开场 logo 保持原版（实机调整 2026-09-19 第五轮回退） | `Patches/LogoBlurPatch.cs` 删除 | — |
| 1.1 奖励右键查看屏按获得场景（实机调整 2026-09-19 第二轮） | `Game/CardFogRenderer.cs`（NInspectCardScreen 源卡无牌堆 → 强制获得场景） | `CardContextClassifierTests` |
| 1.7 认知修改器面板（2026-09-20，原"难度调整器"） | `Game/DifficultyPanel.cs` + `Game/DifficultyRuntime.cs`（user:// 配置持久化）+ `Core/Options/DifficultySettings.cs` | `DifficultySettingsTests` |
| 1.7 选卡揭露 / 商店事件开关规则 | `Core/Options/SelectionRevealPlanner.cs`（随机槽位，确定性+盐值）+ `Core/Reveal/RevealRules.cs` + `Game/CardFogRenderer.cs`（槽位集合解析/刷新全场） | `SelectionRevealPlannerTests` + `RevealRulesTests` |
| 1.7 同名揭露关闭 = 按副本记忆 | `Core/Reveal/InstanceIds.cs`/`DeckOrderBinding.cs`/**`CardIdentity.cs`**（战斗克隆体身份沿 `DeckVersion`/`CloneOf` 回溯本体） + `Game/CardInstanceRegistry.cs` + `Game/RevealPersistence.cs`（实例 ID + 牌序重绑） | `InstanceAndBindingTests` + `CardRevealTrackerTests` |
| 1.7 实时状态/已拥有遗物/地图全路线（2026-09-20 第二批） | `Patches/HpSnapshotVisualPatch.cs`/`TopBarGoldSnapshotPatch.cs`/`PlayerHpBarHidePatch.cs`（实时）、`Patches/RelicHidePatch.cs`（RelicMasking 按拥有上下文）、`Game/MapFogController.cs`（RevealEverything）+ `Game/DifficultyRefresh.cs`（切换即时刷新） | `DifficultySettingsTests` |
| 1.7 可见敌人意图三档（2026-09-21，9.20 任务：不可见/仅第一回合/可见所有意图） | `Core/Options/IntentVisibility.cs` + `Game/IntentGate.cs`（模式短路）、`Game/DifficultyRefresh.cs`（即时刷新） | `DifficultySettingsTests` |
| 1.7 卡牌本局全部揭示（2026-09-21） | `Core/Reveal/RevealRules.cs`（`RevealAllCards` 短路，全部上下文 FullFace）+ `Game/DifficultyPanel.cs` + `Game/DifficultyRuntime.cs`（`reveal_all_cards`） | `RevealRulesTests` |
| 1.7 面板边缘缩进（2026-09-21） | `Game/DifficultyPanel.cs`（标题栏 ◀/▶ 缩进到较近边缘 + 边缘小按钮弹出）+ `Game/DifficultyRuntime.cs`（`docked`/`dock_side`/`dock_y` 持久化） | 编译期 |
| 1.7 面板模块化 + 主标题可用 + 重置（2026-09-21，9.20 任务） | `Game/DifficultyPanel.cs`（文字/认知/感知三模块；主标题界面即可操作；「重置为默认」按钮）+ `Game/DifficultyRuntime.cs`（新键与旧配置迁移） | `DifficultySettingsTests` |
| 1.7 卡牌记忆四模式（2026-09-21，9.20 任务） | `Core/Options/CardMemoryMode.cs` + `Core/Reveal/RevealRules.cs`（好记性获得场景覆盖 / 歪比巴卜永不揭示 / 通晓万物全揭示）+ `Patches/CardModelPlayRevealPatch.cs`/`CardModelUpgradeRevealPatch.cs`（模式化揭示范围） | `RevealRulesTests` |
| 1.7 坏记性计数（2026-09-21，9.20 任务） | `Core/Reveal/BadMemoryCounter.cs` + `Game/BadMemoryTracker.cs` + `Patches/CardPileHandPatch.cs`（上手/离手计数）+ `Game/RevealPersistence.cs`（`BadMemoryCounts` 持久化） | `BadMemoryCounterTests` |
| 1.5 乱码模式（2026-09-21 追加） | `Core/Text/BlurSalt.cs`（固定盐 0 / 进程盐）+ `Core/Options/BlurSaltMode.cs` + `Game/DifficultyPanel.cs`（固定混乱 / 混乱混乱） | `DifficultySettingsTests` |
| 1.4 事件名称乱码修复（2026-09-21，9.20 任务） | `Patches/EventTitleBlurPatch.cs`（`NEventLayout.SetTitle` 后经 `SetTextAutoSize` 路由，事件名随统一比例乱码） | 补丁审计 NEventLayout |
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
| 1.1 开局初始卡组揭示（0.3.0 六项①） | `Game/InitialReveal.cs`（新局 `RunStarted` 且无存档时：好记性按定义、坏记性按副本揭示起始牌组）+ `Game/RevealPersistence.cs`（`OnInitialReveal` 批量写入 + 牌序刷新） | `CardRevealTrackerTests` |
| 1.3 敌人模型可见开关（0.3.0 六项②，默认关） | `Game/EnemyVisualMask.cs`（选项短路 → `Restore`，默认保持呼吸方框）+ `Game/DifficultyRefresh.cs`（扫描 `NCreature` 即时切换）+ `Game/DifficultyRuntime.cs`（`enemy_models_visible`） | `DifficultySettingsTests` |
| 1.7 坏记性：n 默认 2 + 新入组卡牌初始揭示（0.3.0 六项③） | `Core/Options/DifficultySettings.cs`（默认 2）+ `Patches/CardPileHandPatch.cs`（`CardPileDeckAddPatch` Deck 入组钩子）+ `Game/PlayCounterTracker.cs`（坏记性新卡揭示 + 编号） | `DifficultySettingsTests` |
| 1.7 记忆消逝（0.3.0 六项④，默认开，歪比巴卜豁免，无保底） | `Core/Reveal/MemoryFadeRules.cs` + `Game/MemoryFade.cs`（战斗结算按模式判定未揭示并移除）+ `Patches/PlayerAfterCombatEndPatch.cs`（存档写入前执行）+ `Game/RevealPersistence.cs`（`OnCardsRemoved` 清理揭示/计数/编号） | `MemoryFadeRulesTests` + 补丁审计 `Player.AfterCombatEnd` |
| 1.7 坏记性失忆提醒（0.3.0 六项⑤） | `Core/Reveal/BadMemoryCounter.cs`（`WouldForgetOnLeave`）+ `Game/BadMemoryTracker.cs`（`ShouldDim` + 每帧合并刷新）+ `Game/CardFogRenderer.cs`（`BrainFogDim` 覆盖层：仅手牌、已揭示、最后机会） | `BadMemoryCounterTests` |
| 1.7 卡牌计数器 + 出牌计数排行榜（0.3.0 六项⑥，默认关） | `Core/Reveal/CardNumberAllocator.cs`（稳定编号/不重排/格式）+ `Game/PlayCounterTracker.cs`（按副本计数、编号、榜单数据）+ `Game/PlayCounterPanel.cs`（右上角榜单、可收起、随语言）+ `Game/RevealPersistence.cs`（`PlayCounts`/`CardNumbers`/`CardNames`/`NextCardNumbers`） | `CardNumberAllocatorTests` |

## 2. 已知限制 / 近似（晨间重点核查）

1. **黑雾层级**：`CardFogRenderer` 把黑雾矩形插到 `_frame` 的绘制位置（帧应压在其上）。若实机出现"整张牌全黑"或"帧被盖住"，调整 `MoveChild` 目标索引。
2. **悬停**：未做专门的悬停视觉（规格允许：未知牌悬停=边框+雾）。若原版悬停有额外放大/高亮，可能仍显示已知信息，需目视确认。
3. ~~**事件获得卡牌**~~（**已解决 2026-09-21**）：选择屏幕创建时打 `BrainFogAcquireScreen` 标记（`CardAcquireScopePatch`），`CardFogRenderer` 命中即按获得场景（仅稀有度边框）。验收时确认事件"获得卡牌"网格与通用选牌均为稀有度边框。
4. **呼吸方框**：已绑定 Spine 轨道相位（`GetTrackTime/GetAnimationDuration`）；无 Spine 动画的敌人回退相位 0（静态微光）。
5. **受击特效**：特效本身不受影响（VFX 容器独立）；**受击/治疗/格挡数字自 2026-09-21 起按 60% 乱码**（用户规则，撤销"数字可读"），验收时确认数字乱码且特效仍在。
7. **事件选项文本**：只在按钮 `_Ready` 时模糊；若后续流程重写文本（投票刷新等），可能回退为原文。
8. ~~**RitsuLib 持久化时序**~~（**已解决 2026-09-21**）：定义键与实例 ID 均随存档读写；实例 ID 在 `RunStarted` 立即按 `DeckOrderKeys` 重新绑定到牌组（旧存档退回索引绑定），SL 重进不再重置。旧 BlindSpire 存档（mod id 变更）不继承（预期）。
9. **图例**：地图图例保持可见（按 0.02 第 9 条"图例可见"），画线工具隐藏且右键绘制被拦截。
10. **坏记性计数语义（实现定义 2026-09-21）**：按**副本**计数（实例 ID 随牌组绑定，SL 保留）；自然抽牌与效果回手都算"上手"；打出重置计数；离手未打出即 +1（含回合结束弃牌、战斗结束清手牌）；达到 n 时该副本变回未揭示并清零计数；计数随存档持久化。若"同一回合多次回手未打出"只应算 1 次，或战斗结束清手不算，需要调整。
11. **事件名称修复**：`NEventLayout.SetTitle` 直接写 `_title.Text` 会绕过源头乱码，已用 `EventTitleBlurPatch` 重新路由；若后续版本改为 `SetTextAutoSize` 则补丁成为 no-op。
12. **记忆消逝（实现定义 2026-09-21）**：只在战斗胜利结算（`Player.AfterCombatEnd`，存档写入前）移除；好记性按定义、坏记性按副本判定"未揭示"；**无保底**（卡组可能删空）；移除不播放原版移除动画、也不触发 `Hook.BeforeCardRemoved`（依赖该钩子的遗物不会感知）——如需完整走原版移除流程再调整。编号的 `NextCardNumbers` 不因移除回退（序号永久递增）。
13. **失忆提醒**：仅坏记性模式、仅手牌、仅已揭示副本（"本回合不打出将失忆"即 `UnplayedDraws + 1 >= n`，含 n=1 时刚上手即变暗）；变暗为深色覆盖层（`BrainFogDim`），不含动画。
14. **卡牌计数器（实现定义 2026-09-21）**：仅统计"本功能开启期间"的打牌（战斗内生成的无牌组副本不计数）；同名序号按副本固定（初始牌组按牌组顺序标定；移除后不重排、新卡往后续编）；榜单仅列出打出≥1次的副本，被记忆消逝移除的副本从榜单消失（计数/编号/名称条目同步清理）。
15. **初始卡组揭示**：仅"新开一局"（`RunStarted` 且无存档数据）时执行；读档继续不重置；歪比巴卜模式不受影响（保持永不揭示）；名字显示仍受统一乱码比例影响。

## 3. 晨间验收清单（约 10 分钟）

> 启动参数：在环境变量设置 `BRAINFOG_DEBUG=1` 可让日志输出状态摘要（`[BrainFog][State:...]`）。


1. **启动**：游戏加载 mod 无报错（日志 `mods/BrainFog/`；检查 `BrainFog.json` 依赖 RitsuLib 0.6.2 已装）。
2. **新局卡牌**：初始卡组全部黑雾（仅边框）；战斗中打出一张 → 该卡本局永久揭示（同名其他副本、之后获得的同名卡都显示真牌面，但**卡面文字随统一乱码百分比乱码**，图鉴除外）；事件选牌只显示稀有度边框；奖励界面右键查看升级不露牌面（**含已揭示的卡**，如投掷匕首/连续反弹）；查看/升级界面的关键词描述 50% 乱码。
3. **升级**：锻造一张从未打出的牌 → 该卡（按定义）变真牌面。
4. **读档/SL**：揭示 2~3 张 → 退出到主标题 → 继续游戏 → 揭示状态保留（其余仍黑雾）；**按副本模式（关闭「同名卡全部揭露」）同样保留**（2026-09-21 修复：`DeckRebinder` 按定义键重绑实例 ID）。
5. **悬停**：手牌未知牌悬停/选中 → 边框+黑雾（无识别信息）。
6. **牌堆**：打开抽/弃/消耗堆 → 已揭示显示真牌面，未揭示黑雾。
7. **奖励/商店**：卡牌奖励与商店卡牌 → 仅稀有度边框；升级版显示"+"。
8. **顶栏（默认实时）**：受伤/加钱后 HP/金币数字即时变化，但**为乱码**（随统一百分比）；开启「血量/金币快照模式」后回到旧行为：数字不变、**HP 灰色并标注"上次休息时的状态"+提示文字**、休息后刷新、商店/事件扣钱后金币刷新、战斗内自己角色血条隐藏真实血量（数字与填充都隐藏，格挡仍可见）。
9. **濒危**：把 HP 打到 <15% → 角色红边 + 状态栏"濒危：我感觉自己快死了"（战斗内外）。
10. **战斗**：敌人=呼吸方框（呼吸节奏应跟随敌人原动画，悬停显示血条/能力但**不显示敌人名字**）；第 1 回合有意图、之后无；受击特效仍在；**伤害/治疗/格挡数字为乱码（60%，2026-09-21 调整）**；己方召唤物正常可见；战斗场景其余部分全可见（视野遮罩已移除）；**"战斗开始"横幅乱码**（60%）。
11. **战斗外**（**2026-09-21：下列各文本乱码比例统一由面板滑块控制**）：遗物不可见（检视界面黑雾；宝箱/商店悬停无名称描述；遗物奖励行显示"未知遗物"）；Boss 遗物三选一：选项文字 90% 乱码（图标可见）；药水仅轮廓、名称与描述 50% 乱码且同一药水每次一致；地图仅当前/已走/下一层可见，画线按钮消失且右键无法画，**Boss 点显示"?"**（略小于原图标），**图例文字 70% 乱码**；事件文本 75% 乱码且每次进入一致；**先古之民（含建筑师）名称/描述/对话 90% 乱码（对话；名称描述 60%）、商人对话 90% 乱码**；标题界面与暂停菜单选项 75% 乱码（设置按钮可读）；**顶栏无阶段 Boss 图标（悬停描述同移除）**；**阶段横幅/前进按钮/火堆选项/所有提示介绍文字 60%**（源头乱码：结算"胜利/对建筑师造成…"、模式选择、角色选择、继续/主菜单按钮等）；标题 logo 与开场 logo 换为乱码文本；顶栏/地图 UI 描述 70% 乱码（设置/图鉴可读）；图鉴正常。
12. **联机**：进入联机对局 → 日志提示 BrainFog 已禁用（本 mod 不做联机适配）。
13. **异常自检**：若某功能未生效，日志搜索 `[BrainFog][` 前缀：`PatchGuard` 会记录首个失败点（补丁目标漂移的最小线索）。
14. **认知修改器**（**主标题界面与对局内均可操作**；可拖动/收起/边缘缩进；中/英文随游戏语言）：
    - 文字：**「乱码百分比」滑块：0% → 全可读 / 100% → 全乱码（豁免除外），拖动即时重应用**；**「乱码模式」固定混乱（重进不变）/ 混乱混乱（重进重掷，默认）**
    - 认知/获得卡牌：卡牌奖励 5 档（不揭示/随机1-3张/奖励全部揭示）；商店/事件卡面开关
    - 认知/卡牌记忆：**通晓万物**（全揭示）/ **好记性**（打出后同名卡含获得界面一并揭示）/ **坏记性**（n 次上手未打出即变回未揭示；n 可调，默认 1）/ **歪比巴卜**（永不揭示）
    - 感知：**血量/金币失忆模式**（默认关 = 实时+乱码；开启 = 灰显停留）；**血量数/金币数恢复正常显示**（默认关；开启后顶栏与自己的战斗血条数值可读）；显示已拥有遗物；显示地图所有路线；**可见敌人意图三档**
    - **「重置为默认」按钮**；全部设置跨重进保留
15. **可见敌人意图三档**：不可见（默认）→ 战斗中无意图；仅第一回合可见 → 首回合有、之后无（含新入场敌人不显示）；可见所有意图 → 每回合持续可见；切换即时生效。意图数字与悬停提示**不被乱码**（保持可读，便于判断伤害）。
16. **初始卡组揭示（0.3.0 六项①）**：新开一局 → 起始牌组（坏记性默认按副本）全部显示真牌面（卡面文字仍按统一比例乱码）；通晓万物本来就全可见；歪比巴卜仍全黑雾。
17. **敌人模型可见（0.3.0 六项②）**：感知模块打开「敌人模型可见」→ 敌人立即显示真实模型（呼吸方框消失），关闭后恢复方框；敌人名字仍隐藏（独立规则）；自己与召唤物不受影响。
18. **坏记性调整（0.3.0 六项③）**：默认 n=2（面板可调，重置为默认后为 2）；新获得的牌（奖励/商店/事件）入手时即为真牌面，之后每 2 次上手不打出会失忆。
19. **记忆消逝（0.3.0 六项④，默认开）**：坏记性下失忆的牌、好记性下从未打出过的牌，在战斗结束后从卡组消失（打开牌组确认）；歪比巴卜模式整局不删牌；**无保底**（卡组可能被删空）。
20. **失忆提醒（0.3.0 六项⑤）**：坏记性下某张已揭示手牌处于「本回合不打出就失忆」的最后机会 → 牌面明显变暗；打出后恢复；离开手牌后变黑雾。
21. **卡牌计数器（0.3.0 六项⑥，默认关）**：打开「卡牌计数器」→ 右上角出现「出牌计数」榜单（标题栏 ▾/▸ 收起/展开，状态持久化）；打出牌后榜单更新为「打击1 ×N」等（同名副本按加入顺序固定编号，移除后不重排）；关闭选项榜单消失。

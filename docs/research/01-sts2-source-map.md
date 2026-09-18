# STS2 v0.111.0 源码地图（BlindSpire 研读笔记 01）

> 来源：`.refs/sts2-v0.111.0/src/`（ILSpy 8.2 反编译，3538 个 .cs，只读）；官方 XML 文档 `sts2.xml`（20723 个 member，可用于按 summary 快速定位）。
> 游戏版本 v0.111.0（commit `41cef1ea`，2026-08-13），程序集 `sts2`（NET 9，Godot）。**以下路径均相对 `.refs/sts2-v0.111.0/src/`**。

## 0. 结论速览

- 纯 Godot + C# 游戏：唯一托管程序集 `sts2`（`sts2.csproj:5`），入口由 Godot 调 `godotsharp_game_main_init`（`GodotPlugins/Game/Main.cs:13`），游戏根节点是 `NGame`（`MegaCrit/sts2/Core/Nodes/NGame.cs:58`）。
- 架构 = 单例管理器 + 中央 Hook + 内容数据库：`CombatManager`(单例) / `RunManager` / `SaveManager` / `LocManager` / `ModManager`；所有跨系统回调集中在一个静态类 `Hook`（`MegaCrit/sts2/Core/Hooks/Hook.cs:32`）；所有内容都是 `AbstractModel` 子类，按类型名注册进 `ModelDb`（`MegaCrit/sts2/Core/Models/ModelDb.cs:20`）。
- 战斗是 async 命令式状态机：`CardModel.OnPlay` 里调 `DamageCmd`/`PowerCmd`/`CardPileCmd` 等 Command 类；回合循环在 `CombatManager`；伤害/格挡/状态的实际变更统一走 `CreatureCmd.Damage`（:258）。
- 内容枚举 = 源码生成 `AbstractModelSubtypes`（5197 行 Type 列表）+ 运行时反射 mods 程序集（`Core/Helpers/ReflectionHelper.cs:60`）。
- Mod = `mods/{id}/` 下 `manifest.json` + `{id}.dll` + `{id}.pck`；有 `[ModInitializer]` 就调指定方法，否则对 DLL 执行 `Harmony.PatchAll`（`Core/Modding/ModManager.cs:995`）。
- STS2 特有系统：Osty 宠物、Stars/Forge（Regent）、Enchantment/Affliction（附魔/折磨，可挂在卡上）、多人大厅与确定性回放/校验和、AutoSlay 自动跑关（QA 用）。

## 1. 顶层结构

### 1.1 命名空间与目录

`MegaCrit.Sts2.*` 是全部游戏代码；`MegaCrit.Sts2.addons.mega_text` 是文字渲染插件；另有 `System`/`GodotPlugins.Game`/`SentryAutoInit` 等胶水文件。文件量（`.cs`）：

| 目录 | 数量 | 职责 | 代表类型 |
|---|---|---|---|
| `MegaCrit/sts2/Core/Models` | 1725 | 全部内容模型（卡/能力/遗物/怪/事件…） | `AbstractModel`、`ModelDb` |
| `.../Core/Nodes` | 724 | Godot UI/场景节点 | `NGame`、`NCard`、`NCombatUi` |
| `.../Core/Multiplayer` | 155 | 联机、同步、回放 | `NetMessageBus`、`CombatReplay` |
| `.../Core/Entities` | 128 | 战斗实体（Creature/Player/Card 附属结构） | `Creature`、`Player`、`CardPile` |
| `.../Core/Saves` | 120 | 存档/迁移/统计 | `SaveManager`、`SerializableRun` |
| `.../Core/Timeline` | 74 | Epoch/解锁时间线 | `EpochModel` |
| `.../Core/Localization` | 59 | 本地化 | `LocManager`、`LocString` |
| `.../Core/DevConsole` | 49 | 内置调试控制台 | `DevConsole:18` |
| `.../Core/Runs` | 39 | 运行层（RunState/RunManager/RNG 集合） | `RunState`、`RunManager` |
| `.../Core/GameActions` | 39 | 玩家动作与排队执行（含联机同步） | `ActionExecutor:19` |
| `.../Core/Combat` | 32 | 战斗流程 | `CombatManager:39`、`CombatState:29` |
| `.../Core/AutoSlay` | 31 | 自动跑关（非交互模式） | `AutoSlayer` |
| `.../Core/Platform` `.../Debug` `.../Helpers` | 75 | Steam/日志/工具（Scene/Image/Reflection Helper） | `SceneHelper` |
| `.../GameInfo` | 14 | 上传游戏内统计 JSON | `NGameInfoUploader` |

### 1.2 程序集入口与启动顺序

1. `GodotPlugins/Game/Main.cs:13`：Godot 回调 `godotsharp_game_main_init`，注册脚本（`ScriptManagerBridge.LookupScriptsInAssembly`）。
2. 主场景根 `NGame._EnterTree`（`Core/Nodes/NGame.cs:528`）挂接 `%RootSceneContainer`/`%HoverTipsContainer`/`%GameTransitionRect` 等节点后，异步执行 `GameStartupWrapper` → `GameStartup`（`:650`）。
3. `GameStartup` 顺序（不可随意改）：`OneTimeInitialization.ExecuteVeryEarly`（`Core/Helpers/OneTimeInitialization.cs:43`，读设置 + **ModManager.Initialize**）→ `NDevConsole.Create` → 账户目录迁移 → `InitPools`（`NGame.cs:1304`，NCard/NGridCardHolder 对象池）→ `ExecuteEssential`（`OneTimeInitialization.cs:68`：Atlases、`LocManager.Initialize`、`AssemblyInfo.Init`、`ModelDb.Init`/`InitIds`、网络消息与 Action 类型表）→ 云存档/Profile → `LaunchMainMenu`（`NGame.cs:1007`）→ `ExecuteDeferred`（`:92`：全图集、`ModelDb.Preload`、JIT 预热）。
4. 退出：`NGame._ExitTree`（`:750`）反注册 `ModManager.OnModDetected`、`ModManager.Dispose`、关 Steam/Sentry。

### 1.3 子系统一览

战斗（CombatManager/CombatState/Hook/Commands/Powers/Intents）、运行（RunState/RunManager/Rooms/Map/Odds/Rewards）、内容（ModelDb + Models/*）、存档（SaveManager/Serializable* + 迁移）、本地化（LocManager/LocString/SmartFormat）、联机与回放（Multiplayer + GameActions 队列）、UI（Nodes + tscn）、Mod（Modding/*）、平台（Steam/Sentry/Leaderboard）、工具（AutoSlay/DevConsole/Debug）。

## 2. 战斗引擎

### 2.1 状态与流程

- `CombatState`（`Core/Combat/CombatState.cs:29`）：持有 `Allies/Enemies` 两个 `Creature` 列表、`RoundNumber`（从 1 起，玩家+敌人各一回合为一轮）、`CurrentSide`、`EscapedCreatures`、`_allCards`（含未入牌堆的悬浮卡）、`Encounter`、`Modifiers`、`MultiplayerScalingModel`；玩家/卡都由 creature/牌堆反推（类注释 `:22`）。`CreateCreature`（`:491`）会按地图坐标+act+combatId 给怪派生独立 `monster.Rng`（`:503`）。
- 每场战斗一份 `CombatTurnState`（`Core/Combat/CombatTurnState.cs:21`，internal）：持有 CancellationToken、`PlayersReadyToEndTurn/BeginEnemyTurn`、`PlayersTakingExtraTurn`、EndTurn/开始敌人回合的 `TaskCompletionSource`；回合循环只认它而非 manager 字段，防止上一场战斗的回调串台。
- `CombatManager`（`Core/Combat/CombatManager.cs:39`，单例 `:91`）：
  - `SetUpCombat:445` 建立状态；`StartCombatInternal:578` 主循环：触发 `Hook.BeforeCombatStart` → 开怪 → `StartTurn` → `AwaitTurnEndAndSwitchSides`，直到战斗结束（`:615`）。
  - `StartTurn:690`：`PlayerTurnPhase.None` → `Creature.BeforeTurnStart` → `Hook.BeforeSideTurnStart` → 玩家侧进入 `PlayerTurnPhase.Start`（清格挡、球被动、能量重置、抽牌在 phase 注释中列明）。
  - 玩家回合相位：`PlayerTurnPhase`（`Core/Combat/PlayerTurnPhase.cs:6`）= `None/Start/AutoPrePlay/Play/AutoPostPlay/End`。
  - `EndPlayerTurnPhaseOne/Two`（`:1522`/`:1745`）→ `SwitchSides:1856`（额外回合判定 `ShouldTakeExtraTurn`）→ `EndEnemyTurn`/敌人 AI `ExecuteEnemyTurn:1407`。
  - 结束：`EndCombatInternal:1302`：`Hook.AfterCombatEnd`→`AfterCombatVictory`→写回放→存盘→进度/成就；`CheckWinCondition:1378`。
- 历史记录：`CombatHistory`（`Core/Combat/History/CombatHistory.cs:20`）记录每次伤害/出牌，供“本回合”类效果与回放使用。

### 2.2 回合与意图

- 怪物 AI：`MonsterModel`（`Core/Models/MonsterModel.cs:31`）抽象 `GenerateMoveStateMachine()`（`:550`）；状态机部件 `MoveState`（`Core/MonsterMoves/MonsterMoveStateMachine/MoveState.cs:12`，带 `Intents` 数组与 `onPerform`）、`MonsterState`、`ConditionalBranchState`、`RandomBranchState`。
- 意图类型（`Core/MonsterMoves/Intents/`）：`AbstractIntent:17` + `SingleAttackIntent/MultiAttackIntent/AttackIntent/DefendIntent/BuffIntent/DebuffIntent/CardDebuffIntent/HealIntent/StatusIntent/SummonIntent/EscapeIntent/SleepIntent/StunIntent/DeathBlowIntent/HiddenIntent/UnknownIntent`。
- 意图 UI：`NIntent`（`Core/Nodes/Combat/NIntent.cs`）。

### 2.3 伤害 / 格挡 / 状态管线（核心）

伤害唯一收口 `CreatureCmd.Damage`（`Core/Commands/CreatureCmd.cs:258`），顺序：
1. `Hook.ModifyDamage`（`Core/Hooks/Hook.cs:1639`，All 类型，含 additive/multiplicative/上限，`AbstractModel.cs:1579/1613/1595`）→ `Hook.AfterModifyingDamageAmount`；
2. `Hook.BeforeDamageReceived`；
3. 格挡抵扣 `Creature.DamageBlockInternal`（`Core/Entities/Creatures/Creature.cs:431`）；
4. Osty（宠物）转移时的两次 `Hook.ModifyHpLost`（BeforeOsty/AfterOsty，`Hook.cs:910/925`）+ `ModifyUnblockedDamageTarget`；
5. `Creature.LoseHpInternal`（`Creature.cs:446`）→ 组装 `DamageResult`（`Core/Entities/Creatures/DamageResult.cs:9`：Blocked/Unblocked/Overkill/WasBlockBroken/WasFullyBlocked/WasTargetKilled）；
6. 逐个结果触发 `AfterBlockBroken/AfterCurrentHpChanged/AfterDamageGiven`，死亡目标走 `Kill`（`CreatureCmd.cs:446`）：`Hook.BeforeDeath/AfterDeath/AfterPreventingDeath`。
- 标志位 `ValueProp`（`Core/ValueProps/ValueProp.cs:12`）：`Move`（卡/怪攻击，吃力量/易伤）、`Unpowered`（遗物/药水/能力伤害，不吃力量）、`Unblockable`（毒等直扣血）、`SkipHurtAnim`。
- 格挡：`CreatureCmd.GainBlock:650`/`LoseBlock:713`；钩子 `BeforeBlockGained/AfterBlockGained/AfterBlockCleared/AfterBlockBroken`（`AbstractModel.cs:304-347`）。
- 状态（Power）增减：`PowerCmd.Apply:105`、`Decrement:185`、`TickDownDuration:195`（回合末递减，如 Vulnerable）、`ModifyAmount:220`；落点 `Creature.ApplyPowerInternal`（`Creature.cs:610`）。应用链触发 `BeforePowerAmountChanged/AfterPowerAmountChanged`。

### 2.4 Power（能力/buff）体系

- 基类 `PowerModel`（`Core/Models/PowerModel.cs:21`）：每个能力都是一个类，可挂 `CanonicalVars`（`DynamicVar`，用于描述数值）。
- 关键属性：`Type`（Buff/Debuff，`:142`）、`StackType`（`PowerStackType:5`：`None`（不显示数量）/`Counter`（显数、需命令增减）/`Single`（隐藏、恒 1））、`InstanceType`（`PowerInstanceType:3`：`None`/`Instanced`（如 TheBomb 每层独立实例）/`InstancedPerApplier`（按施加者各一份实例））、`AllowNegative`（`:238`）、`AmountOnTurnStart`（回合初快照，防同回合触发）、`SkipNextDurationTick`（怪给玩家上 duration 时跳首跳）、`ShouldScaleInMultiplayer`。
- 生命周期：`BeforeApplied:608` / `AfterApplied:619` / `AfterRemoved:628` / `ShouldPowerBeRemovedAfterOwnerDeath:637` / `ShouldOwnerDeathTriggerFatal:646`。
- 触发时机：实现 `AbstractModel` 的近 100 个虚钩子（见 2.7），常用：`ModifyDamageAdditive/Multiplicative`、`ModifyBlockAdditive/Multiplicative`、`AfterSideTurnStart/End`、`AfterPlayerTurnStart`、`AfterCardPlayed`、`AfterCardDrawn`、`AfterPowerAmountChanged`。
- 内置示例（`Core/Models/Powers/`，269 个文件）：属性类 `StrengthPower/DexterityPower/FocusPower`；易伤类 `VulnerablePower`（乘 1.5 并联动 PaperPhrog/Cruelty/Debilitate）、`WeakPower`、`FrailPower`；防御类 `IntangiblePower/BufferPower/ThornsPower/RegenPower/BarricadePower`；持续伤害 `PoisonPower/DoomPower/ConstrictPower`；层数类 `ArtifactPower`；形态类 `DemonFormPower/EchoFormPower`；STS2 新系统 `DieForYouPower`（Osty）等。
- 扣减语义：duration 类能力自己重写 `AfterSideTurnEnd` 调 `PowerCmd.TickDownDuration`（见 `VulnerablePower.cs` 末尾）。

### 2.5 卡牌模型

- `CardModel`（`Core/Models/CardModel.cs:35`，抽象）：构造 `(canonicalEnergyCost, CardType, CardRarity, TargetType...)`（`:1087`）；`CanonicalVars`（`:552`，`DamageVar/BlockVar/CalculatedDamageVar` 等）→ `DynamicVars`；`CanonicalKeywords`/`CanonicalTags`；升级 `OnUpgrade:1666`。
- 效果执行：`OnPlayWrapper:1858` 完整流程——推入 choice 上下文 → 进 `PileType.Play` → `Hook.ModifyCardPlayResultLocation` → `GeneratePlayCount:2029`（Replay）→ 每个 play：`Hook.BeforeCardPlayed` → `OnPlay:1644`（子类实现，里面调 Command）→ 附魔/折磨 OnPlay → `Hook.AfterCardPlayed` → 结算去向（Discard/Exhaust/移除，`:1989-2004`）→ 空手检查。
- 目标选择：`TargetType`（`Core/Entities/Cards/TargetType.cs:3`：None/Self/AnyEnemy/AllEnemies/RandomEnemy/AnyPlayer/AnyAlly/AllAllies/TargetedNoCreature/Osty）；合法性 `CardModel.IsValidTarget:1762`/`CanPlay:1708`；UI 侧 `NTargetManager`（`Core/Nodes/Combat/NTargetManager.cs:27`）。
- 实例语义：ModelDb 里是 canonical（不可变），进战斗必须 `ToMutable`/`CreateCard`/`CloneCard`（`CombatState.cs:426-452`）；`CreateClone` 用于复制卡。
- 费用：`CardEnergyCost:13`、临时费 `TemporaryCardCost`、全局修正 `LocalCostModifier`；星费 `SetStarCost*`（`:1279-1294`）。
- 牌堆：`PileType`（`Core/Entities/Cards/PileType.cs:7`：Draw/Hand/Discard/Exhaust/Play/Deck）；`CardPile:14`；命令 `CardPileCmd:36`（移动/抽/弃）、`CardCmd:32`（生成/升级/消耗）；`CardPlay:11`（一次打出的元数据：PlayIndex/PlayCount/IsAutoPlay/Resources）。
- 攻击执行器：`DamageCmd.Attack`（`Core/Commands/DamageCmd.cs:14`）→ `AttackCommand`（`Core/Commands/Builders/AttackCommand.cs:189`，链式 `.FromCard().Targeting().WithHitFx().Execute():527`，支持多段 `WithHitCount:489`）。

### 2.6 遗物 / 药水

- `RelicModel:22`：`Rarity`/`Pool`（反查池 `:174`）、`StackCount`（`:286`）、`IsAllowed/IsAllowedAtNeow`（`:435/444`）、`AfterObtained:547`、`Flash:459`；所有遗物钩子与 Power 同源（同一套 `AbstractModel` 虚方法）。
- `PotionModel:32`：`Rarity`/`Usage`/`TargetType`、`EnqueueManualUse:242`、`OnUseWrapper:297`（资源扣除+钩子）→ 子类 `OnUse:361`；池反查 `:109`。
- 奖励：`Core/Rewards/`（`Reward/RewardsSet/CardReward/RelicReward/PotionReward/GoldReward/CardRemovalReward/SpecialCardReward`），由 `RewardsCmd` + `Hook.TryModifyRewards` 驱动。

### 2.7 其它实体

- `EnchantmentModel:21`（附魔，挂在卡上加效果）与 `AfflictionModel:20`（折磨，负面、可挂卡）——STS2 新机制，各有 `OnPlay` 钩子与 `DynamicDescription`。
- `OrbModel:23`（Defect 球）：Passive/Evoke 值、`OrbCmd` 驱动、`PlayerCombatState.OrbQueue`。
- Osty（Necrobinder 宠物）：`OstyCmd`、`AfterOstyRevived`、`AfterSummon`。
- Hook 全表看 `Core/Hooks/Hook.cs:32` 的静态方法（约 140 个，命名即时机：`Before/After*`、`Modify*`、`TryModify*`）。

## 3. 内容注册

### 3.1 ModelDb

- `ModelDb`（`Core/Models/ModelDb.cs:20`）：`Init():423` 遍历 `AllAbstractModelSubtypes` 反射 `Activator.CreateInstance` 建 canonical 实例，key=`ModelId(category, entry)`；`Inject/Remove:438/452`（mod 用）；`InitIds:473` 分配网络排序 ID；`Preload:484` 预热卡图/图标。
- `ModelId`（`Core/Models/ModelId.cs:8`）：`category` = 直接基类类名 slugify（`GetCategoryType:520`），`entry` = 类名 slugify（`GetEntry:535`），如 `cards.strike_ironclad`、`powers.vulnerable`；字符串形如 `"cards.strike_ironclad"`（`ToString`）。
- 基类 `AbstractModel`（`Core/Models/AbstractModel.cs:29`）：canonical/mutable 双态（`AssertMutable:122`/`AssertCanonical:136`）、`DeepCloneFields/AfterCloned`、`ShouldReceiveCombatHooks`（决定是否参与战斗钩子遍历）、`ExecutionFinished` 事件（UI 用）。
- 枚举清单：源码生成类 `AbstractModelSubtypes.cs`（5197 行，`GenerateSubtypesAttribute` 触发）+ mod 程序集 `ReflectionHelper.GetSubtypesInMods`（`Core/Helpers/ReflectionHelper.cs:60`），两者合并（`ModelDb.cs:68`）。`ReflectionHelper.ModTypes:34` 在 ModManager 初始化后可枚举 mod 内所有类型。
- 各类内容的清单入口：卡 `AllCards:114`（从所有池拼）、卡池 `AllCardPools:120`（角色池 + 7 个共享池：Colorless/Curse/Deprecated/Event/Quest/Status/Token `:126`）、角色硬编码 5 人 Ironclad/Silent/Regent/Necrobinder/Defect（`:145`）、事件 18 个共享 + act 专属（`:157/194`）、药水/遗物按池+初始物（`:222/261`）、Act 4 个 Overgrowth/Underdocks/Hive/Glory（`:299`）。
- 池注册 mod 内容：`ModHelper.AddModelToPool<TPool,TModel>()`（`Core/Modding/ModHelper.cs:414`），池实现必须在 `GenerateAllCards`（`Core/Models/CardPoolModel.cs:69`）里调 `ConcatModelsFromMods`（`ModHelper.cs:443`）；卡池第一次访问后冻结，晚了就抛异常（`:431`）。角色模型 `CharacterModel:20` 定义 HP/金币/能量/`CardPool`/`RelicPool`/`PotionPool`/起始卡组/起始遗物。

### 3.2 本地化

- `LocManager`（`Core/Localization/LocManager.cs:169` 初始化）：每语言一个目录 `res://localization/{lang}/`，每个 JSON 文件是一张表（文件名=表名），flat key→string；非 eng 以 eng 为 fallback（`:412`）。
- key 结构：内容模型自带 `Id.Entry` 前缀。卡 `cards.{entry}.title/.description`（`CardModel.cs:108/127`）、能力 `powers.{entry}.*`（`PowerModel.cs:49/51`）、遗物 `relics.*`、药水 `potions.*`、事件 `events.{entry}.title` + `pages.INITIAL.description`（`EventModel.cs:62/64`）、选项 `{textKey}.title/.description`（`EventOption.cs:48`）；系统文案大表：`main_menu_ui`/`settings_ui`/`gameplay_ui`/`static_hover_tips` 等。
- `LocString`（`Core/Localization/LocString.cs:14`）：`(table,key)` + 变量字典，`GetRawText:102` 拿原文、`GetFormattedText:91` 走 SmartFormat 渲染 `{Damage:diff()}` 等函数；`KeyPathToLocString("cards/strike.title")`（`:172`）；`GetRandomWithPrefix` 支持 `key.0/1/2` 随机变体（`:205`）。
- Override：`user://localization_override/{lang}/*.json` 与 Weblate 嵌套目录（`LocManager.cs:430-441`）；mod 覆盖 = `res://{modId}/localization/{language}/{file}` 合并进同名表（`ModManager.cs:1119`）。
- 缺 key 会抛 `LocException`（`Core/Localization/LocTable.cs:38`）；启动校验错误会弹窗（`NGame.cs:1355`）。

### 3.3 资源与场景

- 约定：场景 `res://scenes/{inner}.tscn`（`Core/Helpers/SceneHelper.cs:7`），图片 `res://images/{inner}`（`Core/Helpers/ImageHelper.cs:7`）；卡/能力图集是 `.tres` atlas（`ImageHelper.GetImagePath("atlases/power_atlas.sprites/...")`，`PowerModel.cs:104`）。
- 图集加载器 `AtlasResourceLoader` 注册进 Godot（`OneTimeInitialization.cs:76`），`AtlasManager.LoadEssentialAtlases/LoadAllAtlases`；`PreloadManager.Cache` 缓存贴图/材质。
- Mod PCK 通过 `ProjectSettings.LoadResourcePack` 挂到 `res://`（`ModManager.cs:976`），因此 mod 资源路径必须放在 `res://{modId}/...` 下（本地化即为范例）。

## 4. 运行层

- `RunState`（`Core/Runs/RunState.cs`）：`CreateForNewRun:538`/`FromSerializable:557`；关键字段 `Acts:329`、`Map:366`、`CurrentRoom:440`、`MapPointHistory:295`、`VisitedEventIds:312`、`Rng:480`、`Odds:485`、`SharedRelicGrabBag:490`、`UnlockState:499`、`Modifiers/BadgeModels/ExtraFields`；`IterateHookListeners:812` 汇总全局监听者（含 ModHelper 订阅者）。
- `RunManager`（`Core/Runs/RunManager.cs:50`，单例）：`SetUpNewSingleplayer:296`/多人 `:328`/读档 `:356`/回放 `:416`；`GenerateMap:807`、`EnterMapCoord:841`、`EnterRoomInternal:1200`、`EnterRoom:1264`、`EnterNextAct:1316`、`EnterAct:1352`、`ToSave:678`（配合 `CanonicalizeSave:611` 做多人一致性）；持有所有同步器（`ActionQueueSynchronizer/CombatStateSynchronizer/…`）。
- 地图：`ActMap:12`（抽象 grid）→ `StandardActMap:87`（`CreateFor:111`，`PathGenerate:133` 造路径、`AssignPointTypes:247` 分配点型）；`MapPoint/MapPointType/MapCoord`；`RoomType`（`Core/Rooms/RoomType.cs:32`）与 `MapPointType` 是两套概念（注释详述，:5-28）。
- 房间：`AbstractRoom:10` → `CombatRoom:23`（`CombatRoomMode:7`：Active/Finished/VisualOnly）、`EventRoom/MapRoom/MerchantRoom/RestSiteRoom/TreasureRoom`；奖励在 `Core/Rewards/`。
- 事件：`EventModel:32`（`GenerateInitialOptions:303`、`IsAllowed:324`、`CreateScene:332`、`CalculateVars:406`）+ `EventOption:12`；随机数 `EventModel.Rng:138`。
- 存档：`SaveManager`（`Core/Saves/SaveManager.cs`）：`SaveRun:621`/`LoadRunSave:1079`；文件分 settings/prefs/progress/run/run_history（`Core/Saves/Managers/*`）+ 每 profile 目录；`SerializableRun:16` 是路径+时间+楼层+玩家+RNG+地图历史的 schema 根；schema 迁移 `MigrationManager` + `Core/Saves/Migrations/SerializableRuns/`（V12→V15…）；云同步 `CloudSaveStore`；mod 首次运行会复制未 mod 存档（`ModManager.cs:1261`）。
- RNG：`Rng`（`Core/Random/Rng.cs:28`）自带 `_counter` + `MegaRandom` 状态，`ToSerializable/LoadFromSerializable:357/64` 保证读档后序列一致；`Chaotic:25` 非确定性（禁用于 gameplay）。`RunRngSet`（`Core/Runs/RunRngSet.cs:11`）把可回放随机分成固定流：`UpFront/Shuffle/UnknownMapPoint/CombatCardGeneration/CombatPotionGeneration/CombatCardSelection/CombatEnergyCosts/CombatTargets/MonsterAi/Niche/CombatOrbGeneration/TreasureRoomRelics`；种子字符串只在 UI 显示，实际 seed 由 hash 得来（`:31-34`）。内容级 RNG `new Rng(player, ModelId)` 用 seed+slot+entry hash（`Rng.cs:50`）。
- 多人/回放：`NetGameType`（`Core/Multiplayer/Game/NetGameType.cs:3`：Singleplayer/Host/Client/Replay）；玩家动作走 `ActionQueueSet`+`ActionExecutor`（顺序=同步协议，`ActionExecutor.cs:19`）；回放在 `Core/Multiplayer/Replay/`（`CombatReplayWriter/CombatReplayEvent/Checksum`）。
- 每日/时间线：`Core/Daily/`（TimeServer 防作弊时钟 + 排行榜）、`Core/Timeline/EpochModel:25` + `Core/Unlocks/UnlockState:30`（解锁/纪元门控卡池）。
- `AutoSlay`（`Core/AutoSlay/AutoSlayer.cs:73`）：`--autoslay` 启动参数驱动的自动游玩（QA），通过 `NonInteractiveMode`（`Core/Helpers/NonInteractiveMode.cs:6`）短路 UI；`DevConsole`（`Core/DevConsole/DevConsole.cs:18`）提供调试命令。

## 5. Mod 扩展点

### 5.1 加载机制

- 扫描位置：可执行文件旁 `mods/`（递归）、`mods_STEAMTEST/`、Steam Workshop 订阅项（`ModManager.cs:248-263`）；任何 `*.json` 被当 manifest 尝试解析（`:555`）。
- Manifest 字段（`Core/Modding/ModManifest.cs:16`）：`id`（必填）/`name`/`author`/`description`/`version`/`has_pck`/`has_dll`/`dependencies:[{id,min_version}]`/`affects_gameplay`/`min_game_version`。
- 加载：DLL 路径 `{mod.path}/{id}.dll`，用当前 `AssemblyLoadContext` 加载；PCK 路径 `{id}.pck`，`ProjectSettings.LoadResourcePack`（`ModManager.cs:945-986`）。依赖做 Kahn 拓扑排序 + 用户手动顺序优先（`SortModList:345`），循环依赖报错（`:191`）。
- 入口：DLL 内带 `[ModInitializer("方法名")]` 的类会被调用（`ModInitializerAttribute.cs:11`、`ModManager.cs:995-1005`）；没有则对整个程序集 `Harmony.PatchAll`（`:1011`）。初始化时机 = `ExecuteVeryEarly`（设置读完后、LocManager/ModelDb 之前），**所以注册内容/本地化必须在此期间完成**。
- 其他规则：`min_game_version` 不满足直接 Failed；用户需在设置里同意 mod（`PlayerAgreedToModLoading`）；重复 id / 缺依赖 / 依赖版本不足都会 Failed 并在 `Mod.errors` 生成 `LocString` 供 UI 显示；运行中安装的 Workshop mod 标记 `AddedAtRuntime` 不会热加载（`:759`）；`--nomods` 跳过全部（`:235`）。
- 归属识别：`AssemblyInfo.ModForType`（`Core/Modding/AssemblyInfo.cs:7`）能把任意 Type 映射回 Mod；`GetGameplayRelevantModNameList` 用于多人 mod 一致性校验（`ModManager.cs:1133`）。
- 版本兼容兜底：mod DLL 引用旧版 `sts2`/`0Harmony` 时由 `AssemblyResolve` 强制解析到当前程序集（`ModManager.cs:1165`）。

### 5.2 常规内容扩展点

| 想加什么 | 做法 | 参考 |
|---|---|---|
| 新卡 | 继承 `CardModel`，写 `CanonicalVars/CanonicalKeywords/OnPlay/OnUpgrade`，用 `ModHelper.AddModelToPool` 进池 | `Cards/StrikeIronclad.cs`、`Cards/Anger.cs` |
| 新能力 | 继承 `PowerModel`，重写 `Type/StackType` + 需要的 `Modify*/After*` 钩子 | `Powers/VulnerablePower.cs`、`StrengthPower.cs` |
| 新遗物/药水/球/附魔 | `RelicModel/PotionModel/OrbModel/EnchantmentModel/AfflictionModel` 子类 | `Models/{Relics,Potions,Orbs,Enchantments,Afflictions}` |
| 新怪/遭遇 | `MonsterModel.GenerateMoveStateMachine` + `EncounterModel` | `Models/Monsters/Nibbit.cs` |
| 新事件 | `EventModel.GenerateInitialOptions` + `EventOption` | `Models/Events/` |
| 新角色 | `CharacterModel` 子类（含 3 个池 + 起始卡组/遗物），并注册进 `ModelDb.AllCharacters` 相关路径（受限，需 patch） | `Models/Characters/Defect.cs` |
| 自定义内容模型 | `ModelDb.Inject(type)`（`ModelDb.cs:438`）或 `ModHelper.AddModelToPool` | `ModHelper.cs:408` |
| 参与 Run/Combat 钩子 | `ModHelper.SubscribeForRunStateHooks/SubscribeForCombatStateHooks(id, delegate)`（`:465/:486`），注意 id 唯一、字典序排序 | `IterateAll*Subscribers` |
| 打补丁 | 无 initializer 时自动 Harmony.PatchAll；也可以自己建 Harmony 实例 | `ModManager.cs:1011` |

### 5.3 高频 hook / patch 目标

`AbstractModel`（一切内容的行为基类，`Core/Models/AbstractModel.cs:208` 起约 100 个钩子）、`Hook` 静态类（`Hook.cs:32`，跨模型调度与 `Modify*` 汇聚点）、`CreatureCmd`（伤害/治疗/死亡）、`CombatManager`（回合流程）、`CardModel.OnPlayWrapper`、`ModelDb`、`LocManager`、`NGame`。UI 侧：`NCard.Create`（`Core/Nodes/Cards/NCard.cs:772`）、`NCombatUi:28`、`NCombatRoom:34`、`NModalContainer:17`、`NGlobalUi:26`、`NHoverTipSet:18`、`NTopBar`、`NMainMenuSubmenuStack` / `NRunSubmenuStack`（菜单插入点）。

### 5.4 UI 扩展方式

- 场景必须是 Godot 场景（`.tscn`），mod 用 PCK 提供 `res://{modId}/...`，C# 侧用 `SceneHelper.Instantiate<T>` 或 `ResourceLoader` 加载；节点脚本类直接继承游戏内 `N*` 控件或 `Control`。
- 常用容器：`NRun.Instance.GlobalUi`（`NGlobalUi:26`，常驻 HUD 层）、`NModalContainer`（弹窗栈）、`NHoverTipSet`（描述悬浮）。
- 角色/卡面等美术走 atlas `.tres` 约定（见 3.3）。

### 5.5 兼容性注意

- 只允许 canonical→mutable 的方向：构造 `AbstractModel` 子类时若与已注册类型撞 `ModelId` 会抛 `DuplicateModelException`（`AbstractModel.cs:84`）。
- 池内容冻结时机早（第一次访问 `AllCards`），mod 注册必须发生在 `ExecuteEssential` 之前。
- 多人大厅会比对 `affects_gameplay` 的 mod 名单与版本（`ModManager.cs:1133`），影响联机组队。
- `TestMode`/`--nomods`/`editor` 下行为不同（DLL/PCK 不加载），写测试时用 `TestInitializers`。

## 6. 关键文件索引（按重要性）

| 文件:行 | 说明 |
|---|---|
| `MegaCrit/sts2/Core/Models/AbstractModel.cs:29` | 所有内容模型基类；约 100 个 `virtual` 钩子（`:208` 起） |
| `MegaCrit/sts2/Core/Models/ModelDb.cs:20` | 内容数据库；Init/Inject/GetId/Preload |
| `MegaCrit/sts2/Core/Models/AbstractModelSubtypes.cs:41` | 源码生成的全部模型类型表（mod 之前的基线） |
| `MegaCrit/sts2/Core/Hooks/Hook.cs:32` | 中央钩子调度（约 140 个静态方法） |
| `MegaCrit/sts2/Core/Combat/CombatManager.cs:39` | 战斗单例与回合循环 |
| `MegaCrit/sts2/Core/Combat/CombatState.cs:29` | 战斗状态容器 |
| `MegaCrit/sts2/Core/Combat/PlayerTurnPhase.cs:6` | 玩家回合相位语义（Start→AutoPrePlay→Play→AutoPostPlay→End） |
| `MegaCrit/sts2/Core/Combat/CombatTurnState.cs:21` | 单场战斗生命周期/取消令牌/ready 信号 |
| `MegaCrit/sts2/Core/Commands/CreatureCmd.cs:258` | 伤害/击杀唯一收口 |
| `MegaCrit/sts2/Core/Entities/Creatures/DamageResult.cs:9` | 伤害元信息（blocked/unblocked/overkill…） |
| `MegaCrit/sts2/Core/ValueProps/ValueProp.cs:12` | 伤害类型标志（Move/Unpowered/Unblockable） |
| `MegaCrit/sts2/Core/Models/PowerModel.cs:21` | 能力基类（Stack/Instance/Amount 语义） |
| `MegaCrit/sts2/Core/Commands/PowerCmd.cs:105` | 能力增减/递减命令 |
| `MegaCrit/sts2/Core/Models/CardModel.cs:1858` | 出牌主管线 `OnPlayWrapper`（子类写 `OnPlay:1644`） |
| `MegaCrit/sts2/Core/Commands/DamageCmd.cs:14` | 攻击入口（AttackCommand 链式构建器） |
| `MegaCrit/sts2/Core/Commands/Builders/AttackCommand.cs:189` | 攻击执行（目标/多段/VFX） |
| `MegaCrit/sts2/Core/Entities/Cards/TargetType.cs:3` | 目标类型枚举 |
| `MegaCrit/sts2/Core/Entities/Cards/CardPlay.cs:11` | 单次出牌上下文 |
| `MegaCrit/sts2/Core/Entities/Cards/CardPile.cs:14` | 牌堆实现；`PileType.cs:7` |
| `MegaCrit/sts2/Core/Commands/CardPileCmd.cs:36` / `CardCmd.cs:32` | 牌堆/卡牌命令 |
| `MegaCrit/sts2/Core/Models/RelicModel.cs:22` | 遗物基类 |
| `MegaCrit/sts2/Core/Models/PotionModel.cs:32` | 药水基类 |
| `MegaCrit/sts2/Core/Models/EnchantmentModel.cs:21` | 附魔基类（STS2 新） |
| `MegaCrit/sts2/Core/Models/AfflictionModel.cs:20` | 折磨基类（STS2 新） |
| `MegaCrit/sts2/Core/Models/OrbModel.cs:23` | 球基类 |
| `MegaCrit/sts2/Core/Models/MonsterModel.cs:31` | 怪物基类；`GenerateMoveStateMachine:550` |
| `MegaCrit/sts2/Core/MonsterMoves/MonsterMoveStateMachine/MoveState.cs:12` | 怪物招式状态 |
| `MegaCrit/sts2/Core/MonsterMoves/Intents/AbstractIntent.cs:17` | 意图基类 |
| `MegaCrit/sts2/Core/Models/CardPoolModel.cs:12` | 卡池与 mod 内容拼接点 |
| `MegaCrit/sts2/Core/Models/CharacterModel.cs:20` | 角色定义（池/卡组/遗物/HP） |
| `MegaCrit/sts2/Core/Helpers/ReflectionHelper.cs:60` | mod 类型反射（`ModTypes:34`） |
| `MegaCrit/sts2/Core/Localization/LocManager.cs:169` | 本地化初始化与表加载（`:412`） |
| `MegaCrit/sts2/Core/Localization/LocString.cs:14` | 本地化字符串 + SmartFormat |
| `MegaCrit/sts2/Core/Helpers/SceneHelper.cs:7` / `ImageHelper.cs:7` | res:// 路径约定 |
| `MegaCrit/sts2/Core/Runs/RunState.cs:538` | 运行状态（Rng/Odds/Map/历史） |
| `MegaCrit/sts2/Core/Runs/RunManager.cs:50` | 房间流转与存档协调 |
| `MegaCrit/sts2/Core/Runs/RunRngSet.cs:11` | 12 条可回放 RNG 流 |
| `MegaCrit/sts2/Core/Random/Rng.cs:28` | 可序列化 RNG |
| `MegaCrit/sts2/Core/Map/ActMap.cs:12` / `StandardActMap.cs:87` | 地图生成 |
| `MegaCrit/sts2/Core/Rooms/RoomType.cs:32` | 房间类型（与 MapPointType 的区别） |
| `MegaCrit/sts2/Core/Models/EventModel.cs:32` | 事件基类 |
| `MegaCrit/sts2/Core/Events/EventOption.cs:12` | 事件选项 |
| `MegaCrit/sts2/Core/Saves/SerializableRun.cs:16` | 运行存档 schema |
| `MegaCrit/sts2/Core/Saves/SaveManager.cs:621` | 存档主入口（SaveRun/LoadRunSave） |
| `MegaCrit/sts2/Core/GameActions/ActionExecutor.cs:19` | 动作队列执行（同步协议核心） |
| `MegaCrit/sts2/Core/GameActions/Multiplayer/PlayerChoiceContext.cs:10` | 选择上下文（模型栈） |
| `MegaCrit/sts2/Core/Modding/ModManager.cs:226` | mod 初始化（扫描/排序/加载） |
| `MegaCrit/sts2/Core/Modding/ModManifest.cs:16` | manifest 字段 |
| `MegaCrit/sts2/Core/Modding/ModHelper.cs:414` | mod 内容/钩子注册 API |
| `MegaCrit/sts2/Core/Modding/ModInitializerAttribute.cs:11` | mod 入口特性 |
| `MegaCrit/sts2/Core/Modding/AssemblyInfo.cs:7` | 类型→Mod 归属映射 |
| `MegaCrit/sts2/Core/Helpers/OneTimeInitialization.cs:43` | 三阶段启动（VeryEarly/Essential/Deferred） |
| `MegaCrit/sts2/Core/Nodes/NGame.cs:58` | 游戏根节点/启动流程 |
| `MegaCrit/sts2/Core/Nodes/Cards/NCard.cs:772` | 卡牌 UI 工厂 |
| `MegaCrit/sts2/Core/Nodes/CommonUi/NModalContainer.cs:17` | 弹窗容器（UI 扩展点） |
| `MegaCrit/sts2/Core/Nodes/CommonUi/NGlobalUi.cs:26` | 全局 HUD 层（UI 扩展点） |
| `MegaCrit/sts2/Core/Nodes/Rooms/NCombatRoom.cs:34` | 战斗房间 UI |
| `MegaCrit/sts2/Core/AutoSlay/AutoSlayer.cs:73` | 自动跑关（QA/参考用） |
| `MegaCrit/sts2/Core/DevConsole/DevConsole.cs:18` | 内置控制台命令 |

## 7. 研读建议（BlindSpire 下一步）

- 想改战斗数值：从 `Hook.ModifyDamage`/`CreatureCmd.Damage` + `AbstractModel.Modify*` 入手，先写“只读钩子”旁路验证。
- 想加内容：先照抄 `Anger.cs`（卡）+ `VulnerablePower.cs`（能力）+ `Nibbit.cs`（怪）三个模板，再在 `ModInitializer` 里 `ModHelper.AddModelToPool`。
- 想改 UI：PCK + `res://{modId}/...` + `SceneHelper.Instantiate`，插入点优先用 `NGlobalUi`/`NModalContainer`，避免直接 patch 场景树。
- 参考依赖：BaseLib / RitsuLib 的公开 API 不在本仓库；本笔记仅覆盖原版源码，两者能力边界待补（可另开 02-* 笔记）。

# 联机模式适配可行性评估（研读笔记 04）

> 状态：**评估完成，暂不实施**（2026-09-25 用户决定走方案 C：维持联机自动禁用；本文档留待将来需要时研读）。
> 对象：Slay the Spire 2 **v0.111.0** 联机（合作）模式；BrainFog **v0.3.8**。
> 资料与简称（引用格式 `简称/文件:行`）：
> - `BF/` = 本仓库 `src/`（BrainFog）
> - `GAME/` = `.refs/sts2-v0.111.0/src/MegaCrit/sts2`（游戏反编译）
> - `RL/` = `.refs/ritsulib-0.6.2/Runtime-0.111.0/src/STS2RitsuLib`（RitsuLib 主实现）

## 0. 结论摘要

| 方案 | 内容 | 可行性 | 风险 | 工作量 | 建议 |
| --- | --- | --- | --- | --- | --- |
| **A 纯本地"只盲自己"** | 保留表现层，知识状态 per-player/本地化，联机禁用删卡，`affects_gameplay:false` | **可行** | 中 | 1~2 天量级（8~10 文件 + 测试） | 若将来要做，首选 |
| **B 全员同款联机难度** | 保持 `affects_gameplay:true`，记忆状态全队一致，删卡走确定性 hook/官方通道 | 技术可行 | **高**（一次分叉=炸局，重连未实现） | 一周以上 + 联机实测 | 不建议 |
| **C 维持现状**（已选） | 联机自动禁用；可低成本做守卫前移与文档 | 已实现 | 极低 | 小 | 当前路线 |

关键前提：STS2 联机是**确定性锁步**——每个 peer 本地跑完整 `RunState/CombatState`，执行同一动作队列；host 只做排序、RNG 与 checksum 裁判，客机不写存档，**v0.111.0 断线重连未实现**。

## 1. 游戏联机模型关键事实

1. **锁步而非权威服务器**：`ActionQueueSynchronizer` 在 host 侧定序，`ActionExecutor` 在每个 peer 执行同一动作序列（`GAME/Core/GameActions/Multiplayer/ActionQueueSynchronizer.cs:13-16,140-170,236-305`；`GAME/Core/GameActions/ActionExecutor.cs:123-179`）。
2. **状态同步按节点/每玩家自报**：进入地图节点时各端广播自己的 `SerializablePlayer`（手牌/卡组/遗物/金币/RNG），非本人数据整体覆盖（`GAME/Core/Multiplayer/Messages/Game/SyncPlayerDataMessage.cs:8-17`；`GAME/Core/Multiplayer/CombatStateSynchronizer.cs:119-193`）。
3. **checksum 裁判 + 分叉即终止**：动作、回合、事件/休息房出口都会比对 checksum，不一致 → `StateDivergence` 踢人，客户端直接回主菜单（`GAME/Core/Multiplayer/Game/ChecksumTracker.cs:87-214`；`GAME/Core/Runs/RunManager.cs:1545-1556`；`GAME/Core/Entities/Multiplayer/NetError.cs:58-61`）。重连协议存在但客户端未实现（`NetError.cs:41-45`；`RunLobby.cs:103-135`）。
4. **官方支持 mod，但有严格握手**：双向比对 `id-version` 形式的 **gameplay mod 列表**，不一致 → `ModMismatch` 拒连；`affects_gameplay:false` 的 mod 不比对（`GAME/Core/Multiplayer/Connection/HandshakeManager.cs:114-137`；`GAME/Core/Modding/ModManager.cs:1129-1156`；`GAME/Core/Multiplayer/PeerVersionInfo.cs:37-59`）。
5. **无官方信息隐藏层**：远端手牌数据在本地内存里，仅"不渲染"；无 spoiler/spectator 机制可复用（`GAME/Core/Nodes/Multiplayer/NMultiplayerPlayerState.cs:630-632,1010`；`GAME/Core/Entities/Multiplayer/NetFullCombatState.cs:380-462`）。
6. **RitsuLib 支持联机**：host/client 判定、Sidecar 消息、`ManagedNetAction`、`RunSavedData` 大厅/开局/读档同步、分歧诊断齐备；其自身 `affects_gameplay:false`。注意其数据同步依赖两端都装 RitsuLib（尾挂机制，缺失端静默忽略）（`RL/Networking/Sidecar/*`；`RL/RunData/RunSavedDataStore.cs:53,64`；`RL/RunData/RunSavedDataRunSlot.cs:35-51`）。

## 2. BrainFog 功能分层与联机风险

| 层 | 功能 | 作用域 | 联机风险 |
| --- | --- | --- | --- |
| A | 文本乱码、卡面黑雾渲染、敌人模型/名字/意图、遗物/药水/事件图标、地图迷雾、Boss 图标、本玩家顶栏/血条/濒危提示 | 纯本地节点表现 | 无状态风险；各端随机盐不同→乱码样式/奖励揭示槽不同 |
| B1 | 揭示知识（`CardRevealTracker`/`CardInstanceRegistry`/坏记性计数） | 本地记录 | 事件源是全局的（`CardPile`/`CardModel` 补丁无 owner 过滤），会记入队友行为；实例 ID 各端随机 GUID，跨端无意义 |
| B2 | 初始卡组揭示（`InitialReveal`） | 本地记录 | 遍历 `state.Players`，会把队友起始卡组也写进本地知识 |
| B3 | 揭示持久化（`RevealPersistence`） | RitsuLib **整局共享槽** | 共享槽 host 权威：客机写入不传播、读档被 host 覆盖；应改 `RegisterPerPlayer` 或本地存储 |
| C | 「记忆消逝」`CardPileCmd.RemoveFromDeck` | **共享游戏状态** | 不走游戏 action/网络通道，直接改卡组+地图历史；且只处理 `IsMe` → 各端只删自己玩家的牌，必分叉（对照原版商店删卡有 `CardRemovedMessage`） |
| D | 联机守卫 | 本地开关 | 仅在 `InitializeRunLobby`（开局）触发；大厅/菜单阶段 mod 仍活跃；全有全无 |

主要证据：`BF/Game/CardFogRenderer.cs:34-54`、`BF/Game/InitialReveal.cs:18-49`、`BF/Patches/CardPileHandPatch.cs:12-53`、`BF/Game/RevealPersistence.cs:23,55-94`、`BF/Game/MemoryFade.cs:26-87`（`:77` 删卡）、`BF/Patches/PlayerAfterCombatEndPatch.cs:18`、`BF/Patches/HealRestSiteSnapshotPatch.cs:10-23`（队友休息会刷新本地快照显示）、`BF/Core/MultiplayerGuard.cs:9-11`、`BF/Patches/RunManagerMultiplayerGuardPatch.cs:11-18`。

关键对照（游戏侧）：`GAME/Core/Commands/CardPileCmd.cs:53-89`（无权限校验、无网络消息）、`GAME/Core/Combat/CombatManager.cs:1293-1363`（战斗结算在所有端本地触发）、`GAME/Core/Multiplayer/Game/RestSiteSynchronizer.cs:105-218`（休息选择本地+远端消息都执行）、`GAME/Core/Models/Cards/Guilty.cs:45-56`（官方"战斗结束删卡"是确定性 hook，所有端一致执行）。

## 3. 方案 A：纯本地"只盲自己"（若将来实施）

要点：
1. **知识状态 per-player**：tracker 按 `NetId` 分桶；`CardPile`/`CardModel` 事件入口用 `LocalContext.IsMe(card.Owner)` 过滤；`InitialReveal` 只处理 `LocalContext.GetMe(state)`。
2. **持久化改作用域**：`RunSavedDataStore.RegisterPerPlayer<BrainFogRunData>`（按 netId），或改本地文件；实例 ID 只在本地会话内有效，不跨端共享。
3. **联机禁用共享状态写入**：「记忆消逝」在联机下禁用（或只提示不删卡）；其余功能零共享状态。
4. **`affects_gameplay` 改 `false`**：允许未安装 BrainFog 的好友联机；RitsuLib 作为依赖双方都会有，但对方无 RitsuLib 时自定义同步静默降级（本 mod 的纯本地模式不依赖同步）。
5. **守卫前移**：进入联机大厅即设 `Disabled`（而非等到 `InitializeRunLobby`），避免菜单/大厅文本被改。
6. **可选一致性**：乱码盐改为确定性（按 run/seed）可让各端乱码样式一致；不然保持各端随机（不影响正确性）。

验证要点：零共享状态写入（除 per-player 数据）；`StateDivergence` 报告无新增；联机下逐项验收层 A 功能 + 记忆消逝确认不触发。风险：中（`affects_gameplay:false` 若误标会静默 desync，需完整证明只读表现层；隐藏信息在客户端可被绕过，属"自愿难度"，公开房需谨慎）。

## 4. 方案 B：全员同款联机难度（不建议）

若目标改成"全队一起被脑雾影响"：
- 保持 `affects_gameplay:true`，要求全员安装同版本 BrainFog（否则 `ModMismatch`）；
- 记忆状态必须全队一致（例如 host 权威知识表），否则删卡判定集合不同 → 分叉；
- 删卡改走所有端执行的确定性 hook 或官方同步通道（`AfterCombatEnd` 对所有玩家一致执行/`RewardSynchronizer`/`ManagedNetAction`）；
- 风险：一次不一致即踢且无法回局（v0.111.0 重连未实现）；需要联机实测矩阵。工作量一周以上。

## 5. 方案 C：维持现状（当前决定）

- 保持 `MultiplayerGuard.ShouldDisable = isMultiplayer` 与 `RunManager.InitializeRunLobby` 触发；
- 文档明确"不支持联机"（README/AGENTS/IMPLEMENTATION-MAP 已注明）；
- 低成本可选项（未做）：守卫前移到进入联机大厅即禁用、面板提示"联机已禁用"。
- 注意：当前 `BrainFog.json` 为 `affects_gameplay:true`，意味着**联机双方都要装同版本 BrainFog 才能连上**（连上后本 mod 又自动禁用）。若"好友联机不想装 mod"成为诉求，需要连同方案 A 一起把 `affects_gameplay` 改为 `false`，不能只改清单。

## 6. 留待研读的问题（将来）

1. `RevealPersistence` 的 per-player 槽在断线无人重连的现实下怎样保证客机体验（读档时记忆恢复）。
2. RitsuLib Sidecar 在"仅 host 装 RitsuLib"场景的行为边界（尾挂数据解析缺失）。
3. 联机下队友卡牌/遗物/药水在本地是否应遮挡（层 A 的 owner 过滤策略：当前会连带遮队友）。
4. 官方后续版本是否实现重连（当前 `NetError.RunInProgress` 注释明确未实现）——决定方案 B 的可行性上限。

## 7. 证据索引

- 游戏：`GAME/Core/Multiplayer/Connection/HandshakeManager.cs:114-137`；`GAME/Core/Multiplayer/Game/ChecksumTracker.cs:87-214`；`GAME/Core/GameActions/Multiplayer/ActionQueueSynchronizer.cs:140-305`；`GAME/Core/GameActions/ActionExecutor.cs:123-179`；`GAME/Core/Commands/CardPileCmd.cs:53-89`；`GAME/Core/Combat/CombatManager.cs:1293-1363`；`GAME/Core/Multiplayer/Game/RestSiteSynchronizer.cs:105-218`；`GAME/Core/Models/Cards/Guilty.cs:45-56`；`GAME/Core/Entities/Multiplayer/NetError.cs:41-70`；`GAME/Core/Runs/RunManager.cs:1545-1556,1753-1759`。
- RitsuLib：`RL/RunData/RunSavedDataStore.cs:53,64`；`RL/RunData/RunSavedDataRunSlot.cs:35-51`；`RL/RunData/RunSavedDataPlayerSlot.cs:116-133`；`RL/RunData/RunSavedDataLobby.cs:29-53`；`RL/Networking/Sidecar/RitsuLibSidecarEndpoints.cs`；`RL/Networking/ManagedActions/RitsuLibManagedNetActions.cs:102-301`；`RL/Networking/MessageExtensions/RitsuNetMessageTailExtensions.cs:36-366`。
- BrainFog：`BF/Core/MultiplayerGuard.cs:9-11`；`BF/Patches/RunManagerMultiplayerGuardPatch.cs:11-18`；`BF/Game/RevealPersistence.cs:23,55-94`；`BF/Game/MemoryFade.cs:26-87`；`BF/Game/InitialReveal.cs:18-49`；`BF/Patches/CardPileHandPatch.cs:12-53`；`BF/Game/DifficultyRuntime.cs:245-260`。

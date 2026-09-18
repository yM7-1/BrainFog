# RitsuLib 0.6.2 研读笔记

> 对象：Slay the Spire 2 mod 框架库 **RitsuLib**（作者 OLC），`version 0.6.2`、`min_game_version 0.107.1`、`affects_gameplay: false`（`PKG/mod_manifest.json`）。
> 资料与简称（引用格式 `简称/文件:行`）：
> - `RL/` = `.refs/ritsulib-0.6.2/Runtime-0.111.0/src`（Runtime 主实现，2222 个 .cs，只读参考）
> - `ENT/` = `.refs/ritsulib-0.6.2/STS2-RitsuLib-0.111.0/src`（compat 入口/门面程序集）
> - `SH/` = `.refs/ritsulib-0.6.2/Shared/src`
> - `UI/` = `.refs/ritsulib-0.6.2/Ui/src`
> - `ST/` = `.refs/ritsulib-0.6.2/Settings/src`
> - `GAME/` = `.refs/sts2-v0.111.0/src`（游戏本体对照）
> - `PKG/` = `D:/Steam/steamapps/workshop/content/2868840/3747602295`（发布物；Loader 无源码，结论来自对 `PKG/STS2-RitsuLib.dll` 的 ILSpy 反编译）

## 1. 定位与模块划分

RitsuLib 是「多版本兼容 + 声明式注册 + Harmony 补丁管线 + 设置/持久化/本地化/UI」的通用框架。发布物不是单个 dll，而是 **1 个版本无关 Loader + 1 个按游戏版本选择的 compat 变体 + 3 个共享模块**。

| 模块 | 程序集/位置 | 角色 | 依赖 |
|---|---|---|---|
| Loader | `PKG/STS2-RitsuLib.dll`（编译名 `STS2-RitsuLib.Loader`） | 游戏唯一加载的入口；选变体、验哈希、装载其余模块，最后反射调用 `RitsuLibFramework.Initialize()` | 仅游戏程序集 |
| 入口/门面 | `PKG/compat/<ver>/STS2-RitsuLib.dll` | 每版本一份；1372 条 `TypeForwardedTo` 把公开类型转发到 Runtime/Shared/Ui/Settings；含 Godot 插件胶水（`ENT/GodotPlugins/Game/Main.cs:11`） | 编译期引用 Runtime+Shared+Ui+Settings（`ENT/STS2-RitsuLib.csproj`） |
| Runtime | `PKG/compat/<ver>/STS2-RitsuLib.Runtime.dll` | 框架本体：生命周期、注册表、补丁、设置、持久化、网络、脚手架全在这 | → Shared、Ui、Settings、sts2、0Harmony、SmartFormat（`RL/STS2-RitsuLib.Runtime.csproj`） |
| Shared | `PKG/shared/STS2-RitsuLib.Shared.dll` | 跨游戏版本共用基座：生命周期事件类型、`ModDataStore`、`I18N`、`ResourcePack`、Json 工具、平台兼容 | → GodotSharp、sts2（`SH/STS2-RitsuLib.Shared.csproj`） |
| Ui | `PKG/shared/STS2-RitsuLib.Ui.dll` | 可复用 UI：控件、Shell 主题、Toast、Overlay、浮动窗口、文件对话框 | → Shared、sts2、0Harmony（`UI/STS2-RitsuLib.Ui.csproj`） |
| Settings | `PKG/shared/STS2-RitsuLib.Settings.dll` | 设置页框架：页面/条目/值绑定/反射提供器/BaseLib 配置镜像 | → Ui、Shared、sts2（`ST/STS2-RitsuLib.Settings.csproj`） |

- 依赖方向：`Loader → Runtime → {Ui, Settings, Shared}`，其中 `Settings → Ui → Shared`；**Shared 是最底层且跨版本稳定**，Runtime/Ui/Settings 随 compat 变体与 SDK 演进。
- mod 编译期只需引用门面 `STS2-RitsuLib.dll`：所有公开类型经 `[assembly: TypeForwardedTo]`（`ENT/Properties/AssemblyInfo.cs`，1372 条）转发到真实实现，运行时由 Loader 保证同一份。
- 稳定标识在 `SH/STS2RitsuLib/Const.cs`：manifest id `STS2-RitsuLib`（:13 名称、:19 内部 ModId `com.ritsukage.sts2-RitsuLib`、:25 版本、:31 创意工坊 ID）。注意 **依赖声明用 manifest id（`STS2-RitsuLib`），不是内部 ModId**。

## 2. 生命周期与注册 API

### 2.1 从游戏加载到框架初始化

1. 游戏 ModManager：解析 manifest → 依赖校验/拓扑排序（`GAME/MegaCrit/sts2/Core/Modding/ModManager.cs:330`）→ 加载 `<modId>.dll`（`:953`）→ 找 `[ModInitializer]` 类型并反射调用静态初始化方法（`:996-1002`）；没有初始化器则退回 `Harmony.PatchAll`（`:1011`，即配置/注册代码不会执行）。
2. RitsuLib 的初始化器是 **Loader 里的 `STS2RitsuLib.Loader.Bootstrap.Initialize`**：解析 `ritsulib-variants.manifest` 选 compat 变体并逐文件校验 SHA256，用自建 `AssemblyLoadContext` 装载 5 个模块，把变体程序集关联到游戏 mod `STS2-RitsuLib` 并给 ReflectionHelper 打桥，再对每个模块调 `EnsureGodotScriptsRegistered`，最后反射调用 Runtime 的 `RitsuLibFramework.Initialize`。因为 RitsuLib 是依赖项，它的初始化一定先于 BlindSpire 的 `[ModInitializer]`。
3. `RitsuLibFramework.Initialize`（`RL/STS2RitsuLib/RitsuLibFramework.cs:706`）：初始化设置存储/搜索/日志管线/遥测；按 7 个区域注册必需补丁并 `PatchAllRequired`（`:758-778`，区域枚举 `:188`：Core、SettingsUi、ContentAssets、CharacterAssets、ContentRegistry、Persistence、Unlocks）；启动运行时服务（`:780-792`）；发布 `FrameworkInitializingEvent`/`FrameworkInitializedEvent`（`:755`/`:805`），置 `IsInitialized`/`IsActive`（`:803`）。
4. 之后的事件由补丁在游戏原生节点发布：`OneTimeInitialization.ExecuteEssential/ExecuteDeferred` → Essential/Deferred 事件（`RL/STS2RitsuLib/Lifecycle/Patches/CoreInitializationLifecyclePatch.cs:42,57`）；`ModelDb.Init` → 冻结内容注册 + `ModelRegistryInitializingEvent`（`RL/STS2RitsuLib/Lifecycle/Patches/ModelRegistryLifecyclePatch.cs:59`）；`LocManager.Initialize` → 运行类型发现 `ModTypeDiscoveryHub.RunOnce` 并冲刷延迟内容包（`RL/STS2RitsuLib/Interop/Patches/ModTypeDiscoveryPatch.cs:33,56`）；主菜单 → `MainMenuReadyEvent`（预热设置镜像，`RL/STS2RitsuLib/RitsuLibFramework.cs:793-802`）。
5. 档案服务由档案补丁调用 `EnsureProfileServicesInitialized`（`:875`）后发布 `ProfileServicesInitializedEvent`（`:889`），并初始化所有 profile 作用域的 `ModDataStore`。

### 2.2 生命周期事件

框架事件定义在 `SH/STS2RitsuLib/`（`IFrameworkLifecycleEvent.cs:16`；实现 `IReplayableFrameworkLifecycleEvent` 的事件会被保留并可在订阅时回放）：

| 事件 | 触发时机 | 可回放 |
|---|---|---|
| `FrameworkInitializing/Initialized` | 框架初始化前后（`:755`/`:805`） | 否 |
| `EssentialInitializationStarting/Completed` | `ExecuteEssential` 前后（`SH/...:13`） | Completed 是 |
| `DeferredInitializationStarting/Completed` | `ExecuteDeferred` 前后（`SH/...:13`） | Completed 是 |
| `ModelRegistryInitializing/Initialized`、`ModelIdsInitializing/Initialized`、`ModelPreloadingStarting/Completed` | `ModelDb.Init/InitIds/Preload`（`SH/...:13-17`） | Initialized/Completed 是 |
| `ProfileServicesInitializing/Initialized` | 档案服务就绪（`SH/...:17`） | Initialized 是 |
| `ProfileDataReady/Changed/Invalidated` | 存档数据就绪/切换/失效（`SH/STS2RitsuLib/Utils/Persistence/ProfileDataReadyEvent.cs:39`、`ProfileDataChangedEvent.cs:25`、`ProfileDataInvalidatedEvent.cs:24`） | Ready 是 |
| `GameTreeEntered`、`GameReady` | 节点树进入/游戏就绪（`SH/...GameTreeEnteredEvent.cs:18`） | 是 |
| `MainMenuReady` | 主菜单就绪（`SH/...MainMenuReadyEvent.cs:13`） | 是 |
| `RunStarted`/`RunLoaded`/`RunEnded` | 开局/读档/结束（`SH/...RunStartedEvent.cs:26` 等） | 否 |
| `ContentRegistrationClosed` | 内容注册冻结（`SH/...:17`） | 是 |
| `TelemetryStartupSnapshotReady` | 遥测启动快照（`SH/...:17`） | 是 |

- 订阅 API：`SubscribeLifecycle<TEvent>(handler, replayCurrentState = true)`（`RL/.../RitsuLibFramework.cs:548`）、带 `IDisposable` 参数的重载（`:625`）、一次性 `SubscribeLifecycleOnce`（`:2451`）、观察者接口 `ILifecycleObserver`（`:478`）；回调异常会被捕获并写日志，不会打断派发（`:684-695`）。
- 另有 **约 80 个战斗/卡牌/遗物/药水/房间级事件**（顶层 `RL/STS2RitsuLib/*Event.cs`，如 `CardPlayedEvent`、`BlockGainedEvent`、`RelicObtainedEvent`……），由 `RL/STS2RitsuLib/Lifecycle/Patches/*LifecyclePatch.cs` 发布（例：`AfterCardPlayedLifecyclePatch.cs:40`）；存档类事件 `RunSaving/Saved`、`ProgressSaving/Saved`、`ProfileSwitching/Switched/Deleting/Deleted` 也在同目录。这些事件体量大、覆盖面广，是替代手工 Harmony 补丁的首选监听方式。

### 2.3 内容注册

- 每 mod 一张注册表：`ModContentRegistry.For(modId)`（`RL/STS2RitsuLib/Content/ModContentRegistry.cs:1707`），提供 `RegisterCard/Relic/Potion/Character`（`:1729`/`:1783`/`:1837`/`:1888`）、角色初始卡/遗物/药水、章节与古代选项、卡池/资产覆盖、图鉴过滤器等。
- 推荐入口是流式内容包：`RitsuLibFramework.CreateContentPack(modId)`（`RL/.../RitsuLibFramework.cs:1464`）→ `ModContentPackBuilder`（`RL/STS2RitsuLib/Scaffolding/Content/ModContentPackBuilder.cs:43`）链式注册 → `Apply()`（`:1941`）把注册排队为「延迟内容包」，在 `LocManager.Initialize` 时统一应用（`ModTypeDiscoveryPatch.cs:56`）。
- 注册窗口：`ModelDb.Init` 时冻结（`ModelRegistryLifecyclePatch.cs:59`，同时发布 `ContentRegistrationClosedEvent`）；之后再注册会失败，所以内容声明应在 `Initialize`/早期事件里完成。
- 其他注册表按能力从 `RitsuLibFramework` 取：Keyword（`:1019`）、CardTag（`:1045`）、CardPile（`:1058`）、Timeline（`:1203`）、Unlock（`:1216`）、SecondaryResource（`:980`）、TopBarButton（`:1190`）、NodeAttachment（`:993`）、ModelClone（`:1229`）。注册项 ID 会加 mod 前缀保证唯一（`ModContentRegistry.cs:1546` 等 `GetCompoundId/GetQualified*`）。

### 2.4 声明式自动注册

- 在类型上标注 `[RegisterCard]`、`[RegisterRelic]`、`[RegisterCharacter]`、`[RegisterAct]`、`[RegisterMonster]`、`[RegisterEpoch]`、`[AutoTimelineSlot]`、`[RegisterNodeAttachmentFromScene]`、`[RegisterModelCapability]` 等（基类 `RL/STS2RitsuLib/Interop/AutoRegistration/AutoRegistrationAttribute.cs:9`、`ContentRegistrationAttribute.cs:7`；示例 `RegisterCardAttribute.cs:14`），无需手写注册调用。
- 管线：`ModTypeDiscoveryHub.RunOnce`（`RL/STS2RitsuLib/Interop/ModTypeDiscoveryHub.cs:162`）扫描「游戏已关联到 mod」的程序集（也可手动 `RegisterModAssembly`，`:89`；内置贡献器注册 `:148`），由 `AttributeAutoRegistrationTypeDiscoveryContributor`（`.../AutoRegistration/AttributeAutoRegistrationTypeDiscoveryContributor.cs:44`）确定性排序并执行；在 `LocManager.Initialize` 触发（`ModTypeDiscoveryPatch.cs:33`）。
- 归属 modId 取不到时用 `[RitsuLibOwnedBy("BlindSpire")]`（`RitsuLibOwnedByAttribute.cs:19`）覆盖，否则自动注册会被跳过（`:1946` 会告警）。

### 2.5 Harmony 补丁辅助

- `RitsuLibFramework.CreatePatcher(ownerModId, patcherName)`（`RL/.../RitsuLibFramework.cs:1917`）创建带独立日志的 `ModPatcher`（`RL/STS2RitsuLib/Patching/Core/ModPatcher.cs:43`），一个 patcher 持有一个 Harmony 实例。
- 封装的 patch 模式/能力：
  - 声明式静态补丁 `IPatchMethod`（`Patching/Models/IPatchMethod.cs:13`：`PatchId/Description/IsCritical/GetTargets`）——支持多目标、重载签名（`ModPatchTarget.cs:36`）、`MethodType.Getter/Setter`、`IgnoreIfMissing`、失败是否致命；`RegisterPatch<TPatch>`/`RegisterPatches<T:IModPatches>`（`Patching/Core/ModPatcherExtensions.cs:35,30`，接口 `IModPatches.cs:13`）。
  - 规则扫描补丁 `ModPatchRule`+`PatchRuleBuilder`（`Patching/Rules/ModPatchRule.cs:19`、`PatchRuleBuilder.cs:10`）→ `RegisterFromRule`（`ModPatcherExtensions.cs:20`）。
  - 运行时动态补丁 `DynamicPatchBuilder`（`Patching/Builders/DynamicPatchBuilder.cs:18`，支持 Prefix/Postfix/Transpiler/Finalizer、按名/按属性 getter 添加）→ `ApplyDynamic`（`ModPatcherExtensions.cs:49`）。
  - 应用/回滚：`PatchAll`（`ModPatcher.cs:650`）、`ApplyLateStaticPatches`（`:698`）、对指定方法撤销外部补丁 `UnpatchExternalPatches`（`:753`）、`UnpatchAll`（`:816`）；`ApplyRequiredPatcher(patcher, disableMod)`（`RL/.../RitsuLibFramework.cs:2206`）失败即禁用本 mod。
  - 辅助：`PrivateAccess`（`RL/STS2RitsuLib/Patching/PrivateAccess.cs`）访问私有成员；`Patching/Compat/` 下是 Harmony 兼容护栏。
- 框架自身也使用同一套（`RegisterLifecyclePatches`/`RegisterContentRegistryPatches` 等，`RitsuLibFramework.cs:2755` 起）。

### 2.6 跨 mod 与网络

- `[ModInterop("OtherMod")]`（`RL/STS2RitsuLib/Interop/ModInteropAttribute.cs:28`）+ `InteropTarget/InteropClassWrapper`：运行时重写 stub，无需编译期引用即可调用其他 mod 的成员。
- 多人同步：`RL/STS2RitsuLib/Networking/`（ManagedActions、Sidecar、状态分歧检测等），提供消息注册/同步/校验框架；`RitsuLibManagedGameAction/NetAction` 在 Runtime 公开。

## 3. 常用能力

### 3.1 持久化

- 全局/档案数据：`ModDataStore.For(modId)`（`SH/STS2RitsuLib/Data/ModDataStore.cs:339`），`Register<T>(key, fileName, SaveScope, ...)`（`:455`；云同步重载 `:496`；自定义存储上下文 `:533`）。作用域 `SaveScope.Global/Profile/InMemory`（`SH/STS2RitsuLib/Utils/Persistence/SaveScope.cs:8`）。批量注册用 `BeginModDataRegistration`/`BeginRegistrationScope`（`RL/.../RitsuLibFramework.cs:927`、`ModDataStore.cs:320`）；`ModDataStoreCache<T>` 提供失效感知缓存；JSON 落盘 + 迁移框架（`SH/.../Utils/Persistence/Migration/`）；`RegisterModCloudPersistedSlot`（`RL/.../RitsuLibFramework.cs:1582`）登记云同步槽。
- 就绪时序：`DataReadyLifecycle.IsReady`（`SH/.../DataReadyLifecycle.cs:30`）与 `ProfileDataReady/Changed/Invalidated` 事件；档案切换会重载 profile 作用域数据。
- 局内数据：`RunSavedDataStore.For(modId)`（`RL/STS2RitsuLib/RunData/RunSavedDataStore.cs:11`），`Register<T>`（`:53`，整局共享）、`RegisterPerPlayer<T>`（`:64`，按玩家）；带 `RunSavedDataOptions`（写入时机/联机策略）。
- 模型附加数据：`ModelSavedDataStore.For(modId)`（`RL/STS2RitsuLib/Models/Capabilities/ModelSavedDataStore.cs:12`），`Register<TTarget,TPayload>`（`:54`）或 `RegisterComputed`（`:69`，值直接从模型导入导出）。
- 随机数：`RitsuLibFramework.GetModRunRng/GetModPlayerRng`（`RL/.../RitsuLibFramework.cs:2534,2543`）按 mod+stream 派生确定性 RNG，联机/回放安全。

### 3.2 本地化

- `I18N`（`SH/STS2RitsuLib/Utils/I18N.cs:25`）：从文件目录/嵌入资源/PCK 读 JSON 字典，`Get`（`:348`）、`FromResourcePack`（`:296`）、`ResolveCurrentLanguageCode`（`:910`）、`ForceReload`（`:494`）。
- `RitsuLibFramework.CreateLocalization`（`:1937`）/`CreateModLocalization`（`:1977`，默认目录 `user://<platform>/<userId>/mod_data/<modId>/localization`）/`WithFallback` 变体；设置页文本可用 `ModSettingsText.LocString/I18N`（`ST/.../ModSettingsText.cs:329,361`）。
- 接入游戏原生 `LocString/LocTable` 管线：`RegisterI18NLocTableBridge(modId, i18n, stem)`（`:2044`），虚拟表 ID 形如 `MODID_I18N_STEM`（`RL/STS2RitsuLib/Localization/I18NLocTableBridge.cs:30`）；I18N 变更自动刷新，Dispose 自动注销。
- SmartFormat 自定义格式化器：`GetSmartFormatRegistry`（`:1032`）→ `ModSmartFormatExtensionRegistry`（`RL/STS2RitsuLib/Localization/SmartFormat/`）。

### 3.3 设置 UI

- 命令式：`RitsuLibFramework.RegisterModSettings(modId, builder => ...)`（`RL/.../RitsuLibFramework.cs:1607`）；`ModSettingsPageBuilder`（`ST/STS2RitsuLib/Settings/ModSettingsPageBuilder.cs:99` 构造、`:303 AddSection`、`:326 Build`）→ `ModSettingsSectionBuilder` 提供 `AddHeader/Paragraph/InfoCard`（`:169,181,191`）、`AddToggle`（`:261`）、`AddIntSlider/AddSlider`（`:271,291`）、`AddChoice/AddEnumChoice`（`:342,413`）、`AddColor/String/MultilineString`（`:451,503,540`）、`AddKeyBinding/AddInputBinding`（`:554,573`）、`AddButton/AddSubpage/AddCustom`（`:613,640,650`）、`AddList`（`:235`）、运行时快捷键汇总 `AddRuntimeHotkeySummary`（`:204`）。排序 API `RegisterModSettingsSidebarOrder/PageOrder*`（`RL/...:1677-1725`）。
- 声明式（反射）：类上 `[ModSettingsPage("modId")]`（`ST/.../ModSettingsPageAttribute.cs:10`）+ `[ModSettingsSection]`（`:10`）+ 字段/属性上 `[ModSettingsToggle/IntSlider/Slider/Choice/Color/String/KeyBinding/Button/Subpage/...]`（如 `ModSettingsToggleAttribute.cs:10`），值绑定用 `[ModSettingsBinding(Source = Global/Profile/InMemory/Callback/Project, DataKey=..., ReadUsing=...)]`（`ModSettingsBindingAttribute.cs:10`；枚举 `ModSettingsReflectionBindingSource.cs:7`）。显式注册 `RegisterModSettingsReflectionProvider`（`RL/...:1620`），或依赖主菜单预热时对全部已加载程序集的 `[ModSettingsPage]` 自动扫描（`ST/.../RuntimeReflectionMirrorSource.cs:127,136`；总入口 `ST/.../ModSettingsMirrorRegistrarBootstrap.cs:11`）。
- 值绑定接口 `IModSettingsValueBinding<T>` 及内置实现（默认/回调/成员同步/内存/结构化列表），支持自动保存、复制粘贴、可见/可用条件、宿主面控制。
- **BaseLib / ModConfig / JmcModLib 配置镜像**：已有的 BaseLib `SimpleModConfig` 页面会自动出现在 RitsuLib 设置中（`ST/.../BaseLibMirrorSource.cs:12`，同类 `ModConfigMirrorSource`、`JmcModLibMirrorSource`）——同时依赖 BaseLib 与 RitsuLib 的 mod 无需重复实现设置界面。
- 浮层提示：`RitsuToastService.Show`（`UI/STS2RitsuLib/Ui/Toast/RitsuToastService.cs:136`）。

### 3.4 通用工具

- 日志：`RitsuLibFramework.Logger` 为游戏 `Logger`（类型 `GAME/MegaCrit/sts2/Core/Logging/Logger.cs:12`）；`CreateLogger(modId)`（`RL/.../RitsuLibFramework.cs:1874`）建 mod 专属日志器，patchers 自带日志上下文；`Diagnostics/RitsuDebugLogPipeline` 提供日志查看器。
- 反射：`FastMethodInvoker`（`SH/.../Utils/FastMethodInvoker.cs:37`，委托化调用）、`PrivateAccess`、`AssemblyTypeScanHelper`、`ReflectionHelper` 类型缓存桥。
- 资源：`ResourcePack`（`SH/.../Utils/ResourcePack.cs:27`，`FromZip` :62 / `FromDirectory` :83）；框架自带资源在 `PKG/assets.zip`（`SH/STS2RitsuLib/RitsuAssetStore.cs:11`）；内嵌 PNG 加载器 `RL/STS2RitsuLib/RitsuLibEmbeddedPngResourceLoader.cs:12`；`FileOperations`、Godot 节点/场景辅助（`Shared/Utils`、`RL/STS2RitsuLib/Scaffolding/Godot/`）。
- Json：`JsonPatch/JsonMergePatch/JsonPointer/JsonCanonicalizer`（`SH/STS2RitsuLib/Utils/Json/`）。
- 输入：`RuntimeHotkeyService.Register`（`RL/.../RuntimeInput/RuntimeHotkeyService.cs:188`），文本绑定规范化 + 设置页快捷键汇总。
- 其他：`RegisterRightClick`（`RL/.../RitsuLibFramework.cs:3503`）、`RitsuSearch`（拼音搜索）、`RitsuModManager` 模组清单（`RL/STS2RitsuLib/Compat/RitsuModManager.cs:11`）、框架存在性探测 `ExternalFrameworkRegistry`（`SH/.../Compat/ExternalFrameworkRegistry.cs:42,57`）、遥测 / 更新检查（`:1783` 起、`:3665` 起）、异常/彩蛋策略（`SH/STS2RitsuLib/RitsuLibExceptionPolicy.cs`、`RitsuLibEasterEggPolicy.cs`）。
- **与 BaseLib 互操作**：`ConfirmExternalFrameworkInterop`（`RL/.../RitsuLibFramework.cs:837-844`）在延迟初始化后检测 BaseLib 并注册生命条预测/视觉移植/最大手牌等桥；外部框架 ID 见 `SH/.../Compat/ExternalFrameworkIds.cs`（`baselib` 等）。

## 4. 版本兼容机制

### 4.1 发布物结构与多版本变体

`PKG/` 结构（当前安装只含 0.111.0 变体）：

```
STS2-RitsuLib.dll            # Loader（版本无关）
mod_manifest.json            # id=STS2-RitsuLib, version=0.6.2, min_game_version=0.107.1
ritsulib-variants.manifest   # schema=2；shared[] + variants[]（compatTarget + 文件 + sha256）
RitsuLib.References.props    # 给下游 mod 的 csproj 引用集成
assets.zip                   # 框架 UI/字体等资源
shared/                      # Shared/Ui/Settings 三个 dll（跨版本）
compat/<ver>/                # 每游戏版本：STS2-RitsuLib.dll（门面）+ Runtime.dll
viewer/                      # 日志查看器（离线 html）
```

`ritsulib-variants.manifest` 声明了 0.107.1 / 0.109.0 / 0.110.0 / 0.111.0 四个变体，每个文件的 sha256 都写死。

### 4.2 运行时如何选变体（Loader 反编译结论）

1. 探测宿主游戏版本 `Sts2HostVersion.Resolve`：依次尝试 `ReleaseInfoManager.Instance.ReleaseInfo.Version` → 游戏目录 `release_info.json` → launcher `.cache_stamp` → 游戏程序集版本；解析支持 `v` 前缀与 `-`/`+` 后缀。
2. `BundleLayout.Read`（`STS2RitsuLib.Loader`）：要求 manifest `schema == 2`；变体数 1~64；`compatTarget` 必须是 `X.Y.Z`；**选 `compatTarget <= host` 中最大者**；无匹配则抛 `NotSupportedException`。
3. 校验：shared/变体模块数量与名字必须匹配清单、逐个校验 SHA256、所有模块程序集版本必须一致（防混装）；哈希不符直接抛错。
4. 装载：`ModuleAssemblyResolver` 挂到当前 `AssemblyLoadContext`，已加载同名程序集时报冲突/“已从其他安装加载”；成功后把变体程序集通过 `ModManager.AssociateAssemblyWithMod("STS2-RitsuLib", asm)` 关联到游戏 mod（失败则用反射桥兜底）。
5. 结论：**升级游戏版本时 RitsuLib 会自动落到 ≤ 当前版本的最近变体**；不要手工改名/重打包 `compat`、`shared` 内文件，否则哈希校验失败。

### 4.3 `RitsuLib.References.props` 与 csproj 集成

props 做什么（`PKG/RitsuLib.References.props`）：

- `RitsuLibReferenceRoot` = props 文件所在目录；若 `compat/` 下**恰好一个**版本目录，自动把它设为 `RitsuLibReferenceTarget`；多于一个时必须由使用方显式指定，否则报错。
- 用目标校验正则 `^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)$`；为 5 个程序集加 `Reference`（`STS2-RitsuLib`、`STS2-RitsuLib.Runtime` → `compat/<target>/`；`Shared`、`Ui`、`Settings` → `shared/`），全部 `Private="False"`（不会复制进你的输出）。
- `ValidateRitsuLibDirectoryReferences` target 在 `ResolveAssemblyReferences` 前校验文件存在，缺文件即报错。

可抄的最小 csproj：

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netcoreapp9.0</TargetFramework>
    <LangVersion>11.0</LangVersion>
    <AssemblyName>BlindSpire</AssemblyName>
    <GenerateAssemblyInfo>False</GenerateAssemblyInfo>
  </PropertyGroup>

  <PropertyGroup>
    <RitsuLibRoot>D:/Steam/steamapps/workshop/content/2868840/3747602295/</RitsuLibRoot>
    <!-- 若 compat/ 下今后有多个版本目录，再取消注释：
    <RitsuLibReferenceTarget>0.111.0</RitsuLibReferenceTarget> -->
  </PropertyGroup>
  <Import Project="$(RitsuLibRoot)RitsuLib.References.props" />

  <ItemGroup>
    <Reference Include="GodotSharp" />   <!-- 游戏安装目录 -->
    <Reference Include="sts2" />         <!-- 游戏安装目录 -->
    <Reference Include="0Harmony" />
    <Reference Include="BaseLib" />      <!-- 本项目同时依赖 BaseLib 时 -->
  </ItemGroup>
</Project>
```

要点：`Import` 必须在定义 `RitsuLibRoot`/`RitsuLibReferenceTarget` 之后；props 只管 RitsuLib 自身，`sts2/GodotSharp/0Harmony`（及 BaseLib）仍需自行用 HintPath 指到游戏安装目录/BaseLib 目录（参照 BaseLib 笔记 `02-baselib-api.md` §3.2）。

### 4.4 版本元数据与门控

- Runtime 程序集带 `[assembly: AssemblyMetadata("Sts2ApiCompat", "0.111.0")]`（`RL/Properties/AssemblyInfo.cs:26`），启动日志据此打印 `[compat branch: ...]`（`RL/STS2RitsuLib/RitsuLibBuildInfo.cs:31`）。
- 宿主版本探测 API：`Sts2HostVersion.Numeric` / `ReleaseLabel`（`RL/STS2RitsuLib/Compat/Sts2HostVersion.cs:146,152`）；`Compat/Sts2ApiCapabilityGate.cs`、`Sts2ApiFeatureThresholds.cs` 预留集中式能力门控。
- 游戏侧同时约束：manifest `min_game_version` 高于当前游戏 → mod 直接 Fail；下游 manifest 的 `dependencies[].min_version` 高于已装 RitsuLib → Fail（`GAME/.../ModManager.cs:805-925`）。

## 5. 最小接入步骤（BlindSpire）

1. **manifest**（`BlindSpire.json`）：`id: "BlindSpire"`、`has_dll: true`、`has_pck` 按需、`affects_gameplay: true`（加玩法内容时）、`min_game_version: "0.107.1"`（按需）、依赖 `[{"id": "STS2-RitsuLib", "min_version": "0.6.2"}]`（若也依赖 BaseLib，两者并列）。dll 文件名必须为 `BlindSpire.dll`（游戏按 `<modId>.dll` 查找，`GAME/.../ModManager.cs:953`）。
2. **csproj**：按 §4.3 片段引用 RitsuLib + 游戏程序集 + BaseLib；访问游戏 internal 类型加 `[assembly: IgnoresAccessChecksTo("sts2")]`；有 Godot 脚本类型时按 BaseLib 笔记的方式声明 `AssemblyHasScripts`/`ScriptPath`。
3. **入口**：`[ModInitializer("Initialize")] public static class BlindSpireMain`；在 `Initialize()`（RitsuLib 保证已就绪）里：
   ```csharp
   var patcher = RitsuLibFramework.CreatePatcher(ModId, "main");
   patcher.RegisterPatches<BlindSpirePatches>();                 // IModPatches
   RitsuLibFramework.ApplyRequiredPatcher(patcher, () => { /* 标记本 mod 失效 */ });

   RitsuLibFramework.CreateContentPack(ModId)
       .Card<BlindSpireCardPool, BlindSpireStrike>()             // 或改用 [RegisterCard] 特性
       .Apply();

   RitsuLibFramework.RegisterModSettingsReflectionProvider<BlindSpireSettings>(); // [ModSettingsPage] 类
   var loc = RitsuLibFramework.CreateModLocalization(ModId, "BlindSpire");
   RitsuLibFramework.RegisterI18NLocTableBridge(ModId, loc);
   ```
4. **打包**：只带 `BlindSpire.dll`（+ 可选 pck/json）与自有关卡资源；**不要把 RitsuLib 的 dll 复制进自己目录**（props 的 `Private=False` 已避免编译期复制）；要求玩家安装完整 RitsuLib（workshop 或本地 mods 目录，保持 `compat/`+`shared/`+`ritsulib-variants.manifest`+`assets.zip` 原样）。
5. **注意**：内容/设置注册要在 `ModelDb.Init`（内容冻结）之前完成；`[ModInitializer]` 缺失时游戏会退回 `Harmony.PatchAll`，所有 RitsuLib API 都不会被初始化；每局/每档案数据分别用 `RunSavedDataStore`/`ModDataStore(Profile)`，跨存档全局配置用 `SaveScope.Global`。

## 6. 索引（最重要文件）

| 文件 | 作用 |
|---|---|
| `PKG/STS2-RitsuLib.dll`（Loader，ILSpy） | 变体选择/哈希校验/模块装载/反射启动 Runtime |
| `PKG/RitsuLib.References.props` | 下游 csproj 引用集成（`Private=False`） |
| `PKG/ritsulib-variants.manifest` | schema 2 的 shared/变体清单 + sha256 |
| `RL/STS2RitsuLib/RitsuLibFramework.cs` | 总入口：初始化、生命周期订阅、所有注册/工具 API |
| `RL/STS2RitsuLib/Content/ModContentRegistry.cs` | 内容注册表（卡/遗物/药水/角色/池/资产） |
| `RL/STS2RitsuLib/Scaffolding/Content/ModContentPackBuilder.cs` | 流式内容包（`Apply()` 延迟注册） |
| `RL/STS2RitsuLib/Interop/ModTypeDiscoveryHub.cs` + `Interop/Patches/ModTypeDiscoveryPatch.cs` | 自动注册/互操作管线与触发点 |
| `RL/STS2RitsuLib/Patching/Core/ModPatcher.cs` + `Patching/Models/IPatchMethod.cs` | 补丁管线与声明式补丁模型 |
| `RL/STS2RitsuLib/Lifecycle/Patches/ModelRegistryLifecyclePatch.cs` | 内容冻结与模型生命周期事件 |
| `SH/STS2RitsuLib/Data/ModDataStore.cs` | 全局/档案 JSON 持久化 |
| `RL/STS2RitsuLib/RunData/RunSavedDataStore.cs` | 局内存档槽 |
| `SH/STS2RitsuLib/Utils/I18N.cs` + `RL/STS2RitsuLib/Localization/I18NLocTableBridge.cs` | 本地化与游戏 LocTable 桥 |
| `ST/STS2RitsuLib/Settings/ModSettingsPageBuilder.cs` + `ModSettingsSectionBuilder.cs` | 设置 UI 构建 |
| `ST/STS2RitsuLib/Settings/RuntimeReflectionMirrorSource.cs` | `[ModSettingsPage]` 反射页发现/注册 |
| `ST/STS2RitsuLib/Settings/BaseLibMirrorSource.cs` | BaseLib 配置镜像 |
| `SH/STS2RitsuLib/` 下 `*Event.cs` | 全部框架生命周期事件定义 |
| `RL/STS2RitsuLib/*Event.cs` + `RL/STS2RitsuLib/Lifecycle/Patches/` | 战斗/卡牌等游戏事件定义与发布补丁 |
| `GAME/MegaCrit/sts2/Core/Modding/ModManager.cs` | 游戏 mod 加载/依赖排序/初始化器调用 |

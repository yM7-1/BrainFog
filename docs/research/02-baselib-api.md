# BaseLib v3.4.7 研读笔记

> 对象：Slay the Spire 2 mod 依赖库 **BaseLib**（作者 Alchyr）
> 资料与简称（引用格式 `简称/文件:行`）：
> - `BL/` = `.refs/baselib-v3.4.7/src/Baselib`（反编译只读参考）
> - `QR/` = `.refs/quickrestart-example/src`
> - `GAME/` = `.refs/sts2-v0.111.0/src`
> - 发布物 = `D:/Steam/steamapps/workshop/content/2868840/3737335127/BaseLib/`
>
> 本笔记基于 BaseLib **v3.4.7**（`BaseLib.json`：`"version": "v3.4.7"`，`min_game_version: 0.107.1`，`has_pck/has_dll: true`，`affects_gameplay: false`）。

## 1. 定位与能力清单

BaseLib 是面向 mod 作者的通用工具库：把「注册自定义内容、配置界面、本地化、扩展存档、UI 挂点、Harmony 补丁辅助」等重复工作封装成基类与工具。它自身不改玩法（`affects_gameplay: false`），但为依赖它的 mod 打开相关开关（见 §3/§5）。

按目录列出能力（BL 下共 493 个 .cs）：

| 模块（目录） | 能力 |
|---|---|
| `Abstracts/` | 内容基类：卡/能力/遗物/药水/宝珠/角色/事件/遭遇/怪物/ACT/远古/池/徽章/卡牌修饰器/自定义资源等，构造即注册 |
| `Cards/` | `BaseLibKeywords`（如 `Purge`，`BL/Cards/BaseLibKeywords.cs:10`）与卡面变量（`Cards/Variables/`：计算伤害/格挡、Refund、Persist、Scry 等） |
| `Config/` | 静态属性驱动的 mod 配置系统 + 自动设置界面（`SimpleModConfig`、`NConfig*` 控件、`[Config*]` 特性） |
| `Audio/` | `ModAudio`/`ModSound`：全局或战斗内播放自定义音频，音量分类 |
| `BaseLibScenes/` | 自定义 Godot 节点：日志窗口、横向滚动容器、额外费用/资源显示、奖励高亮等 |
| `Commands/` | 战斗命令：`ScryCmd`、多牌堆选牌 `MultiPileCardSelect` |
| `Common/Rewards/` | 现成奖励：卡牌升级/变形奖励、随机升级、LinkedRewardSet |
| `ConsoleCommands/` | 调试控制台命令，如 `showlog` |
| `Diagnostics/` | Harmony 补丁转储（HarmonyPatchDump*） |
| `Extensions/` | 大量扩展方法（ModelDb、Card、Power、Relay、Harmony`TryPatchAll` 等） |
| `Hooks/` | 新 Hook 接口与聚合入口 `BaseLibHooks`（Scry、额外资源、最大手牌、治疗量等） |
| `Monsters/` | `MoveBuilder`：用构建器写怪物意图/行动 |
| `Patches/` | 内建 Harmony 补丁（内容/兼容/修复/UI/本地化/存档/网络），无需手动调用 |
| `Utils/` | `SpireField`/`SavedSpireField`、`HookUtils`、`CommonActions`/`MonsterActions`、`WeightedList`、`WhatMod`、`ModInterop`、patch 辅助（`InstructionPatcher`/`AsyncMethodCall`）等 |

## 2. 核心入口类与常用 API

### 2.1 mod 入口与初始化

```csharp
[ModInitializer("Initialize")]                 // BL/BaseLibMain.cs:21
public static class BaseLibMain {
    public const string ModId = "BaseLib";     // :27
    public static Logger Logger { get; }        // :34
    public static Harmony MainHarmony { get; }  // :37
    public static void Initialize();            // :51
}
```
- 自己的 mod 也用同样方式声明：QR 的入口 `[ModInitializer("Initialize")]`（`QR/QuickRestart/QuickRestartCode/MainFile.cs:13`），初始化方法必须 **static**（`GAME/MegaCrit/sts2/Core/Modding/ModManager.cs:1085-1101`）。
- BaseLib 初始化时：注册自身配置、`ScriptManagerBridge.LookupScriptsInAssembly`、执行内建补丁、`MainHarmony.TryPatchAll`（`BL/BaseLibMain.cs:51-79`）。

### 2.2 内容注册（构造即注册）

所有基类实现空标记接口 `ICustomModel`（`BL/Abstracts/ICustomModel.cs:3`）。**在构造函数里调用 `CustomContentDictionary`**，因此只需继承并在自己的构造中 `base(...)`：

| 内容 | 基类（file:line） | 注册点 |
|---|---|---|
| 卡 | `CustomCardModel`（`BL/Abstracts/CustomCardModel.cs:18`，ctor `:251`） | `AddModel`（`:259`） |
| 遗物 | `CustomRelicModel`（`BL/Abstracts/CustomRelicModel.cs:7`，ctor `:11`） | `AddModel` |
| 药水 | `CustomPotionModel`（`BL/Abstracts/CustomPotionModel.cs:13`，ctor `:186`） | `AddModel` |
| 能力/Power | `CustomPowerModel`（`BL/Abstracts/CustomPowerModel.cs:8`）、`CustomTemporaryPowerModel` | 随模型注册 |
| 角色 | `CustomCharacterModel`（`BL/Abstracts/CustomCharacterModel.cs:16`，ctor `:84-86`） | `AddCharacter`（`CustomContentDictionary.cs:122`） |
| 事件 | `CustomEventModel`（`BL/Abstracts/CustomEventModel.cs:14`，ctor `:30`） | `AddEvent`（按 Acts 分共享/章节事件） |
| 遭遇 | `CustomEncounterModel`（`BL/Abstracts/CustomEncounterModel.cs:11`） | `AddEncounter`（`:123`） |
| 怪物 | `CustomMonsterModel`（`BL/Abstracts/CustomMonsterModel.cs:11`） | 随场景转换注册 |
| ACT | `CustomActModel`（`BL/Abstracts/CustomActModel.cs:24`） | `AddAct`（`:284`） |
| 远古 | `CustomAncientModel`（`BL/Abstracts/CustomAncientModel.cs:14`） | `AddAncient`（`:54`） |
| 宝珠 | `CustomOrbModel`（`BL/Abstracts/CustomOrbModel.cs:7`，ctor `:36`） | 随模型注册 |
| 卡池/遗物池/药水池 | `CustomCardPoolModel`/`CustomRelicPoolModel`/`CustomPotionPoolModel` | `IsShared` 控制共享 |
| 卡牌修饰器 | `CardModifier`（`BL/Abstracts/CardModifier.cs:29`） | `AddModifier`（`:219`） |
| 自定义资源 | `CustomResource` / `BasicCustomResource`（`BL/Abstracts/BasicCustomResource.cs:16`） | 反射自动注册（`BL/Patches/PostModInitPatch.cs:89-112`） |

- 卡/遗物/药水必须标注池特性 `[Pool(typeof(...))]`，否则 `AddModel` 直接抛异常（`BL/Patches/Content/CustomContentDictionary.cs:60-70`，特性定义 `BL/Utils/PoolAttribute.cs:6`）。
- 卡牌本地化/肖像等通过覆写 `CustomPortraitPath`、`Localization`、`CustomFrame` 等虚属性（`CustomCardModel.cs:205-249`）。
- 注册时序：类型在 `ModelDb.InitIds` 时汇总（`CustomContentDictionary.cs:18`），场景转换在 `ModelDb.Preload` 后处理（`BL/Patches/PostModInitPatch.cs:23-37`），早期后置初始化挂在 `LocManager.Initialize`（`:51`）。

### 2.3 配置（含自动设置 UI）

```csharp
ModConfigRegistry.Register("QuickRestart", new Config());   // QR/MainFile.cs:39
```
- `Register` 仅在「有设置或有按钮」时生效（`BL/Config/ModConfigRegistry.cs:10-18`）。
- `SimpleModConfig` 自动从**静态属性**生成 UI（`BL/Config/SimpleModConfig.cs:297`、`GenerateOptionsForAllProperties` `:541`）：bool→勾选、string→输入框、数值→滑块、enum→下拉、`Color`→取色器（`:445-476`）。
- 特性：`[ConfigSlider(min,max,step)]`、`[SliderRange]`、`[ConfigSection]`、`[ConfigButton]`、`[ConfigColorPicker]`、`[ConfigTextInput]`、`[ConfigHideInUI]`、`[ConfigVisibleIf]`、`[ConfigHoverTip]`、`[ConfigIgnore]`、`[ConfigIgnoreRestoreDefaults]`（均在 `BL/Config/Config*Attribute.cs`）。
- 配置文件落盘：`OS.GetUserDataDir()/mod_configs/<命名空间>.cfg`，JSON 字符串字典；读写带防抖/锁/损坏备份（`ModConfig.cs:96-116`、`:218-262`、`:386-405`）。
- 常用 API：`Save()`/`SaveDebounced()`/`Load()`（`ModConfig.cs:213-344`）、事件 `ConfigChanged`、`OnConfigReloaded`（`:78-85`）、`GetLabelText`（`:443`，走 `settings_ui` 本地化表）。
- 坑：配置类必须在命名空间内或给构造函数显式文件名，否则抛异常（`:101-106`）；非 static 属性只警告不生效（`:150-157`）。

### 2.4 本地化

- 内容基类实现 `ILocalizationProvider`：`LocTable` + `List<(string,string)>? Localization`（`BL/Abstracts/ILocalizationProvider.cs:5-8`），例如卡牌 `Localization`（`CustomCardModel.cs:249`）。
- 追加自定义 loc 表：`CustomLocTableManager.Register("xxx")`（`BL/Utils/CustomLocTableManager.cs:18`）。
- SimpleLoc 语法（`*金色*`、`$蓝色$`、`!diff!`、`[E]` 能量图标等）需为 mod 调 `SimpleLoc.EnableSimpleLoc(modId)`（`BL/Patches/Localization/SimpleLoc.cs:60`）。
- 模型描述覆写见 `BL/Patches/Localization/ModelLocPatch.cs:77`、`DefaultLoc.cs`、`ExtraTooltips.cs`。

### 2.5 UI 挂点与自定义节点

- 战斗内加 UI：`ExtraCombatUi.RegisterCombatUiElement(...)`（`BL/Patches/UI/ExtraCombatUi.cs:122`，位置枚举 `:18`）。
- 卡牌上挂控件：`ExtraCardUi.RegisterCreateCardUiElement(...)`（`BL/Patches/UI/ExtraCardUi.cs:111`/`:123`）。
- 现成节点（`BL/BaseLibScenes/`）：`NLogWindow`、`NHorizontalScrollContainer`、`NAdditionalCostDisplay`、`NAdditionalResourceDisplay`、`NCustomLinkedRewardSet`、`NRewardHighlight`。
- 调试：游戏内控制台 `showlog` 打开日志窗口（`BL/ConsoleCommands/OpenLogWindow.cs:13-15`；窗口节点 `BL/BaseLibScenes/NLogWindow.cs:18`），可配置启动即开、错误即开（`BL/Config/BaseLibConfig.cs`）。

### 2.6 数据/扩展工具

- 附加字段（不污染游戏类）：`SpireField<TKey,TVal>`（`BL/Utils/SpireField.cs:7`，索引器 `:17`）与其可存档版 `SavedSpireField`（`BL/Utils/SavedSpireField.cs:13`，需在 `[SavedProperty]` 体系下使用）。
- Hook：`HookUtils.Dispatch/Aggregate/Modify/All`（`BL/Utils/HookUtils.cs:14+`）遍历战斗 Hook 监听者；BaseLib 新接口见 `BL/Hooks/*`（`IAfterScryed`、`IMaxHandSizeModifier`、`IHealAmountModifier`、`IAfterSpendResource`、`IModifyResourceCostInCombat` 等），聚合入口 `BL/Hooks/BaseLibHooks.cs:13+`。
- 常用动作封装：`CommonActions.CardAttack/CardBlock/Apply<T>/SelectCards/Draw/GenerateCards`（`BL/Utils/CommonActions.cs:23+`）；怪物版 `MonsterActions`（`BL/Utils/MonsterActions.cs:10+`）；怪物 AI `MoveBuilder`（`BL/Monsters/MoveBuilder.cs:19`）。
- 音频：`new ModSound("res://...ogg")` + `ModAudio.PlaySound/PlaySoundInRun`（`BL/Audio/ModSound.cs`、`BL/Audio/ModAudio.cs:189-201`）。
- 随机：`WeightedList<T>`（`BL/Utils/WeightedList.cs:10`）。
- 跨 mod 双向互操作：`[ModInterop("OtherModId")]` + `[InteropTarget]` 生成代理（`BL/Utils/ModInterop/ModInteropAttribute.cs:6`，处理器 `BL/Patches/Features/ModInterop.cs:46`）。
- Patch 辅助：`InstructionPatcher`（`BL/Utils/Patching/InstructionPatcher.cs:11`）、`AsyncMethodCall`（`BL/Utils/Patching/AsyncMethodCall.cs:15`）、`PatchAsyncMoveNext`（`BL/Extensions/HarmonyExtensions.cs:51`）、`TryPatchAll`（`:12`）。
- 归属查询：`WhatMod.FindMod(Type)`（`BL/Utils/WhatMod.cs:91`），用于「这个模型来自哪个 mod」提示。

## 3. 依赖与初始化方式

### 3.1 manifest（`<ModId>.json`）

游戏 `ModManifest` 字段（`GAME/MegaCrit/sts2/Core/Modding/ModManifest.cs:18-46`）：`id/name/author/description/version/has_pck/has_dll/dependencies[]/affects_gameplay/min_game_version`。

```jsonc
// QuickRestart.json（workshop 发布物，QR 示例的真实 manifest）
{
  "id": "QuickRestart", "version": "v2.0.0",
  "min_game_version": "0.107.0",
  "has_pck": true, "has_dll": true,
  "dependencies": [ { "id": "BaseLib", "min_version": "3.4.5" } ],
  "affects_gameplay": false
}
```
- 依赖是对象数组 `{id,min_version}`（`GAME/.../ModDependency.cs`）；旧式纯字符串会告警（`ModManifest.cs:54-73`）。
- 加载顺序：按依赖拓扑排序，依赖先加载（`GAME/.../ModManager.cs:331-342`）。
- 校验：`min_game_version` 高于当前游戏版本 → mod 直接 Fail（`:805-867`）；依赖缺失或版本低于 `min_version` → Fail（`:871-925`）。
- 重要：BaseLib 仅在「某依赖方 manifest 同时声明 `affects_gameplay: true` 且依赖 BaseLib」时才开启玩法修改相关能力（如 `CardModifier.RegisterSave`，`BL/Patches/PostModInitPatch.cs:62-75`）。BrainFog 若要注册玩法内容，manifest 建议 `affects_gameplay: true`。

### 3.2 csproj 引用

- 反编译后的 QR csproj（`QR/QuickRestart.csproj`）用的是裸引用：
  ```xml
  <Reference Include="BaseLib" /> <Reference Include="sts2" />
  <Reference Include="GodotSharp" /> <Reference Include="0Harmony" />
  ```
  `TargetFramework` = `netcoreapp9.0`，`LangVersion`=11。
- **BaseLib 自身不发布任何 .props**（workshop 目录只有 dll/json/pck 三个文件）。对比：RitsuLib 随包提供 `RitsuLib.References.props`，用 `HintPath` + `Private="False"` 指向各版本兼容程序集（`.../3747602295/RitsuLib.References.props`）。
- 因此 BrainFog 需要自备 `Directory.Build.props`（或逐条 HintPath）：指向 `D:/Steam/steamapps/workshop/content/2868840/3737335127/BaseLib/BaseLib.dll`（或开发机上的 `mods/BaseLib/BaseLib.dll`），并设 `Private="False"` 避免把依赖 dll 复制进输出。`sts2`/`GodotSharp`/`0Harmony` 可从 `D:/Steam/steamapps/common/Slay the Spire 2/data_sts2_windows_x86_64/` 取。
- 访问游戏 internal 类型需 `[assembly: IgnoresAccessChecksTo("sts2")]`（`QR/Properties/AssemblyInfo.cs`），并通过 `AssemblyHasScripts` 列出所有 Godot 脚本类型（同文件）。

### 3.3 pck 的作用与打包

- 游戏对有 `has_pck: true` 的 mod 调 `ProjectSettings.LoadResourcePack(<mod.path>/<id>.pck)`（`GAME/.../ModManager.cs:970-986`），随后才加载 dll 并执行 `[ModInitializer]`（`:992-1021`）。
- BaseLib 的 pck 内容：`BaseLib/scenes/LogWindow.scn`、导入纹理 `.ctex`（power 图标、configbutton 等）、以及 C# `[ScriptPath]` 脚本映射（如 `res://BaseLibScenes/NLogWindow.cs`）。QR 的 pck 同样含 `res://QuickRestartCode/*.cs` 映射与 `mod_image.png`。
- 结论：需要自定义场景/贴图/[ScriptPath] 节点时，用 Godot 编辑器把 `res://` 资源导出为 `<ModId>.pck`，并在 `<ModId>.json` 置 `has_pck: true`；纯逻辑 mod 只发 dll 也可（`has_pck: false`）。

## 4. QuickRestart 示例剖析

功能：暂停菜单加「重开本房间」按钮 + 长按快捷键重开 + 配置界面。依赖 BaseLib ≥3.4.5。

1. **入口**：`MainFile` 同时是 `Node` + `[ModInitializer("Initialize")]` + `[ScriptPath("res://QuickRestartCode/MainFile.cs")]`（`QR/.../MainFile.cs:13-15`）。`Initialize()` 里注册配置并 `new Harmony("QuickRestart").PatchAll()`（`:35-42`）。
2. **配置**：`Config : SimpleModConfig`，静态属性 `HoldDur`（`[ConfigSlider(500,3000,100, Format="{0}ms")]`，`:11-12`）、`ShowIndicator`（bool）、`RestartKey`（`Key`，因 Key 类型不支持自动 UI 而 `[ConfigHideInUI]`，`:18-19`）。覆写 `SetupConfigUI`：先自动生成所有属性行，再手工加分隔线、自定义 `NConfigKeybind` 行与「恢复默认」按钮（`:22-38`）。
3. **按键监听**：Harmony patch 游戏 `NGame._Input`（`QR/.../Keybind.cs:10-11`），长按计时到 `Config.HoldDur` 后触发重开。
4. **暂停菜单按钮**：`[HarmonyPatch(typeof(NPauseMenu), "_Ready")]` Postfix 里复制 `settingsButton` 生成新按钮，插到 giveUp 按钮前，标签取 `LocString("gameplay_ui","QR_RESTART_BUTTON")`，连接 `Released` 信号（`QR/.../Restarter.cs:27-84`）。打开暂停菜单时按单机/是否结束禁用按钮（`:87-107`），关菜单时置灰（`:109-120`）。
5. **重开逻辑**：`RestartRoomAsync()` 校验单机且存在存档后，`RunManager.CleanUp` → `SaveManager.LoadRunSave` → `RunState.FromSerializable` → `SetUpSavedSingleplayer` → 重载房间（`Restarter.cs:142-208`）。
6. **长按进度环**：`HoldProgressIndicator : TextureProgressBar`，`[ScriptPath]` 注册；`EnsureCreated()` 把自己 `AddChild` 到 `NGame.Instance`（`:57-73`），`_Process` 里绘制鼠标旁圆环（`:107-146`）。
7. **BaseLib 在本例中的作用**：`ModConfigRegistry.Register`（`MainFile.cs:39`）把配置接入游戏设置界面；`SimpleModConfig` 提供配置 UI/持久化/控件基类；`NConfigButton` 作为自定义键位控件基类（`NConfigKeybind : NConfigButton`，`NConfigKeybind.cs:24,54`）。其余功能全走原生 Harmony/游戏 API——说明 BaseLib 的配置与 UI 层是可选增强，不强制绑定。

## 5. 兼容与版本注意

- **版本窗口**：BaseLib 3.4.7 声明 `min_game_version: 0.107.1`，游戏本体 0.111.0（`release_info.json`）满足；QuickRestart 用 `min_game_version: 0.107.0` + `BaseLib ≥3.4.5`。游戏版本若是低于 mod 的 min_game_version，mod 会被直接判 Fail（`GAME/.../ModManager.cs:858-866`）。
- BaseLib 内建 main/beta 双分支兼容层 `BetaMainCompatibility`（`BL/Utils/BetaMainCompatibility.cs:24+`）；依赖解析也在这里（`:193 HasDependency`，用于 §3.1 的玩法开关判断）。
- 兼容性补丁目录 `BL/Patches/Compatibility/`：`MissingLocPatch`（缺失本地化兜底）、`OptionalFormNodePatch`、`UnknownCharacterPatches`（未知角色容错）等；联机同步的自定义类型注册见 `BL/Patches/Networking/GetCustomTypes.cs`。
- 升级游戏后若 BaseLib 报补丁失败，优先看日志窗口（`showlog`）与 `Diagnostics/HarmonyPatchDump*` 输出；BaseLib 的 patch dump 入口挂在主菜单（`BL/Patches/Utils/HarmonyPatchDumpMainMenuPatch.cs`）。
- 常见坑清单：
  1. 忘记 `[ModInitializer]` → 游戏退回自动 `Harmony.PatchAll`，配置/自定义注册代码不会执行（`GAME/.../ModManager.cs:1007-1013`）。
  2. 配置属性写成实例属性 → 被忽略（`ModConfig.cs:150-157`）；配置类不在命名空间且未传 filename → 启动抛异常（`:101-106`）。
  3. 注册内容忘了 `[Pool(...)]` → `AddModel` 抛异常（`CustomContentDictionary.cs:64`）。
  4. 构造函数抛异常会吞掉所有注册（注册发生在 ctor），BaseLib 对部分反射注册有 try/catch 保护，但基类 ctor 异常会直接让类型初始化失败。
  5. 玩法类 mod 未声明 `affects_gameplay: true` → BaseLib 不会开启 `CanModifyGameplay` 相关扩展（`PostModInitPatch.cs:62-75`）。
  6. 发布时必须 dll 与 `<ModId>.json` 同名同目录；`has_pck: true` 但缺 pck 文件会在加载期报错。
  7. 自定义模型若参与联机/存档，需按 BaseLib 的扩展存档（`Patches/Saves/*`、`SavedSpireField`）与网络类型（`Patches/Networking/*`）规范实现，否则读档/多人不同步。

## 6. 索引（最重要文件）

| 文件 | 作用 |
|---|---|
| `BL/BaseLibMain.cs` | 入口、Logger、MainHarmony、内建补丁装载 |
| `BL/Patches/PostModInitPatch.cs` | 早/晚期 mod 后初始化、扫描 mod 类型、玩法开关 |
| `BL/Patches/Content/CustomContentDictionary.cs` | 内容注册表与模型入池 |
| `BL/Abstracts/CustomCardModel.cs` | 卡牌基类（含变量/肖像/本地化钩子） |
| `BL/Abstracts/CustomCharacterModel.cs` | 角色基类（视觉/音效/选项） |
| `BL/Config/ModConfig.cs` / `SimpleModConfig.cs` | 配置存取与自动 UI |
| `BL/Patches/UI/ExtraCombatUi.cs` / `ExtraCardUi.cs` | 战斗 UI / 卡面 UI 挂点 |
| `BL/Utils/CommonActions.cs` / `MonsterActions.cs` / `MoveBuilder.cs` | 卡与怪物行为封装 |
| `BL/Utils/SpireField.cs` / `SavedSpireField.cs` | 扩展字段/可存档字段 |
| `BL/Utils/HookUtils.cs` / `BL/Hooks/*` | 新 Hook 接口与派发 |
| `BL/Audio/ModAudio.cs` / `ModSound.cs` | 自定义音频 |
| `BL/Utils/Patching/*` / `BL/Extensions/HarmonyExtensions.cs` | IL 补丁辅助 |
| `BL/Patches/Localization/SimpleLoc.cs` | 简化本地化语法 |
| `D:/Steam/.../BaseLib/BaseLib.json` | 发布版 manifest（版本/依赖声明参考） |
| `QR/QuickRestart/QuickRestartCode/MainFile.cs` / `Config.cs` / `Restarter.cs` | BaseLib 配置 + 原生 Harmony 的完整小示例 |

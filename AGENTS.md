# AGENTS.md — BrainFog 协作备忘

## 发布规则（2026-09-21 用户定）

- 日常改动：只 `commit` + `push` 到 GitHub（origin = `yM7-1/BrainFog`）
- **不要自动上传/更新创意工坊**：任何工坊更新前必须先询问用户，得到确认后再执行
- 上传流程见 `steam-workshop-upload` skill；命令：
  `/mnt/d/steamcmd/steamcmd.exe +login lwh4646 +workshop_build_item "D:\\0_git\\BrainFog\\packaging\\workshop\\BrainFog_workshop.vdf" +quit`
- 工坊描述 UTF-8 字节数上限约 8000：超限提交报 `Failed to update workshop item (Invalid Parameter)`；
  接近上限时优先精简末尾的乱码示范段（`packaging/workshop/description.md` 尾部）
- 本地测试副本 `mods/BrainFog` 与工坊订阅冲突（DUPLICATE_ID）：部署本地前确认用户已退订，
  或让用户直接用订阅版验收
- 三处版本号需同步：`BrainFog.csproj`、`BrainFog.json`、`packaging/workshop/content/BrainFog/BrainFog.json`；
  工坊内容包 = 构建后的 `BrainFog.dll`（拷入 `packaging/workshop/content/BrainFog/`）

## 常用命令

- 引用程序集（首次/换版本）：`bash tools/fetch-refs.sh`（NuGet `Book.StS2.RefLib` → `.refs/sts2-refs/`，不入库）
- 一键验证：`bash tools/check.sh`（installed 游戏构建+审计；min 0.107.1 编译；0.107.1/0.111.0 补丁目标审计）
- 构建默认对 `.refs/sts2-refs/0.107.1`（最低支持版本）编译；要显式对本地游戏编译加 `-p:Sts2UseLocalGame=true`
- 本地部署：`dotnet build BrainFog.csproj -c Release -p:CopyModOnBuild=true -p:SteamRoot=/mnt/d/Steam`
- 目标环境：WSL；Windows 互操作若失效，注册 `echo ':WSLInterop:M::MZ::/init:PF' > /proc/sys/fs/binfmt_misc/register`
- steamcommunity 页面验证：Windows `curl.exe -x http://127.0.0.1:7897`（WSL 直连不通）

## 项目当前状态（2026-09-25，v0.3.7）

- 目标游戏 **STS2 v0.107.1 – v0.111.0**（正式服 + 测试服共用同一 DLL）+ **RitsuLib 0.6.2**；仅单人（联机自动禁用）
- **测试**：`tools/check.sh` 全绿（installed 241 项；min 版本编译 + 0.107.1/0.111.0 双版本审计 243 项）
- **代码/文档**：GitHub `main` 已同步；工作区干净
- **工坊**：线上版本 **0.3.6**（条目 3804204184，最后更新 2026-09-21）；v0.3.7 未上传，按「发布规则」先询问用户
- **功能现状**（0.3.5 已实机验收；0.3.6/0.3.7 待实机确认）：
  - 核心：乱码百分比 0–100% + 固定/随机盐、卡牌黑雾与四记忆模式、记忆消逝（原版删卡动画）、
    失忆提醒变暗、初始卡组揭示、坏记性 n=2/新卡先认得
  - 感知：敌人模型可见、意图三档、地图全路线、显示遗物（全场景）、数值可读、失忆模式
  - 地图：画图功能保持原版（0.3.3）；地图选项开启时恢复阶段 Boss 图标与图例（0.3.4）；
    Boss/图例悬停描述跟随文字乱码百分比（0.3.5）
  - 修改器：三模块面板（主标题可用/可拖动/收起/缩进/重置）+「当前设置为默认」（0.3.2 默认值调整）
  - 0.3.6：移除「卡牌计数器」选项与右上角排行榜（用户决定；坏记性"新卡先认得"保留）
  - 0.3.7：「关闭脑雾尖塔」开关：勾选后 mod 完全停止影响游戏并即时恢复原版呈现，配置跨重进保留；
    关闭期间面板仅保留该开关（可随时恢复），联机自动禁用不受影响
- **待办与限制**：`docs/IMPLEMENTATION-MAP.md` §2（已知限制）与 §3（实机验收清单）；
  冻结规格：`docs/spec/SPEC-consolidated.md`；版本窗口与升级流程：`docs/UPGRADE.md`
- **版本历史**：`CHANGELOG.md`（0.3.7 / 0.3.6 / 0.3.5 / 0.3.4 / 0.3.3 / 0.3.2 / 0.3.1 / 0.3.0 …）

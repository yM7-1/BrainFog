# 游戏版本升级指南

BlindSpire 只支持**固定游戏版本**（当前 v0.111.0）。游戏升级后按以下步骤适配：

1. **更新引用版本**
   - `BlindSpire.json` 的 `min_game_version` 改为新版本；
   - `BlindSpire.csproj` 的 `RitsuLibReferenceTarget` 改为新版本（RitsuLib 需提供对应 compat 变体）；
   - 检查 `SteamRoot` 下新版本 `data_sts2_windows_x86_64/sts2.dll` 存在。
2. **重建并跑审计**
   ```bash
   bash tools/check.sh
   ```
   重点看 `PatchTargetAuditTests`：任何缺失的补丁目标（类型/字段/方法）都会列在失败信息里。
3. **修复漂移**
   - 审计目标缺失 → 更新代码中的补丁目标（Harmony 字符串方法名、私有字段名、类型）；
   - 编译失败 → 游戏 API 变更，按新反编译源码调整（重建反编译见 `docs/research/00-overview.md`）；
   - 视觉/行为回归 → 对照 `docs/IMPLEMENTATION-MAP.md` §3 验收清单人工过一遍。
4. **实机验证**：跑 `docs/IMPLEMENTATION-MAP.md` §3 清单，重点：卡牌黑雾、读档重绑、地图迷雾、事件模糊。

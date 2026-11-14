---
name: brainfog-workshop-upload
description: 更新/上传《脑雾尖塔 BrainFog》到 Steam 创意工坊（STS2 app 2868840，条目 3804204184）。Use when 用户提到 脑雾尖塔/BrainFog/工坊更新/上传创意工坊/workshop_build_item/3804204184，或在 D:\0_git\BrainFog（WSL /mnt/d/0_git/BrainFog）下做版本发布、打包、描述/changenote 同步。
---

# 脑雾尖塔 · Steam 创意工坊上传

> 本项目专用的发布路径与流程。通用的 Steam 工坊规则见
> `C:\Users\Admin\.agents\skills\steam-workshop-upload\SKILL.md`（本机已有）。
> **发布纪律**：普通代码改动只 commit + push GitHub；任何工坊上传前必须先得到用户明确确认。

## 1. 固定路径（最重要）

| 用途 | Windows 路径 | WSL 路径 |
| --- | --- | --- |
| 项目根 | `D:\0_git\BrainFog` | `/mnt/d/0_git/BrainFog` |
| 工坊 VDF | `D:\0_git\BrainFog\packaging\workshop\BrainFog_workshop.vdf` | `/mnt/d/0_git/BrainFog/packaging/workshop/BrainFog_workshop.vdf` |
| 上传内容目录 | `D:\0_git\BrainFog\packaging\workshop\content` | `/mnt/d/0_git/BrainFog/packaging/workshop/content` |
| mod 内容 | `...\content\BrainFog\`（`BrainFog.dll` + `BrainFog.json`） | `.../content/BrainFog/` |
| 主图 | `D:\0_git\BrainFog\packaging\workshop\preview.jpg` | `/mnt/d/0_git/BrainFog/packaging/workshop/preview.jpg` |
| steamcmd | `D:\steamcmd\steamcmd.exe` | `/mnt/d/steamcmd/steamcmd.exe` |
| 游戏安装 | `D:\Steam\steamapps\common\Slay the Spire 2` | `/mnt/d/Steam/steamapps/common/Slay the Spire 2` |
| 游戏程序集 | `...\Slay the Spire 2\data_sts2_windows_x86_64` | `/mnt/d/Steam/steamapps/common/Slay the Spire 2/data_sts2_windows_x86_64` |
| 本地 mods（必须为空/无 BrainFog） | `D:\Steam\steamapps\common\Slay the Spire 2\mods` | `/mnt/d/Steam/steamapps/common/Slay the Spire 2/mods` |

- GitHub：`https://github.com/yM7-1/BrainFog`（origin；只推这里）
- Steam 上传账号：`lwh4646`（steamcmd 已缓存凭据，拥有 STS2）
- AppID：`2868840`
- 工坊条目：`3804204184`
- 游戏当前目标窗口：**v0.107.1 – v0.113.0**（发布前以 `BrainFog.json` 为准）

## 2. 上传前检查

1. **版本号三处同步**（外加 CHANGELOG 顶部）：
   - `BrainFog.csproj` `<Version>`
   - `BrainFog.json` `version`
   - `packaging/workshop/content/BrainFog/BrainFog.json` `version`
   - `CHANGELOG.md` 顶层 `## x.y.z`
   `ReleaseShapeTests.Version_IsConsistentAcrossReleaseFiles` 会校验。
2. **构建 + 测试 + 补丁目标审计**：
   ```bash
   cd /mnt/d/0_git/BrainFog
   bash tools/check.sh
   ```
   期望最后输出 installed 与双版本审计全绿（当前规模 277 / 279）。
3. **构建正式产物并拷入内容目录**：
   ```bash
   export DOTNET_ROOT=$HOME/.dotnet
   export PATH=$DOTNET_ROOT:$PATH
   cd /mnt/d/0_git/BrainFog
   dotnet build BrainFog.csproj -c Release -p:CopyModOnBuild=false \
     -p:Sts2UseLocalGame=true -p:SteamRoot=/mnt/d/Steam \
     -p:Sts2DataDir="/mnt/d/Steam/steamapps/common/Slay the Spire 2/data_sts2_windows_x86_64" \
     -p:RitsuLibReferenceTarget=0.113.0
   cp .godot/mono/temp/bin/Release/BrainFog.dll packaging/workshop/content/BrainFog/
   cp BrainFog.json packaging/workshop/content/BrainFog/
   ```
   注意：`tools/check.sh` 最后一次构建是最低版本 0.107.1 的同一份代码；如不放心，上传前按上面命令对 installed 0.113.0 再构建一次。
4. **确认本地 mods 无同名副本**：`mods/BrainFog` 必须不存在；工坊订阅版与本地 mod 同名会 `DUPLICATE_ID`。
5. **更新描述与 changenote**（下一节）。

## 3. 描述 / changenote 规则

工坊实际读取的是 **VDF 里的 `"description"` 字段**，不是单独的 `description.md`。
三处描述源要保持同步：

- `packaging/workshop/description.md`（Markdown 母本）
- `packaging/workshop/description_bbcode.txt`（BBCode 预览）
- `packaging/workshop/BrainFog_workshop.vdf`（真正上传的 `"description"`）

硬规则：

- 描述 UTF-8 **≤ 8000 字节**（接近上限先精简末尾乱码示范段）。
- VDF 里**禁止半角双引号 `"`**（steamcmd 不支持 `\"`，会静默截断）；中文引号用「」。
- VDF 必须有：`"description"` 的闭合引号、`"changenote"`、`"visibility" "0"`、最后的 `}`。
  手工裁剪描述时最容易把闭合引号和 `}` 一起删掉，上传前务必确认。
- `changenote` 写本版本实际改动（中文即可），不要写半角双引号。

快速检查（WSL）：

```bash
cd /mnt/d/0_git/BrainFog
python3 - <<'PY'
from pathlib import Path
s = Path('packaging/workshop/BrainFog_workshop.vdf').read_text(encoding='utf-8')
i = s.index('"description"'); j = s.index('\n\t"changenote"')
content = s[i:j].split('\t\t', 1)[1][1:-1]
print('description bytes:', len(content.encode()))
print('half-width quotes:', content.count('"'))
assert len(content.encode()) <= 8000
assert s.rstrip().endswith('}')
PY
```

## 4. 上传命令

WSL 直接调 Windows steamcmd（本机 WSL 互操作可用）：

```bash
/mnt/d/steamcmd/steamcmd.exe +login lwh4646 \
  +workshop_build_item 'D:\0_git\BrainFog\packaging\workshop\BrainFog_workshop.vdf' +quit
```

也可以运行项目里的：

```bat
D:\0_git\BrainFog\packaging\workshop\upload.bat
```

成功标志：

```text
Preparing content...
Uploading content...
Uploading preview image...
Committing update...Success.
```

首次上传会把新的 `publishedfileid` 回填到 VDF；本条目已有 ID，正常走更新。

## 5. 上传后验证

优先用 Steam Web API（不依赖 steamcommunity 网页/代理）：

```bash
curl.exe -sS --max-time 30 -X POST \
  "https://api.steampowered.com/ISteamRemoteStorage/GetPublishedFileDetails/v1/" \
  --data-urlencode "itemcount=1" \
  --data-urlencode "publishedfileids[0]=3804204184" \
  > /tmp/brainfog_workshop_api.json
```

用 Python 检查：

```python
import json
d = json.load(open('/tmp/brainfog_workshop_api.json'))['response']['publishedfiledetails'][0]
print(d['title'], d['file_size'], len(d['description'].encode()))
for k in ['0.4.0', 'Blitz', 'Dread', 'v0.113.0']:
    print(k, k in d['description'])
```

- `file_size` 应等于 `BrainFog.dll + BrainFog.json` 的字节和。
- `description` 字节数应与 VDF 中一致。
- 工坊页面：`https://steamcommunity.com/sharedfiles/filedetails/?id=3804204184`
- steamcommunity 在 WSL 可能不可达；若本机代理 `127.0.0.1:7897` 在运行，可用
  `curl.exe -x http://127.0.0.1:7897` 抓页面。代理未启动时不要卡在网页验证，Web API 已足够核对标题/描述/内容大小。

## 6. 常见坑

| 症状 | 处理 |
| --- | --- |
| 描述被截断 / 只存了一部分 | VDF 描述里有半角双引号；改用「」，重新上传 |
| `Invalid Parameter` / 工坊拒绝 | 描述超过约 8000 UTF-8 字节；精简末尾乱码示范段 |
| 上传成功但游戏里 mod 消失 | 本地 `mods/BrainFog` 与订阅版重复；移出游戏目录（不要只改名留在 mods 里） |
| steamcmd 卡在自更新 | 正常；超过 2 分钟重跑一次命令 |
| 主图被拒 | 只收 jpg/png/gif，≤1MB；当前 `preview.jpg` 为 1024×1024 |
| VDF 解析失败 | 检查描述闭合引号、`"changenote"`、`"visibility"`、最后 `}` |

## 7. 发布后收尾

1. `git add -A && git commit -m "chore(workshop): publish x.y.z description/changenote"`，再 `git push origin main`。
2. 更新 `AGENTS.md` 的「工坊线上版本」与日期。
3. 如需实机验收：Steam 客户端订阅自己的条目 → 启动游戏 → 模组列表来源应为创意工坊。
4. 不要把 `.refs/`、`.godot/`、`.local/`、游戏 mods 目录内容提交进仓库。

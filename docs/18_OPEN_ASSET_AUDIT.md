# 开源模型与动作资源审计（2026-09-02）

这份清单只收录许可证能覆盖实际模型、贴图或动画文件的来源。仓库公开、
可以下载或代码采用 MIT，并不自动代表其中搬运的第三方美术也能用于商业游戏。

## 结论

当前最值得接入的是 Quaternius UAL2 免费标准版的三段剑连击、冲斩和招架。它与
现有 UAL1 骨架一致，并在 2026 更新中修复挥剑肘部扭曲。KayKit 继续只提供格挡受力和左右闪身，
不替换已经制作的北齐人物外观。场景只选择通用小物，不引入欧洲教堂、六角地图或
完整地牢布局。

| 优先级 | 来源 | 已核验内容 | 许可证 | 对本项目的用途 | 处理决定 |
|---|---|---|---|---|---|
| A | [Quaternius Universal Animation Library 2](https://quaternius.com/packs/universalanimationlibrary2.html) | 官方免费 Standard 版；实际含 `Sword_Regular_A/B/C`、`Sword_Dash_RM`、`Sword_Block`，Unity FBX 与 UAL1 使用同一完整 Humanoid 骨架 | CC0 1.0；官方包内 `License.txt` | 三段轻击、独立冲斩和举刀招架 | 已接入五条；完整源 FBX 单文件压缩保存，运行时只复制五条 `.anim` |
| A | [KayKit Character Pack: Adventurers 1.0](https://github.com/KayKit-Game-Assets/KayKit-Character-Pack-Adventures-1.0) | 官方仓库；4 个免费角色、武器与附件；Knight GLB 实际含 76 个动画条目 | CC0 1.0；仓库内 `LICENSE.txt` 明确覆盖个人、教育和商业项目 | 单手刀攻击、格挡、受击、闪避的动作来源 | 下一批动画候选 |
| A | [KayKit Character Pack: Skeletons 1.0](https://github.com/KayKit-Game-Assets/KayKit-Character-Pack-Skeletons-1.0) | 官方仓库；4 个骷髅角色；Warrior GLB 实际含 95 个动画条目 | CC0 1.0 | 敌人动作补充、起身、唤醒、嘲讽和第二套死亡 | 只取动作，不把卡通骷髅当最终敌人 |
| B | [CMUMocap for Unity](https://github.com/keijiro/CMUMocap) | 6 个 Unity Humanoid FBX，单文件约 0.5–1 MB；包含武术步法、花式脚步和翻滚/翻腾序列 | 转换仓库指向 [CMU Mocap 条款](https://mocap.cs.cmu.edu/)：可嵌入商业产品，不可直接转售数据；应保留来源说明 | 敌人步法、闪身和动作参考 | 先做隔离预览；不直接替换持刀攻击 |
| B | [Microsoft Rocketbox](https://github.com/microsoft/Microsoft-Rocketbox) | 微软官方仓库；115 个绑定人物、417 个动画文件；大量行走、跑步、转身、蹲伏和表演动作 | MIT；仓库在 2020 年更新为 MIT | 普通人群、受伤行走、转身和剧情动作参考 | 体量过大且服装现代，只按文件取用 |
| B | [KayKit Dungeon Remastered 1.0](https://github.com/KayKit-Game-Assets/KayKit-Dungeon-Remastered-1.0) | 官方仓库；203 个 FBX；墙、破墙、地板、楼梯、木架、桌椅、木桶和箱子多为几十 KB | CC0 1.0 | 远景或角落里的通用木石杂物 | 只挑通用小物并重新配本项目材质 |
| C | [Unity Standard Assets Characters](https://github.com/Unity-Technologies/Standard-Assets-Characters) | Unity 官方旧角色、移动和动画示例 | Unity Companion License，只能用于依赖 Unity 的项目 | Animator、移动与脚步实现参考 | 目标版本是 Unity 2019.3 beta，不整包导入 Unity 6 工程 |

## 公开人物与服装候选

| 来源 | 许可证与实查结果 | 决定 |
|---|---|---|
| [Quaternius Ultimate Modular Men](https://quaternius.com/packs/ultimatemodularcharacters.html) | CC0；11 个模块化男性、24 条动作，轮廓是统一的西式低多边形奇幻角色 | 不替换当前真人；北齐方向和现有 MakeHuman 面部会一起丢失 |
| [MakeHuman system assets](https://static.makehumancommunity.org/assets/assetpacks/makehuman_system_assets.html) | 官方系统资产 CC0，含亚洲皮肤；未提供可直接使用的北朝男性军服 | 保留当前 CC0 真人和亚洲肤色，只自制外衣 |
| [OpenGameArt Greatsword Warrior](https://opengameart.org/content/greatsword-warriorwith-sword-rigged) | CC0；已下载到临时目录并在 Blender 检查，实际为羊角面甲、欧洲奇幻甲和巨剑，贴图与骨骼精度低于当前角色 | 拒绝接入，不用许可证安全掩盖美术方向错误 |
| [OpenGameArt Modular RPG Characters](https://opengameart.org/content/modular-rpg-characters) | CC0；约 2–3k 三角面、45 条动作，Blender 2.79 模块化西式奇幻人物 | 可作技术参考，不替换北齐人物，不把 16.6 MB 整包放进仓库 |
| [Quaternius Modular Weapons Pack](https://quaternius.com/packs/medievalweapons.html) | CC0；24 个带贴图的 FBX/OBJ/Blend，官方允许个人和商业项目使用；实际方向是通用欧洲中世纪剑、斧、锤、弓与盾 | 许可证合格但轮廓不合格，不把欧洲十字护手剑当北齐长刀；只作为移动端模型体量参考 |
| [OpenGameArt Katana](https://opengameart.org/content/katana) | Clint Bellanger，CC0；中等分辨率日本刀、刀鞘和三种可替换刀镡，Blender 格式 | 明确是日本刀，不接入；避免把许可证安全误当作时代与地域适配 |

## 当前武器决定

M0 仍只有一把武器。公开 CC0 候选没有同时满足许可、六世纪中国轮廓和当前角色比例，因此没有为了“拿来就用”引入错误模型。项目改为运行 `scripts/blender/build_northern_qi_longblade.py`，自产 3,136 三角面的“镇军环首长刀”。物理锚点是[大都会博物馆 30.65.2](https://www.metmuseum.org/art/collection/search/23352)：中国、河南、约公元 600 年、总长 102.2 厘米，带环首和 P 形鞘装；馆方将图像标为 Public Domain。游戏模型没有拷贝照片或扫描几何，只采用直身、紧凑刀装、缠柄、环首和约一米总长的约束。

架空变化是把参考实物转成可读性更强的直身单刃军刀，并加入无字军籍片和简化环内对纹；玩法目的只是让挥刀、格挡、弹反和拼刀时能看清刃向。时代检查排除了日本刀曲率与刀镡、西欧十字护手、双手巨剑、奇幻符文和后世高密度装饰。它不是考古复原，后续若发现更直接的北齐刀实物资料，可在不改玩法接口的前提下替换几何。

本轮因此没有把“公开模型”硬换进工程。当前衣甲保留项目自制轮廓，并新增跟随上臂的
两只合身袖筒，让西式内衬不再从肩甲与护臂之间直接露出；完整 21 部件模型仍低于
两万三角面。后续若找到许可证明确、年代和轮廓也匹配的中国北朝服装，再做隔离预览。

## KayKit 可用动作

对 `Knight.glb` 的 glTF 动画表做了实际解析。下面这些动作存在于文件中，适合先做
Unity Humanoid 重定向试验：

| 战斗用途 | 动画名 |
|---|---|
| 轻击四方向素材 | `1H_Melee_Attack_Chop`、`1H_Melee_Attack_Slice_Diagonal`、`1H_Melee_Attack_Slice_Horizontal`、`1H_Melee_Attack_Stab` |
| 双手/重击参考 | `2H_Melee_Attack_Chop`、`2H_Melee_Attack_Slice`、`2H_Melee_Attack_Spin`、`2H_Melee_Attack_Stab` |
| 格挡组 | `Block`、`Blocking`、`Block_Attack`、`Block_Hit` |
| 四向闪避 | `Dodge_Backward`、`Dodge_Forward`、`Dodge_Left`、`Dodge_Right` |
| 受击与死亡 | `Hit_A`、`Hit_B`、`Death_A`、`Death_B` |
| 战斗移动 | `Running_A`、`Running_B`、`Running_Strafe_Left`、`Running_Strafe_Right`、`Walking_Backwards` |

Skeletons 包除上述主要动作外，还有 `1H_Melee_Attack_Jump_Chop`、`Idle_Combat`、
`Taunt`、`Skeletons_Awaken_Standing`、`Skeletons_Awaken_Floor` 和复活动作。这些适合
敌人原型，但不适合主角的克制北朝军士气质。

## 文件与 Unity 处理限制

- Adventurers 的 `Knight.glb` 为 3,659,532 字节，符合仓库单文件 10 MB 上限；同一
  角色的 FBX 为 20,659,324 字节，不能直接提交。
- Skeletons 的 `Skeleton_Warrior.glb` 为 4,863,620 字节；FBX 为 22,402,076 字节，
  同样不能直接提交。
- Unity 不原生导入这两份 GLB。正确路径是把 GLB 放在 `SourceArt/` 或临时目录，
  用 Blender 只导出选中的动作，再由 Unity 提取为独立 `.anim`，最后不把超大的
  中间 FBX 放进仓库。
- 所有外来动作必须设置为 Humanoid、关闭根运动，并先检查脚底、肩膀、刀手、
  起势/生效/收势以及镜像后的持刀方向。
- 动画只能提供身体运动。命中时刻、碰撞、伤害、卡肉、镜头、音效和特效继续由
  现有战斗系统统一负责。

## 不采用的来源

- GitHub 上的 Mixamo 打包仓库或自动下载工具：工具代码的开源许可证不覆盖 Adobe
  动画文件，Mixamo 动画也不允许作为可提取的素材库重新分发。
- 商业游戏拆包、动作复刻文件、没有原作者和许可证文本的 FBX：不进入工程。
- Quaternius 的第三方镜像：本项目已经保存官方 CC0 包和许可证，没有理由换成
  无法证明文件链路的转存。
- `CULTURE3D` 等中国遗产研究数据集：未找到足以覆盖商业游戏分发的明确资产许可，
  而且数据主要是照片、点云和研究重建，不是移动端关卡模型。
- KayKit Medieval Hexagon：许可证安全，但六角地块和欧洲建筑方向与灰窑外院冲突。

## 建议接入顺序

1. 从 Adventurers 提取斜斩、横斩、刺击、劈砍、格挡受击和左右闪避，共 7 个动作。
2. 在独立预览场景重定向到当前 MakeHuman 身体，保留现有动作作为 A/B 对照。
3. 只替换视觉动作，继续使用现有攻击窗口、碰撞和技能数值。
4. 通过后再考虑 CMU 的两套武术步法；Rocketbox 和 Skeletons 暂不下载整包。
5. 场景最多先取 10–15 个 Dungeon 通用小物，统一换成灰窑材质后再决定是否保留。

任何真正进入 `Assets/_BladeBreath/ThirdParty/` 的文件，都要把来源 URL、获取日期、
上游版本或提交、原始哈希、处理步骤和许可证副本写入 `ASSET_LICENSE.md`。

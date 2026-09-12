# 无铭：漳城夜 / Wuming: Night of Zhangcheng

> `BladeBreath / 刃息` 是内部开发代号。

一款 **iOS 优先、固定斜俯视 3D、短局构筑的硬核动作游戏**。世界以北齐末年为历史母体，但王朝、人物与事件完全架空；战斗先验证读招、拼刀、格挡、弹反、闪身、破势与处决，再进入肉鸽房间和悟招构筑。

![status](https://img.shields.io/badge/status-M0.3%20Unity%20migration-6b8c91)
![engine](https://img.shields.io/badge/Unity-6.3%20LTS-black)
![license](https://img.shields.io/badge/code-MIT-green)

## 当前状态

正式主工程已经从 Godot 切换为：

- Unity `6000.3.23f1`
- C#
- Universal Render Pipeline `17.3.0` 包（当前训练场仍使用内置渲染管线）
- Input System `1.16.0`
- 横屏、iOS 优先
- 3D 场景 + 固定倾斜俯视相机

仓库仍保留 `prototype-web/` 作为历史战斗参考，但它不再是正式运行时的源码真相。

### 已经移植到 Unity C# 的灰盒能力

- 相机相对移动；
- 长刀三段轻击与独立重斩；
- 双方有效攻击帧相遇时的拼刀；
- 按住格挡与起手弹反窗口；
- 方向闪身与无敌窗口；
- 生命、架势、架势恢复、崩势；
- 对失衡敌人的近身处决；
- 确定性训练敌人；
- 可格挡明斩与不可格挡延迟重斩；
- 灰盒场地、斜俯视相机、HUD 和一键重置。

### 还没有移植完

- 听刃 / 流影两种流派与四个武技；
- 势权；
- 手机虚拟摇杆和技能按钮；
- 命中停顿、镜头震动、震动反馈与正式 VFX；
- 房间流程、悟招三选一和局外叙事。

因此当前是 **Unity 迁移基线**，不是对旧 Godot M0.2 的功能完全等价复刻。

### 本次新增：人形角色接入工具

已接入一套有真实男性面部、头发、手脚、服装和完整骨骼的 MakeHuman CC0 人体，以及 Quaternius 与 KayKit 的 CC0 动作和许可记录。主角与敌人共用人体和骨骼，通过冷蓝/烟红服装材质区分，并绑定右手长刀；十四个运行时槽位包含四条互不重复的刀法、格挡受力、前滚和左右闪身。旧的球体、盒子和棱面护甲不再挂在角色 Prefab 上。原战斗规则继续负责命中和无敌判定。

**2026-09-02 已在 Windows 的 Unity 6000.3.23f1 中生成并运行人形训练场。** 真实编辑器编译、无警告的 KayKit Humanoid 导入、四条独立刀法、左右闪身，以及键盘移动、拼刀、格挡、弹反、受击、处决和十次重置的 Play Mode 检查通过。首次操作及尚未完成的 iPhone 验收见 [人形样片上手指南](docs/14_HUMANOID_SANDBOX.md)。手机按钮与完整肉鸽流程不在这一步内。

## 第一次打开 Unity 项目

1. 克隆仓库：

   ```bash
   git clone https://github.com/ryandong8282/bladebreath.git
   cd bladebreath
   ```

2. 在 Unity Hub 安装 `6000.3.23f1`。Windows 试玩只需编辑器本体；最终在 Mac 上构建 iOS 时再安装 **iOS Build Support** 和 Xcode。
3. 在 Unity Hub 选择 **Add project from disk**，打开仓库根目录。
4. 等待 Package Manager 还原 URP 与 Input System，并等待脚本编译结束。
5. 菜单执行：

   ```text
   BladeBreath > Art > Create Humanoid Sandbox
   ```

6. 等待自动配置检查完成，在生成的 `Assets/_BladeBreath/Scenes/HumanoidSandbox.unity` 中点击 Play。

要对照原来的胶囊灰盒，可执行 `BladeBreath > Prototype > Create Combat Sandbox`。这两个场景分开保存，人形菜单再次运行会保留已有资产和场景修改。

即使还没有生成场景，空场景进入 Play Mode 时，`PrototypeBootstrap` 也会自动创建胶囊灰盒；看到胶囊时请确认已经执行人形菜单并打开 `HumanoidSandbox.unity`。

## 灰盒操作

| 动作 | 键盘 / 鼠标 | 手柄 |
|---|---|---|
| 移动 | `WASD` / 方向键 | 左摇杆 |
| 三段轻击 / 处决 | `J` / 鼠标左键 | West / X |
| 上段重斩 | `L` | North / Y |
| 格挡 / 起手弹反 | 按住 `K` / 鼠标右键 | 左肩键 |
| 闪身 | `Space` / `Shift` | East / B |
| 重置 | `R` | Start |

敌人变成橙色时是可格挡明斩；变成红色并显示“不可格挡”时应闪身。敌人架势崩溃后，靠近并再次斩击即可处决。

## 浏览器历史参考版

不安装 Unity 时仍可直接双击：

```text
prototype-web/index.html
```

它保留旧 M0.2 的两套流派、技能和更完整的交锋规则，主要用于对照 Unity 迁移时是否丢失原来的设计意图。不要长期同时维护两套正式逻辑。

## 目录

```text
bladebreath/
├── Assets/_BladeBreath/
│   ├── Editor/                    # 场景生成与项目设置菜单
│   ├── Scripts/
│   │   ├── Camera/
│   │   ├── Characters/
│   │   ├── Core/
│   │   ├── Input/
│   │   ├── Prototype/
│   │   └── UI/
│   └── Scenes/                    # 首次执行菜单后生成
├── Packages/manifest.json
├── ProjectSettings/ProjectVersion.txt
├── prototype-web/                 # 历史参考，不是正式主工程
├── docs/
└── scripts/validate_unity_project.py
```

Unity 自动生成的 `Library/`、`Temp/`、`Logs/`、`Obj/`、构建目录和 IDE 工程文件不得提交；Unity 生成的 `.meta` 文件必须提交。

## 校验

无需 Unity License 的仓库结构校验：

```bash
python3 scripts/validate_unity_project.py
# 或
make validate
```

这会验证项目版本、Package Manifest、必要 C# 文件、冲突标记、基本花括号结构，以及 Godot 运行时是否意外回流。它不能替代 Unity 编辑器编译、Play Mode 和 iPhone 真机测试。

## 核心设计底线

1. 先证明一刀是否有意思，再做肉鸽内容量；
2. 读招优先于数值，玩家必须知道为什么输；
3. 常规战斗只放 1–3 个真正有威胁的敌人；
4. 武器决定动作骨架，流派改变最佳决策，而不是只换颜色；
5. 技能由基本功供能，不鼓励绕圈等冷却；
6. 北齐只是历史母体，不照搬人物，也不做朝代元素乱炖；
7. 移动端可读性、输入可靠性和 60 FPS 高于特效数量；
8. 当前里程碑没通过前，不加第二把武器、大地图、装备海和联网。

## 文档入口

- [产品愿景与范围](docs/00_PRODUCT_VISION.md)
- [战斗系统规格](docs/01_COMBAT_SYSTEM.md)
- [Unity 技术架构](docs/02_TECH_ARCHITECTURE.md)
- [美术方向](docs/03_ART_DIRECTION.md)
- [世界观与剧情框架](docs/04_STORY_FRAMEWORK.md)
- [北齐历史母体圣经](docs/04A_HISTORICAL_BIBLE.md)
- [里程碑与验收标准](docs/05_ROADMAP.md)
- [范围护栏](docs/06_SCOPE_GUARDRAILS.md)
- [Unity → iOS 构建说明](docs/07_IOS_EXPORT.md)
- [资产生产流程](docs/08_ASSET_PIPELINE.md)
- [第一次试玩记录表](docs/09_FIRST_PLAYTEST.md)
- [老板试玩指南](docs/10_OWNER_GUIDE.md)
- [旧 M0.2 构建报告](docs/11_M0_2_BUILD_REPORT.md)
- [Unity 迁移记录](docs/13_UNITY_MIGRATION.md)
- [人形样片上手指南](docs/14_HUMANOID_SANDBOX.md)
- [开发 Backlog](docs/BACKLOG.md)
- [老板许愿池](docs/WISH_POOL.md)

## 原创与授权边界

原 Unity 灰盒使用引擎原生几何体、代码材质和 IMGUI。当前人形样片增加了 MakeHuman CC0 真人身体、Quaternius CC0 动画、Poly Haven CC0 场景表面，以及六组已登记但尚未挂到真人服装上的项目生成表面；来源、原始许可、完整 ImageGen 提示词、修改方式和哈希均登记在 [ASSET_LICENSE.md](ASSET_LICENSE.md)及对应素材清单。未加入外部字体、音乐或音效。后续外部或 AI 生成资产进入工程前仍须登记。

代码采用 [MIT License](LICENSE)。原创美术、音乐与剧情资产可在对应目录另行声明授权。

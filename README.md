# 无铭：漳城夜 / Wuming: Night of Zhangcheng

<p align="center"><img src="assets/icon.svg" width="128" alt="Wuming: Night of Zhangcheng icon"></p>

> **BladeBreath / 刃息** 是内部开发代号。这是一款 iOS 优先的固定斜俯视 3D 硬核动作游戏：以北齐末年为历史母体，玩家作为“无铭者”在漳京亡国前夜，通过武器、军系流派、读招、拼刀、弹反与破势处决杀出造像署。

![status](https://img.shields.io/badge/status-M0.2%20playable-6b8c91)
![engine](https://img.shields.io/badge/Godot-4.7.2-478cbf)
![license](https://img.shields.io/badge/code-MIT-green)

## 现在有什么

仓库包含两套相互校验的实现：

1. **浏览器战斗验证版**：零依赖，下载仓库后双击即可玩，用来最快判断战斗有没有意思；
2. **Godot 3D 原型**：未来 iOS 正式开发的主工程，长期战斗规则以这一版为准。

两套版本具备同一条最小闭环：

```text
读招 → 守 / 弹 / 闪 / 迎刀 → 夺取势权 → 打崩架势 → 处决
```

当前不是完整游戏，也不是美术成片。它只验证最重要的一件事：

> **两个简陋武俑在一块空地上对刀，是否已经能让人想再打一遍。**

## 直接试玩浏览器版

1. 下载或克隆仓库；
2. 双击 `prototype-web/index.html`；
3. 点击“开始试刃”。

无需联网、npm 或本地服务器。

### 控制

| 操作 | 键盘 / 鼠标 |
|---|---|
| 移动 | `W A S D` / 方向键 / 左下摇杆 |
| 攻击、连段、处决 | `J` / 鼠标左键 / “斩” |
| 格挡、起手弹反 | 按住 `K` / 鼠标右键 / “守” |
| 闪身 | `Space` / `Shift` / “闪” |
| 武技一 | `Q` / `U` |
| 武技二 | `E` / `I` |
| 切换听刃 / 流影 | `Tab` / “换式” |
| 重置 | `R` |

试玩时先不评价画面精不精致，只回答四个问题：

```text
1. 哪一次碰刀最爽？
2. 哪一次挨打不知道原因？
3. 哪个按钮最容易按错？
4. 输了以后还想不想立刻再来？
```

需要便携单文件时执行：

```bash
python3 scripts/build_web_single.py
```

生成文件位于 `dist/Wuming-Zhangcheng-M0.html`，它属于本地构建产物，不必提交到仓库。

## Godot 主工程

### 已实现的 M0 战斗

- 正交斜俯视镜头与相机相对移动；
- 长刀三段攻击，第三段拥有更高拼刀等级；
- 按住格挡，起手短窗口为精准弹反；
- 方向闪身、无敌帧和完美闪避；
- 生命、架势、架势恢复、崩势与近身处决；
- 敌我攻击有效窗口重叠时结算压刀、对刀或崩刀；
- **听刃流**：弹反或压刀获得锋意，武技“震刃 / 回锋”；
- **流影流**：完美闪避获得锋意，武技“掠影 / 追风斩”；
- “造像署执刃者”循环使用普通斩、延迟重斩、快速回身斩与不可格挡横扫；
- 键鼠、手柄与多点触屏操作；
- 生命 / 架势 / 锋意 HUD、敌招提示、战斗日志、胜负和重置。

### 启动

1. 安装 Godot 4.7.2 Standard（非 .NET 版）；
2. 在 Project Manager 中导入仓库根目录的 `project.godot`；
3. 点击 **Run Project**。

浏览器原型已通过自动化逻辑烟雾测试。Godot 工程已通过静态结构校验，并由 GitHub Actions 配置真实引擎解析与 180 帧启动测试；iPhone 真机输入、温度和帧率仍需实测。

## 世界观基准

- 历史母体：北齐末年，约公元 570 年代；
- 架空王朝：**大衡**；
- 首都：漳水平原上的 **漳京**；
- 北方军事中心：**晋垒**；
- 时间：**靖平七年，冬，西军越关第三日**；
- 主角：胸前无名、军籍无字的重铸武俑 **“无铭者”**；
- 开场地点：造像署所属 **校刀院灰窑外院**。

真实历史不直接改名照搬，而是提供政治地理、军事组织、器物、墓葬壁画与石窟艺术的约束。玩家应能从线索推断时代母体，但世界中的人物与事件必须独立成立。

## 技术路线

- 引擎：Godot 4.7.2 Standard
- 语言：GDScript
- 表现：轻量 3D + 正交斜俯视镜头 + 2D HUD / VFX
- 渲染：Mobile renderer
- 目标平台：iPhone / iPad 横屏
- 原型视口：1280 × 720
- 浏览器验证：原生 HTML Canvas + JavaScript，无运行时依赖

## 项目结构

```text
bladebreath/
├── project.godot                  # Godot 主工程
├── scenes/main.tscn
├── src/                           # 战斗、角色、UI、场地
├── resources/                     # 长刀与两种流派数据
├── prototype-web/                 # 可直接双击运行的浏览器版
├── assets/                        # 原创美术、模型、音频工作区
├── docs/                          # 产品、战斗、历史与制作规范
└── scripts/                       # 校验、构建、打包与发布
```

## 校验

不安装 Godot 也能校验 GDScript 结构和浏览器战斗逻辑：

```bash
./scripts/validate.sh
```

安装 Godot 后执行完整解析与 180 帧启动烟雾测试：

```bash
GODOT_BIN=/absolute/path/to/Godot STRICT_GODOT=1 ./scripts/validate.sh
```

## 公开仓库

```text
https://github.com/ryandong8282/bladebreath
```

## 设计底线

1. 读招优先于数值，玩家必须知道自己为什么输；
2. 常规战斗只放 1–3 个真正有威胁的敌人；
3. 武器决定动作骨架，流派改变最佳决策，不只换伤害颜色；
4. 技能由基本功供能，锋意不能站着等出来；
5. 北齐只是历史母体，不复制真实人物关系，也不做朝代元素乱炖；
6. 移动端可读性和输入可靠性高于特效数量；
7. M0 通过前不做第二把武器、大地图、装备词条或长篇演出。

## 文档入口

- [产品愿景与范围](docs/00_PRODUCT_VISION.md)
- [战斗系统规格](docs/01_COMBAT_SYSTEM.md)
- [技术架构](docs/02_TECH_ARCHITECTURE.md)
- [美术方向](docs/03_ART_DIRECTION.md)
- [世界观与剧情框架](docs/04_STORY_FRAMEWORK.md)
- [北齐历史母体圣经](docs/04A_HISTORICAL_BIBLE.md)
- [里程碑与验收标准](docs/05_ROADMAP.md)
- [范围护栏](docs/06_SCOPE_GUARDRAILS.md)
- [iOS 导出说明](docs/07_IOS_EXPORT.md)
- [资产生产流程](docs/08_ASSET_PIPELINE.md)
- [第一次试玩记录表](docs/09_FIRST_PLAYTEST.md)
- [老板试玩指南](docs/10_OWNER_GUIDE.md)
- [M0.2 构建与验证报告](docs/11_M0_2_BUILD_REPORT.md)
- [开发 Backlog](docs/BACKLOG.md)
- [老板许愿池](docs/WISH_POOL.md)

## 原创与授权边界

当前仓库没有引入第三方模型、贴图、字体、动画、音乐或音效。历史文物仅作为研究资料，生产资产必须原创或使用许可明确的开放资源，并登记在 [ASSET_LICENSE.md](ASSET_LICENSE.md)。可以借鉴“武器 × 流派 × 架势战斗”的抽象结构，但不复制商业游戏的角色、名称、动画、图标、UI、关卡、剧情或数值表。

代码采用 [MIT License](LICENSE)；原创美术、音乐与剧情资产可在对应目录另行声明授权。

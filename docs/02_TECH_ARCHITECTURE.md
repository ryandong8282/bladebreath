# 技术架构

## 引擎决策

原型采用 **Godot 4.7.2 Standard + GDScript + Mobile renderer**。首版不用 C#，目标是尽快验证战斗手感，而不是证明技术栈复杂。

选择依据：

- 轻量、开源、无运行时抽成；
- 3D、物理、UI、输入和资源系统完整；
- 场景与脚本以文本为主，适合 Git 与 AI 协作；
- 后续可以在 macOS 上导出 Xcode 工程并测试 iOS。

## 运行时结构

```text
Main
├── ArenaBuilder（程序化校刀院灰盒）
├── PlayerController
│   └── Combatant
├── EnemyController
│   └── Combatant
├── CameraFollow
├── CombatHUD
└── MobileControls
    ├── VirtualJoystick
    └── MobileActionButton × 6
```

### Combatant

公共结算唯一入口：

- 生命、死亡；
- 架势、恢复、失衡；
- 格挡、弹反、闪避；
- 武器有效窗口与拼刀等级；
- 处决接收；
- 镜头、HUD 与日志信号。

### PlayerController

只解释玩家意图并执行长刀动作：

- 相机相对移动；
- 三段攻击与输入缓存；
- 格挡、弹反和闪身请求；
- 听刃 / 流影的锋意获取条件；
- 两个流派各两项灰盒技能；
- 对失衡目标的上下文处决。

### EnemyController

造像署执刃者使用确定序列：

```text
明斩 → 迟锋 → 回身斩 → 裂地
```

M0 故意不用行为树。执刃者首先是战斗语法老师，不是 AI 展示品。

## 当前目录

```text
src/
  actors/player_controller.gd
  actors/enemy_controller.gd
  camera/camera_follow.gd
  combat/combatant.gd
  core/combat_types.gd
  core/input_bootstrap.gd
  data/weapon_definition.gd
  data/style_definition.gd
  game/main.gd
  ui/combat_hud.gd
  ui/mobile_action_button.gd
  ui/mobile_controls.gd
  ui/virtual_joystick.gd
  world/arena_builder.gd
  world/primitive_factory.gd

resources/
  weapons/longblade.tres
  styles/hearing_blade.tres
  styles/flowing_shadow.tres
```

## 战斗状态优先级

```text
死亡 > 失衡 > 闪身/攻击 > 格挡 > 移动 > 待机
```

状态切换只有一个所有者。攻击被弹反、拼刀失败或架势崩溃时，由 `Combatant.force_stagger()` 统一中断当前动作，再由具体控制器清理自己的计时器。

## 拼刀结算

来招可拼刀，并且接收方仍处于自身武器有效窗口时：

```text
自己的 clash_level > 来招 clash_level  → 压刀
自己的 clash_level = 来招 clash_level  → 对刀
自己的 clash_level < 来招 clash_level  → 崩刀
```

结果只在 `Combatant` 结算。玩家控制器只负责听刃流在压刀成功后获得锋意。

## 输入边界

`InputBootstrap` 建立统一 action。键鼠和触屏都只产生同一组意图，战斗层不关心输入设备来源。触屏按钮直接绑定 action，虚拟摇杆只提供二维移动向量。

## 数据策略

M0 采用两层数据：

1. `WeaponDefinition` / `StyleDefinition` 保存跨招式参数；
2. 单招的前摇、有效、后摇、伤害、架势伤害和拼刀等级集中在控制器顶部或动作字典。

M1 再提取 `ActionDefinition` Resource。现在不提前制造复杂编辑器工具，但禁止把可调参数散落到无关函数。

## 信号通道

```text
health_changed
posture_changed
edge_changed
state_changed
combat_log
impact_requested
telegraph_started
attack_resolved
```

HUD、相机、未来音效、震动与 VFX 只能监听反馈，不能反向修改战斗结算。

## 性能边界

- 目标 60 FPS；
- 常规高威胁敌人不超过 3；
- Mobile renderer；
- 严格限制实时阴影、透明叠加和全屏后处理；
- 内容阶段为常用 VFX 和投射物建立对象池；
- 不在每帧创建大批节点或遍历整棵场景树。

## 验证策略

```bash
./scripts/validate.sh
GODOT_BIN=/path/to/Godot ./scripts/validate.sh
```

第一条做资源路径、括号、重复类/函数、缩进和冲突标记检查；第二条额外让 Godot 解析项目并运行主场景 180 帧。完整手感仍必须人工测试。

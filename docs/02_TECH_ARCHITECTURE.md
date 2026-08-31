# Unity 技术架构

## 引擎决策

正式主工程采用：

- Unity `6000.3.23f1`
- C#
- Universal Render Pipeline `17.3.0`
- Input System `1.16.0`
- 3D 场景 + 正交斜俯视相机
- iOS 横屏优先

选择 Unity 不是为了提前堆商业插件，而是为了获得成熟的 iOS 构建链、Animator / Timeline / Shader Graph、移动端性能工具、资源生态和更容易找到接手者的 C# 工程结构。当前仍坚持少包、少抽象、先灰盒。

## 当前启动方式

`PrototypeBootstrap` 带有 `RuntimeInitializeOnLoadMethod`。任何场景进入 Play Mode 后，如果场景中没有启动器，它会自动生成：

```text
BladeBreath Runtime Greybox
├── Grey Kiln Courtyard
├── Player - Wuming
│   ├── CharacterController
│   ├── Combatant
│   └── PlayerController
├── Enemy - Statue Office Bladebearer
│   ├── CapsuleCollider
│   ├── Combatant
│   └── EnemyController
└── Prototype HUD

Main Camera
└── TopDownCamera
```

这是迁移期保险绳，不是长期关卡生产方式。执行菜单：

```text
BladeBreath > Prototype > Create Combat Sandbox
```

会生成并保存 `Assets/_BladeBreath/Scenes/CombatSandbox.unity`，同时加入 Build Settings。

## 运行时组件职责

### `Combatant`

唯一公共战斗结算入口：

- 生命、死亡；
- 架势、延迟恢复；
- 格挡与起手弹反；
- 无敌窗口；
- 失衡与处决；
- 战斗结果和可观察状态。

它不读取输入，不控制相机，也不直接画 UI。

### `PlayerController`

把玩家意图翻译成动作：

- 相机相对移动；
- 转向；
- 长刀灰盒攻击；
- 格挡、弹反请求；
- 方向闪身；
- 对失衡目标的上下文处决。

当前攻击参数集中在 Inspector 字段中。恢复三段连段后，再将招式定义迁移到 `ScriptableObject`，不提前制造大型技能编辑器。

### `EnemyController`

M0 训练敌人使用确定性节奏，而不是行为树：

```text
逼近 → 明斩 → 明斩 → 明斩 → 延迟不可格挡裂地 → 循环
```

它首先是一名战斗语法老师。每次攻击拥有可见预警、结算和恢复阶段。

### `PrototypeInput`

统一桌面与手柄意图。它同时兼容新 Input System 与 Legacy Input Manager 预处理符号，迁移期不因输入后端设置不同而完全失去控制。

移动端虚拟摇杆和按钮尚未接入；正式接入时应输出相同的动作意图，而不是让战斗代码认识具体 UI 按钮。

### `TopDownCamera` 与 `PrototypeHud`

只观察战斗：

- 相机跟随玩家；
- HUD 展示生命、架势、敌招和当前事件；
- 不反向修改结算。

## 当前代码结构

```text
Assets/_BladeBreath/
├── Editor/
│   └── BladeBreathProjectSetup.cs
└── Scripts/
    ├── Camera/TopDownCamera.cs
    ├── Characters/EnemyController.cs
    ├── Characters/PlayerController.cs
    ├── Core/Combatant.cs
    ├── Input/PrototypeInput.cs
    ├── Prototype/PrototypeBootstrap.cs
    └── UI/PrototypeHud.cs
```

## 状态优先级

```text
死亡 > 失衡 > 闪身 > 攻击 > 格挡 > 移动 > 待机
```

同一状态切换只能有一个所有者。被弹反、架势崩溃或死亡时，控制器必须停止继续制造有效攻击。

## 下一层数据架构

达到 M0.2 功能等价后，再引入：

```text
WeaponDefinition : ScriptableObject
StyleDefinition  : ScriptableObject
ActionDefinition : ScriptableObject
ModifierDefinition : ScriptableObject
```

建议招式数据：

```text
ActionDefinition
├── startup
├── active
├── recovery
├── damage
├── postureDamage
├── clashLevel
├── movementCurve
├── cancelRules
└── feedbackProfile
```

“听刃”“流影”通过战斗事件与 Modifier 组合改变决策，不在 `PlayerController` 里铺满流派名判断。

## 反馈边界

后续统一暴露事件：

```text
HealthChanged
PostureChanged
StateChanged
AttackStarted
AttackResolved
ImpactRequested
TelegraphStarted
ExecutionStarted
```

HUD、相机、音频、震动和 VFX 订阅这些事件。反馈层可以改变观感，不能改变伤害、无敌或架势结算。

## 性能边界

- 默认目标 60 FPS，兼容档 30 FPS；
- 常规高威胁敌人不超过 3；
- 优先 URP Forward / Mobile 友好设置；
- 限制实时阴影、透明叠加、全屏后处理和过度 Shader 变体；
- 高频 VFX、伤害字和投射物进入内容阶段前建立对象池；
- 不在 `Update` 中做全场对象搜索、LINQ 聚合或持续分配；
- 性能结论以目标 iPhone 真机 Profiler 数据为准。

## 验证策略

```bash
python3 scripts/validate_unity_project.py
```

该命令验证仓库结构和最基本的 C# 文本完整性。每次玩家可见改动还必须通过：

1. Unity Console 编译；
2. Play Mode；
3. 对应分辨率 Game View；
4. 涉及移动端时的 iPhone 真机测试。

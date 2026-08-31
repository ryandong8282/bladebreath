# `_BladeBreath`

Unity 正式工程的自有内容统一放在这里。

```text
_BladeBreath/
├── Art/            # 原创模型、材质、贴图、动画和 VFX
├── Audio/          # 音乐、环境与战斗音效
├── Data/           # ScriptableObject 战斗数据
├── Editor/         # 仅编辑器可用的搭建与校验工具
├── Prefabs/        # 角色、敌人、场景块和反馈预制体
├── Scenes/         # Boot、CombatSandbox、Run、Hub
├── Scripts/        # 运行时代码
└── Tests/          # EditMode / PlayMode 测试
```

当前仓库只提交纯代码灰盒。第一次在 Unity 中打开后，执行：

`BladeBreath > Prototype > Create Combat Sandbox`

Unity 会生成 `Scenes/CombatSandbox.unity`，同时应用横屏与 iOS 基础设置。

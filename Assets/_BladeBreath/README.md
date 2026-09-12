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
├── ThirdParty/     # 官方原始模型、动画压缩包及许可
└── Tests/          # EditMode / PlayMode 测试
```

第一次在 Unity 中打开后，可执行人形接入菜单：

`BladeBreath > Art > Create Humanoid Sandbox`

随后可执行当前垂直切片的画面升级：

`BladeBreath > Art > Apply Grey Kiln Art Pass`

该菜单会把两名角色重建为有面部、头发、手脚和完整骨骼的 MakeHuman 男性，再挂接 Blender 生成的北朝军服轮廓：长衣、分片裙摆、前后札甲、双层肩甲、护臂、束发、空白铭牌与执刃者灰陶面。玩家用靛蓝旧布，敌人用朱红制服和陶面区分。灰窑外院本体、双窑火、残像、脚手架、陶器、灰尘、战斗火花、刀光、声音与 HUD 都已接入；不需要购买素材包。

Unity 会生成 `Scenes/HumanoidSandbox.unity`、两套角色外观 Prefab、十四个动作槽位、六组 1K 角色表面和 Animator，同时应用横屏与 iOS 基础设置。当前长刀核心模组包含“入洞提撩—腰砍—拗步追砍”三段轻击、独立重斩“埋头·镇落”、持守/弹反、有效帧拼刀，以及带原创图标、锋意消耗和冷却状态的三项武技“迎推 / 回锋 / 震烈”；三段轻击和重斩使用四条独立 KayKit CC0 动作，格挡受力与左右闪身也有独立身体动作。拼刀会触发独立锵鸣、卡肉、震屏，并给予 5 秒“抗衡”增益。敌人出招锁向且只覆盖前方扇形，玩家绕背后可用重击发动两段背袭处决，处决全程无敌。闪身已加长并带运行时拖影，普通受击拥有独立短硬直和后震，但不会误开处决窗口。玩家与训练敌人共用动作骨架。iPhone 真机验收仍未完成。完整记录见仓库 `docs/14_HUMANOID_SANDBOX.md` 与 `docs/17_LATE_MING_WEAPON_COMBAT_STUDY.md`。

原 `BladeBreath > Prototype > Create Combat Sandbox` 菜单继续保留胶囊灰盒。

# M0.2 构建报告：校刀院灰窑外院

## 本轮目标

把项目从口头设定推进到一个可直接打开的战斗验证件，并将北齐末年母体固化为后续生产约束。

## 交付内容

### 可运行

- `prototype-web/index.html`：浏览器零依赖版本；
- `project.godot`：Godot 3D 主工程；
- `scripts/build_web_single.py`：可选的本地单文件构建器。

### 战斗

- 长刀三段攻击；
- 格挡与起手弹反；
- 闪身与完美闪避；
- 主动拼刀；
- 生命、架势、锋意、实验性势权；
- 崩势与处决；
- 听刃 / 流影两套资源与技能循环；
- 造像署执刃者的四类攻击。

### 世界与美术

- 大衡、漳京、晋垒、西军与造像署；
- 靖平七年亡国夜；
- 无铭者与被改写的军籍；
- 万像署第一章结构；
- 校刀令·迟盖小头目纸面规格；
- 石窟残彩、墓道仪仗、灰窑铜火视觉规则；
- 北齐历史母体、架空映射与时代禁区。

## 自动验证结果

执行：

```bash
./scripts/validate.sh
```

当前本地结果：

```text
Static validation passed: 15 GDScript files, 4 Godot resources/scenes.
Web combat smoke: basic attack damaged enemy; startup parry damaged posture and generated Edge; style switch changed skill data; draw path completed.
```

## 未验证项目

- 当前执行环境没有 Godot 4.7.2 二进制，因此本地未完成 Godot 解析和 180 帧运行测试；
- GitHub Actions 已配置真实 Godot 引擎验证，但最终状态以远端工作流为准；
- 未进行 iPhone 真机导出；
- 未测试真实触屏延迟、震动和刘海 / 灵动岛安全区；
- 还没有真人试玩结论；
- 势权线仍处于实验状态。

这些限制不会伪装成“已经完成”。M0.2 的下一项工作不是加新武器，而是先试玩并调一刀的手感。

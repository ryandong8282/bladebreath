# Unity → iOS 构建说明

## 前置条件

- macOS；
- Xcode；
- Unity Hub；
- Unity `6000.3.23f1`；
- 安装该编辑器版本的 **iOS Build Support**；
- Apple Developer Team；
- 唯一 Bundle Identifier。

当前占位配置：

```text
Bundle Identifier: com.ryandong8282.bladebreath
Minimum iOS: 15.0
Scripting Backend: IL2CPP
Orientation: Landscape
```

建立正式 App Store Connect 记录前可以调整 Bundle ID；建立后不要随意改。

## 首次项目设置

Unity 编译完成后执行：

```text
BladeBreath > Project > Apply iOS Defaults
```

它会应用：

- 横屏；
- Linear Color Space；
- iOS IL2CPP；
- 占位 Bundle ID；
- 最低 iOS 版本；
- Force Text 资源序列化。

之后仍要在 Player Settings 中人工检查产品名、图标、版本号、签名相关设置和隐私声明。

## 推荐构建流程

1. 先在 Unity Editor 的 `CombatSandbox.unity` 验证战斗；
2. 在 Build Profiles 中切换到 iOS；
3. 确认唯一 Bundle Identifier、版本号和 Build Number；
4. 将 Xcode 工程导出到仓库外的空目录；
5. 在 Xcode 选择 Development Team 与签名；
6. 连接真实 iPhone 构建；
7. 用 Development Build + Profiler 验证输入、CPU、GPU、内存和发热；
8. 通过后再生成 Archive / TestFlight 包。

## 渲染与性能

- 编辑器 Game View 不是性能结论；
- iOS 模拟器不能替代真机 GPU、触控、震动和温控；
- 优先从 URP 的简单 Forward 配置开始；
- 先关闭昂贵后处理和多余实时阴影，再分析瓶颈；
- 记录测试设备、系统版本、画质档、平均帧率、低帧和温度；
- 默认目标 60 FPS，确实无法稳定时才设计明确的 30 FPS 兼容档。

## 不提交到 Git

- Apple 私钥和签名证书；
- Provisioning Profile；
- 带个人凭据的导出选项；
- Xcode `xcuserdata`；
- `DerivedData/`；
- 导出的 Xcode 工程；
- `.ipa`；
- Unity `Library/`、`Temp/`、`Logs/` 和 `Obj/`。

## 首次真机验收表

- 横屏左右方向都正确；
- 刘海、圆角和 Home Indicator 不遮挡 UI；
- 同时移动与攻击不丢触摸；
- 按住格挡、滑出按钮和系统中断后的释放状态正确；
- 前后台切换不会卡在攻击、格挡或无敌状态；
- 音频中断与恢复正常；
- 震动可关闭；
- 触屏失败不是因为按钮过小或遮挡；
- 目标画质档帧率稳定；
- 连续玩 15–20 分钟后的发热和耗电可接受。

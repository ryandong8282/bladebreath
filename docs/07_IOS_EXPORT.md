# iOS 导出说明

## 前置条件

- macOS；
- Xcode；
- Godot 4.7.2 Standard；
- 与引擎版本匹配的 Godot Export Templates；
- Apple Developer Team；
- 唯一的 Bundle Identifier。

## 推荐开发流程

1. 先在 Godot 桌面编辑器验证战斗；
2. 安装 Export Templates；
3. `Project → Export → Add… → iOS`；
4. 填入 App Store Team ID 与 Bundle Identifier；
5. 导出到空目录，生成 Xcode 工程；
6. 在 Xcode 中配置签名并连接真机；
7. 真机检查安全区、触屏、震动、音频和性能。

建议 Bundle ID 暂定为：

```text
com.ryandong.bladebreath
```

正式上架前可更换，但一旦建立 App Store Connect 记录，不应随意改变。

## 渲染注意

项目默认使用 Mobile renderer。iOS 模拟器对渲染后端存在额外限制，战斗性能和触屏体验必须以真机为准。Apple Silicon Mac 可以辅助调试，但仍不能代替 iPhone 测试。

## 不提交到 Git 的内容

- 个人签名证书；
- Provisioning Profile；
- Apple 私钥；
- Xcode `xcuserdata`；
- 导出的 `.ipa`；
- 带个人 Team ID 的私有配置（除非仓库明确使用占位符）。

## 首次真机验收表

- 横屏方向正确；
- 刘海和 Home Indicator 不遮挡按钮；
- 同时移动和按攻击不丢触摸；
- 按住格挡时移出按钮区域仍能正确释放；
- 震动可关闭；
- 切到后台再回来不会卡在攻击或格挡状态；
- 来电/音频中断恢复正常；
- 设备发热与耗电可接受；
- 目标画质档位帧率稳定。

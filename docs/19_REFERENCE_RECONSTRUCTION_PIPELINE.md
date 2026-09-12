# 参考图深度与图生 3D 管线

这一轮承认当前 Unity 画面与目标图仍有明显质量差距。程序化方块可以验证战斗和构图，但不能继续承担最终巨像、窑炉、人物服装和近景道具的精度。本管线只服务当前一座灰窑外院，不扩充第二关卡或第二武器。

## 已落地的两类重建

### 整图：镜头空间深度

五张项目目标图先由 Depth Anything V2 Small 生成 16 位相对深度图，再按 52° 垂直视场角反投影为带顶点颜色的 PLY 网格。HUD 稳定区域在网格阶段被剔除。每张代理约 2.46 万顶点、4.6 万三角形。

这些代理用于：

- 比对目标图的前、中、后景层次；
- 标定 Unity 相机高度、俯角和视场角；
- 确定巨像、窑炉、门洞、角色与前景遮挡的屏幕占比；
- 在 Blender 中从轻微侧视角发现当前场景体块偏差。

单目深度没有生成被遮挡的背面，因此它是 2.5D 镜头代理，不是可供玩家绕行的关卡模型。把整张图直接挤成一张浮雕后放进游戏会在转动镜头时露馅。

### 单物件：完整网格

完整资产改成“参考图提取设计语言 → 生成干净单物件输入 → 单图重建 360° 网格 → Blender 清理 → Unity 移动端版本”。第一件是右侧破损石灰岩巨像。干净输入必须满足完整轮廓、单一物件、中性光、无 UI、无脚手架和透明或中灰背景，否则模型会把画面杂物焊进网格。

当前正式工具是 MIT 许可的 TripoSR；它能从单图生成完整网格并烘焙 UV 纹理。第一版原始网格已经生成，再由 Blender 清理、减面并合入灰窑地标 FBX：成品巨像为 25,204 三角形，使用一张 1K 颜色纹理。Apache-2.0 的 InstantMesh 保留为后续几何升级候选。Hunyuan3D 2.1 虽然能生成 PBR 材质，但其社区许可证限制输出在欧盟、英国和韩国的使用，不进入面向全球 iOS 发行的正式资产链。

Unity 运行时不会相信图生 3D 工具给出的任意单位、坐标和朝向。`GreyKilnCourtyard` 会把巨像统一到批准高度，旋转正面对准固定战斗镜头，把底座埋入场景碎石层，并给它独立的窑火轮廓光。整图深度对照还把默认镜头距离改到 7.8 米，并把原本因反向绕序而不可见的圆形地坪改成 64 段、单绘制网格的石环。

当前网格已经证明这条技术路线可以落进实际游戏画面，但单张输入仍会产生软化的五官、对称化和模糊凿刻。它是第一件可运行的生成地标，不是最终英雄级雕像；下一次替换必须在保持同一三角形、纹理和屏幕占比预算的前提下提高表面可信度。

### 运行时环境母图：固定镜头 2.5D 重建

为了让当前唯一灰窑外院先达到目标图的场景密度，工程另外生成了一张无角色、无 UI、无文字的原创 16:9 环境母图。它保留宽阔圆形石场、后墙双窑火、右侧残损巨像、陶器与木架的画面关系，但不使用任何商业游戏模型、纹理或关卡布局。运行时颜色图裁为 1024 × 576；Depth Anything V2 Small 从它生成相对深度，再转成 256 × 144 的线性反深度纹理。

`DepthReconstructedBackdrop` 在启动时生成 128 × 72 网格，即 9,216 顶点和 18,034 三角形，按 37° 垂直视场角把像素反投影到 12–32 米的相机视锥。网格固定在世界空间，因此跟随和滚轮缩放会产生克制的视差；角色、武器、刀光和 UI 始终由 Unity 真实渲染。原程序化院落的可见网格在母图启用时关闭，但碰撞、窑火、战斗范围与地面逻辑继续存在。角色脚下增加一张代码生成的柔边接触影，减少人物像贴纸悬在母图上的感觉。

这是一座固定轴线战斗房间的生产手段。单目深度没有后脑勺、墙背面或遮挡后的几何，镜头大幅旋转、绕到巨像背后或把它当开放关卡都会露出拉伸面。当前里程碑只允许轻微跟随和拉近拉远；需要自由镜头的近景人物、武器、可碰撞地标仍必须走完整网格、Blender 清理和 Unity LOD 流程。

## 移动端入库门槛

生成模型不会直接复制到 `Assets/`。Blender 处理后必须同时满足：

- 巨像保留一个连续主轮廓，清除漂浮面、内层壳和背景残片；
- LOD0 不超过约 35,000 三角形，LOD1 不超过约 12,000 三角形；
- 一套 1K 主纹理，必要时增加 1K 法线；不提交模型权重或超 10 MB 二进制；
- 原点落在底座中心，米制比例稳定，正面朝向 Unity 约定；
- 以中距离游戏镜头检查轮廓和材质，不用近距离离线渲染掩盖问题；
- 在 `ASSET_LICENSE.md` 和相邻 `SOURCE_MANIFEST.json` 记录输入、工具、许可证、哈希和人工修改。

## 历史与架空检查

- **真实锚点**：六世纪北方石灰岩造像、响堂山石窟造像比例、残损与凿刻痕迹。
- **架空改造**：巨像的胸前系带、破裂位置、工场状态和在灰窑院内的位置属于大衡造像署原创设定。
- **玩法目的**：在右侧形成稳定的大体量地标，为战斗方向、绕背和镜头景深提供参照。
- **时代错置检查**：不加入明清盔甲、日式武者轮廓、欧洲哥特雕塑或现代水泥浇筑表面。

## 可复现命令

外部模型与虚拟环境位于 `D:\AI_Tools`，不会进入仓库。深度代理由下列脚本生成：

```powershell
D:\AI_Tools\Depth-Anything-V2\.venv\Scripts\python.exe scripts\reference_reconstruction.py `
  SourceArt\ReferenceReconstruction\Targets\target_01.jpg `
  --depth-anything-root D:\AI_Tools\Depth-Anything-V2 `
  --checkpoint D:\AI_Tools\Depth-Anything-V2\checkpoints\depth_anything_v2_vits.pth `
  --output-dir SourceArt\ReferenceReconstruction\Depth `
  --input-size 770 --mesh-width 256 --vertical-fov 52 --hud-mask
```

Blender 检查文件由 `scripts/blender/build_reference_depth_proxy.py` 创建。代理只留在 `SourceArt/`，不会被 Unity 打包。

运行时环境母图使用同一个脚本，但输入改为裁好的 1K 生产颜色图、垂直视场改为 37°，并关闭 HUD 剔除：

```powershell
D:\AI_Tools\Depth-Anything-V2\.venv\Scripts\python.exe scripts\reference_reconstruction.py `
  Assets\_BladeBreath\Art\Environment\Generated\Textures\GreyKilnDepthBackdrop_Albedo.png `
  --depth-anything-root D:\AI_Tools\Depth-Anything-V2 `
  --checkpoint D:\AI_Tools\Depth-Anything-V2\checkpoints\depth_anything_v2_vits.pth `
  --output-dir SourceArt\ReferenceReconstruction\ProductionPlate `
  --input-size 770 --mesh-width 256 --vertical-fov 37
```

Unity 只打包 1K 颜色图和 256 × 144 反深度图；高分辨率深度、PLY 代理、模型权重和 Python 环境都留在 `SourceArt/` 或仓库外。

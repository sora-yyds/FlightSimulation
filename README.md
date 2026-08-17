# FlightSimulation

基于 Unity 开发的第三人称战斗机飞行模拟项目，提供偏街机风格的气动控制、地面起降、无限测试场地和未来航迹指示。

## 核心特性

- 基于 `Rigidbody` 的推力、升力、阻力与侧滑模拟
- 俯仰、横滚、偏航、协调转弯及侧滑修正
- 可切换的失速模拟与高迎角飞行辅助
- 三点式起落架、地面滑跑、转向、刹车与起降流程
- 循环回收的无限地面、距离网格和方向参照物
- 第三人称跟随镜头、飞行 HUD 与未来航迹预测
- 中文 Inspector 参数，便于直接调校飞行手感

## 快速开始

### 环境要求

- Unity `2022.3.21f1`

### 安装

```bash
git clone https://github.com/sora-yyds/FlightSimulation.git
cd FlightSimulation
```

仓库不包含 SU-27 模型及其纹理。运行前请将对应资源放入以下路径，并保留仓库中的 `.meta` 文件：

- `Assets/Planes/Meshes/su-27pu_ussr.fbx`
- `Assets/Planes/Textures/su27pu-hull.png`
- `Assets/Planes/Textures/su27pu-window.png`

使用 Unity Hub 打开项目，载入 `Assets/Scenes/SampleScene.unity`，然后进入 Play Mode。

### 操作

| 操作 | 按键 |
| --- | --- |
| 增加 / 降低油门 | W / S |
| 偏航 / 地面转向 | A / D |
| 横滚 | Num 4 / Num 6 |
| 俯仰 | Num 8 / Num 5 |
| 收放起落架 | G |
| 轮刹 | 空格 |

失速模拟可通过画面左上角按钮开启或关闭。

## 技术栈

- Unity 2022.3 LTS
- C#
- Universal Render Pipeline 14.0.10
- Unity PhysX / Rigidbody
- Legacy Input Manager

## 开源协议

项目源代码基于 [MIT License](LICENSE) 开源。第三方模型、纹理及其他资源的使用权以其各自原始授权为准。

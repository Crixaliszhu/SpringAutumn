一、设计目标
由于《春秋问鼎》采用：
天下地图（World Map）
        ↓
区域地图（Region Map）
因此摄像机不能使用一套逻辑。
应该设计：
World Camera
       +
Region Camera
两套独立摄像机系统。

二、总体 Camera 架构
CameraSystem
|
├── WorldCameraController
|
├── RegionCameraController
|
├── CameraInputAdapter
|
└── CameraState
职责：
模块
职责
WorldCameraController
控制天下地图摄像机
RegionCameraController
控制区域地图摄像机
CameraInputAdapter
统一鼠标和触摸输入
CameraState
保存当前摄像机状态

三、World Map Camera（天下摄像机）
3.1 设计定位
天下地图的作用：
查看势力范围； 
查看战争状态； 
选择 Region； 
国家战略规划。 
因此：
需要大范围、低频、平滑的移动。

3.2 默认视角
推荐：
        Camera
           \
            \
             \
              地图
即：
45°斜俯视角（Isometric-like）
参数建议：
参数
建议
Rotation X
45°
Rotation Y
0°
Field Of View
40~50°

3.3 支持操作
1. 拖动地图
手机：
单指拖动
PC：
鼠标右键拖动
或 WASD
效果：
Camera Position 改变

2. 缩放
手机：
双指捏合
PC：
鼠标滚轮
范围：
Min Zoom
   |
   |
   |
Max Zoom
例如：
状态
高度
最近
30
最远
120

3.4 不支持旋转
V1.0 建议禁止：
绕地图旋转
原因：
影响玩家方向感； 
增加触摸复杂度； 
不利于微信小游戏操作。 

四、Region Map Camera（区域摄像机）

4.1 设计定位
区域地图承担：
查看城邑； 
查看村庄； 
查看军队； 
战争操作。 
因此：
需要更近距离、更精细的观察。

4.2 默认视角
类似：
     Camera
        \
         \
          🏯 城
       🌾     🌾
参数：
参数
建议
Rotation X
50~60°
FOV
45°

4.3 支持操作
地图拖动
同 World Map：
单指拖动

缩放
允许：
查看整个 Region
      ↓
观察单个城邑
例如：
状态
高度
最近
8
最远
35

4.4 自动聚焦（重点）
进入 Region 时：
World Map
      ↓
选择 Region
      ↓
加载 Region Map
      ↓
Camera Focus Region Center

4.5 点击目标自动移动
例如：
玩家点击：
咸阳城
摄像机：
Smooth Move
        ↓
中心对准咸阳城
方便查看细节。

五、Camera 边界限制

World Map
限制：
不能飞出天下地图
例如：
X:
-200 ~ 200

Z:
-150 ~ 150

Region Map
限制：
不能飞出区域地图
例如：
X:
-50 ~ 50

Z:
-50 ~ 50

六、Camera 动画系统
推荐使用：
SmoothDamp
而不是：
直接修改 Transform
效果：
当前位置
     ↓
缓慢移动
     ↓
目标位置
体验更接近商业策略游戏。

七、Camera 状态保存
设计：
CameraState
{
    Vector3 Position;

    float Zoom;
}

World Map
保存：
当前查看位置
缩放等级
例如：
玩家查看：
楚国边境
进入区域：
楚西郡
返回天下地图：
恢复：
楚国边境视角
避免重新寻找。

Region Map
保存：
当前 Region 摄像机位置
缩放等级
方便再次进入。

八、Camera 与 UI 的关系
非常重要：
UI 操作时禁止摄像机响应
错误：
点击按钮
      ↓
地图拖动

正确：
Touch
 |
EventSystem.IsPointerOverUI()
 |
YES
 |
忽略 Camera Input

九、Camera 与 Input 解耦
不要：
Camera
   |
读取 Input
推荐：
InputSystem
      |
CameraInputAdapter
      |
CameraController
好处：
手机与 PC 共用 Camera； 
以后可接入手柄； 
方便测试。 

十、Unity 代码结构
建议目录：
Assets
└── _Game
    └── Presentation
        └── Camera
            ├── CameraManager.cs
            ├── ICameraController.cs
            │
            ├── WorldCameraController.cs
            ├── RegionCameraController.cs
            │
            ├── CameraInputAdapter.cs
            └── CameraState.cs

十一、类设计
ICameraController
public interface ICameraController
{
    void Move(Vector2 delta);

    void Zoom(float value);

    void Focus(Vector3 target);

    void SaveState();

    void RestoreState();
}

CameraManager
负责：
当前模式管理
例如：
World Mode
      ↓
切换
      ↓
Region Mode

十二、摄像机切换流程
进入 Region：
WorldMap
    |
保存 World Camera State
    |
加载 Region Scene
    |
创建 Region Camera
    |
Focus Region Center

返回天下：
Region Map
    |
保存 Region Camera State
    |
切换 World Map
    |
恢复 World Camera State

十三、微信小游戏性能建议

1. 不使用 Cinemachine（V1.0）
原因：
增加包体； 
功能过剩； 
需要额外学习和维护。 
V1.0 自研简单 CameraController 更合适。

2. 避免频繁计算
例如：
不要：
每帧 Raycast 大量检测
应该：
只有点击时检测

3. 摄像机数量
运行时：
只激活一个 Camera
不要：
World Camera + Region Camera 同时 Render

十四、未来 V2/V3 扩展
预留：
战斗镜头
例如：
攻城开始
      ↓
Camera Fly To 城门

历史事件镜头
例如：
秦灭晋
      ↓
Camera Zoom Out
      ↓
展示天下版图变化

昼夜天气镜头效果
未来加入：
雾效； 
光照变化； 
季节变化。 

十五、最终 Camera 架构
                InputSystem
                     |
                     ↓
          CameraInputAdapter
                     |
             +-------+--------+
             |                |
             ↓                ↓
    WorldCamera       RegionCamera
             |                |
             +-------+--------+
                     |
              CameraManager
                     |
               Unity Camera

设计总结
《春秋问鼎》的 Camera 设计核心是：
天下看格局，区域看经营。
因此：
地图
Camera 特点
World Map
高空、稳定、查看势力变化
Region Map
近距离、精细、支持战争操作
关键原则：
✅ 两套 Camera Controller； 
✅ 保存地图浏览状态； 
✅ 单指拖动； 
✅ 双指缩放； 
✅ 不允许自由旋转； 
✅ UI 输入与 Camera 解耦； 
✅ 只激活一个 Camera。
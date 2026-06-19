一、设计目标
Input System 是玩家与游戏世界交互的入口。
它负责：
玩家手指
    ↓
Input System
    ↓
射线检测（Raycast）
    ↓
识别地图对象
    ↓
Selection System
    ↓
打开对应 UI
    ↓
玩家下达 Command
    ↓
GameEngine

二、核心设计原则
2.1 输入与业务完全分离
错误设计：
点击村庄
    ↓
VillageView
    ↓
直接修改粮食
问题：
UI 与游戏逻辑耦合； 
难以测试； 
难以扩展。 

正确设计：
点击村庄
    ↓
Selection System
    ↓
Village Panel
    ↓
BuildCommand
    ↓
GameEngine
    ↓
WorldRuntime

三、输入层级设计
根据双地图架构：
Input System
        |
        |
        +----------------+
        |                |
        ↓                ↓
 World Input        Region Input

World Map 输入
可点击：
对象
功能
Region
查看区域信息
国家标识
查看国家信息（V2）
空白区域
取消选择

Region Map 输入
可点击：
对象
功能
City
城市管理
Village
村庄管理
Army
军队控制
空地
取消选择

四、地图对象接口设计
所有地图可点击对象统一实现：
public interface ISelectable
{
    string Id { get; }

    SelectionType Type { get; }

    void OnSelected();

    void OnDeselected();
}

SelectionType
public enum SelectionType
{
    Region,

    City,

    Village,

    Army,

    Nation
}

五、View 层实现
例如：
RegionView
public class RegionView : MonoBehaviour, ISelectable
{
    public string Id;

    public SelectionType Type 
        => SelectionType.Region;

    public void OnSelected()
    {
        Highlight(true);
    }

    public void OnDeselected()
    {
        Highlight(false);
    }
}

VillageView
VillageView
        |
        | 实现
        ↓
ISelectable
负责：
高亮； 
动画； 
显示选中状态。 
不负责：
修改人口； 
修改资源； 
处理征兵。 

六、Selection Manager 设计
系统核心：
SelectionManager
        |
        |
当前选中对象 CurrentSelection

职责
管理：
当前选择对象； 
取消旧选择； 
触发 UI 更新。 

选择流程
例如点击村庄：
Raycast
    ↓
VillageView
    ↓
SelectionManager.Select()
    ↓
取消旧对象高亮
    ↓
设置新对象
    ↓
打开 VillagePanel

七、SelectionManager 类设计
public class SelectionManager
{
    private ISelectable current;

    public void Select(ISelectable target)
    {
        if(current != null)
        {
            current.OnDeselected();
        }

        current = target;

        if(current != null)
        {
            current.OnSelected();
        }

        SelectionEvent.Publish(current);
    }

    public void Clear()
    {
        Select(null);
    }
}

八、Raycast 点击检测
Unity 中：
Screen Point
        |
        ↓
Camera Ray
        |
        ↓
Physics Raycast
        |
        ↓
Collider
        |
        ↓
ISelectable

九、点击优先级设计
World Map
通常：
Region
即可。

Region Map
可能出现：
军队站在城市上
例如：
     🚩
     🏯
点击应该优先：
Army
 ↓
City
 ↓
Village
 ↓
Terrain

实现方式
使用 Layer：
Army Layer        Priority 1

City Layer        Priority 2

Village Layer     Priority 3

Ground Layer      Priority 4
Raycast 按 Layer 顺序检测。

十、长按与拖动区分
手机端重点。

点击
时间：
< 200ms
移动：
< 10像素
结果：
Selection

拖动
移动：
> 10像素
结果：
Camera Move

长按（V2）
未来：
500ms
触发：
快捷菜单
例如：
军队

移动
攻击
驻扎

十一、双击设计（V2）
例如：
双击 Region：
自动进入 Region Map
双击 Army：
Camera Focus Army

十二、UI 输入优先级
非常重要。
例如：
玩家点击：
建造按钮
不能：
同时选择地图

流程：
Touch
 |
EventSystem
 |
是否点击UI？
 |
YES
 |
停止地图输入

十三、输入状态机设计
定义：
public enum InputState
{
    Idle,

    Selecting,

    DraggingCamera,

    OpeningUI,

    ExecutingCommand
}

状态转换：
          点击
Idle ------------> Selecting
 |
 |拖动
 ↓
DraggingCamera

Selecting
 |
 |打开面板
 ↓
OpeningUI

UI按钮
 |
 ↓
ExecutingCommand

十四、Input 模块结构
目录：
Assets
└── _Game
    └── Presentation
        └── Input
            |
            ├── InputManager.cs
            |
            ├── TouchInputAdapter.cs
            |
            ├── MouseInputAdapter.cs
            |
            ├── SelectionManager.cs
            |
            ├── ISelectable.cs
            |
            ├── SelectionType.cs
            |
            └── InputState.cs

十五、Input 与 Camera 关系
不要：
Input
 |
Camera
 |
Selection
因为会耦合。

推荐：
                 InputManager
                       |
         +-------------+-------------+
         |                           |
         ↓                           ↓
 CameraController            SelectionManager

手指移动
Touch Move
     |
     ↓
CameraController.Move()

手指点击
Touch Click
     |
     ↓
Raycast
     |
     ↓
SelectionManager.Select()

十六、Input 与 UI 的关系
SelectionManager
          |
          |
          EventBus
          |
          |
UI Manager
          |
          |
打开对应 Panel
例如：
选择 Village
        |
VillageSelectedEvent
        |
VillagePanel.Open()

十七、完整玩家操作链
建造农田
手指点击村庄
        |
Raycast
        |
VillageView
        |
SelectionManager
        |
VillagePanel
        |
点击【建造】
        |
BuildCommand
        |
CommandQueue
        |
Game Tick
        |
ConstructionSystem
        |
WorldRuntime 更新
        |
BuildingFinishedEvent
        |
UI刷新

十八、微信小游戏性能优化

1. 不在 Update 中大量 Raycast
错误：
每帧检测所有点击对象

正确：
只有 Touch End 执行一次 Raycast

2. 使用 LayerMask
例如：
Selectable Layer
减少：
地形； 
装饰物； 
特效。 
的检测。

3. 对象数量评估
V1.0：
Region：24

City：21

Village：68

Army：最多几十
总选择对象：
约100~150个
对移动端压力极小。

十九、未来 V2 扩展
预留：
框选军队； 
多军队编队； 
长按快捷命令； 
手势操作； 
战斗技能点击； 
地图标记。 

二十、最终 Input 架构
                Player
                   |
                Touch
                   |
             InputManager
                   |
       +-----------+-----------+
       |                       |
       ↓                       ↓
CameraController        RaycastSystem
                               |
                               ↓
                         ISelectable
                               |
                               ↓
                     SelectionManager
                               |
                               ↓
                           EventBus
                               |
                               ↓
                              UI
                               |
                               ↓
                          Command
                               |
                               ↓
                          GameEngine

设计总结
《春秋问鼎》V2 输入系统核心思想：
Input 负责感知玩家行为，Selection 负责选择对象，UI 负责展示操作，Command 负责改变天下。
严格遵守：
Input ≠ 游戏规则
View ≠ 数据
UI ≠ State
完整链路：
点击地图
    ↓
选择对象
    ↓
打开面板
    ↓
生成 Command
    ↓
GameEngine Tick
    ↓
WorldRuntime 改变
    ↓
Event 通知
    ↓
地图与 UI 刷新